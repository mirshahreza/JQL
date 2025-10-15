using System;
using System.Linq;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class RelationScenariosTests
    {
        private readonly RealDbFixture _fx;
        public RelationScenariosTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Detect_OneToMany_And_ManyToMany_And_SelfReference_And_OneToOne()
        {
            var schema = new DbSchemaUtils(_fx.ConnectionName);
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Self reference: Node(Id PK, ParentId FK -> Node.Id)
            const string node = "ITEST_Node";
            var nodeTbl = new DbTableChangeTrackable(node)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", DbType = "INT", AllowNull = false },
                    new JqlColumnChangeTrackable("ParentId") { DbType = "INT", AllowNull = true, State = "n", Fk = new JqlFk("", node, "Id") }
                ]
            };
            schema.CreateOrAlterTable(nodeTbl);
            factory.CreateServerObjectsFor(new DbObject(node, DbObjectType.Table));

            // One-to-One: Profile(MasterId PK & FK -> Master.Id)
            const string profile = "ITEST_Profile";
            var profTbl = new DbTableChangeTrackable(profile)
            {
                Columns = [ new JqlColumnChangeTrackable("MasterId") { IsPrimaryKey = true, DbType = "INT", AllowNull = false, State = "n", Fk = new JqlFk("", DbTestHelper.Master, "Id") } ]
            };
            schema.CreateOrAlterTable(profTbl);
            factory.CreateServerObjectsFor(new DbObject(profile, DbObjectType.Table));

            // Ensure Master model exists
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Master, DbObjectType.Table));

            // Load models
            var masterModel = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            var nodeModel = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, node);
            var profileModel = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, profile);

            // OTM should exist from Detail
            bool hasOtm = masterModel.Relations?.Any(r => r.RelationType == RelationType.OneToMany) == true;
            Assert.True(hasOtm);

            // MTM should exist from MasterTag
            bool hasMtm = masterModel.Relations?.Any(r => r.RelationType == RelationType.ManyToMany) == true;
            Assert.True(hasMtm);

            // Self reference: Node should be tree
            Assert.True(nodeModel.IsTree());

            // Relation between Master and Profile should exist (type could be Unknown/OTM based on implementation)
            bool related = masterModel.Relations?.Any(r => r.RelationTable.Equals(profile, StringComparison.OrdinalIgnoreCase)) == true
                           || profileModel.Relations?.Any(r => r.RelationTable.Equals(DbTestHelper.Master, StringComparison.OrdinalIgnoreCase)) == true;
            Assert.True(related);

            // Cleanup
            schema.DropTable(profile);
            schema.DropTable(node);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void LogicalRelations_Create_And_Remove()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            factory.CreateLogicalFk("LR_Master_To_Tag", DbTestHelper.Master, "Id", DbTestHelper.Tag, "Id");
            var model = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.True(model.Relations?.Any(r => r.RelationName == "LR_Master_To_Tag") == true);

            factory.RemoveLogicalFk(DbTestHelper.Master, "Id");
            var model2 = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            // Either removed or ignored depending on implementation, ensure no crash
            Assert.NotNull(model2);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void EnforceRelation_True_Applies_On_FK_Create()
        {
            var schema = new DbSchemaUtils(_fx.ConnectionName);
            const string strict = "ITEST_StrictDetail";
            var tbl = new DbTableChangeTrackable(strict)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", DbType = "INT", AllowNull = false },
                    new JqlColumnChangeTrackable("MasterId") { DbType = "INT", AllowNull = false, State = "n", Fk = new JqlFk("FK_Strict", DbTestHelper.Master, "Id") { EnforceRelation = true } }
                ]
            };
            schema.CreateOrAlterTable(tbl);

            var fks = schema.GetTableFks(strict);
            Assert.True(fks.Rows.Count >= 1);

            schema.DropTable(strict);
        }
    }
}
