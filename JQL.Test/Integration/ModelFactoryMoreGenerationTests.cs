using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class ModelFactoryMoreGenerationTests
    {
        private readonly RealDbFixture _fx;
        public ModelFactoryMoreGenerationTests(RealDbFixture fx) => _fx = fx;

        [Fact]
        public void Create_Duplicate_Recreate_Queries_Save_Model_File()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Ensure base model exists
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Master, DbObjectType.Table));

            // 1) Create a new query on model
            factory.CreateQuery(DbTestHelper.Master, nameof(QueryType.ReadList), "ReadList2");

            // 2) Duplicate that query
            factory.DuplicateQuery(DbTestHelper.Master, "ReadList2", "ReadList2_Copy");

            // 3) Recreate json of one method from its type
            factory.ReCreateMethodJson(new DbObject(DbTestHelper.Master, DbObjectType.Table), "ReadList2");

            // Assert model file exists and contains our queries
            var path = JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.True(File.Exists(path));
            var model = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.Contains(model.DbQueries, q => q.Name == "ReadList2");
            Assert.Contains(model.DbQueries, q => q.Name == "ReadList2_Copy");
        }

        [Fact]
        public void Sync_And_RemoveRemovedRelations_Do_Not_Delete_Model_File()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Ensure model exists
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Master, DbObjectType.Table));

            // Sync columns with DB
            factory.SyncDbDialog(DbTestHelper.Master);

            // Normalize relations in queries if needed
            factory.RemoveRemovedRelationsFromDbQueries(DbTestHelper.Master);

            // Model should remain on disk
            var path = JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void SynchDbDirectMethods_Generates_Controller_CSharp_File()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Generate DbDirect controller file (C#). It should be created even if no procs/functions.
            factory.SynchDbDirectMethods();

            var csPath = JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, "DbDirect").Replace(".jqlmodel.json", ".cs");
            Assert.True(File.Exists(csPath));
        }

        [Fact]
        public void LogicalFk_Create_Then_Remove_Updates_Model_File()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Ensure model exists
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Master, DbObjectType.Table));

            // Create a logical FK on PK->PK (for test purposes)
            factory.CreateLogicalFk("LR_Master_To_Tag", DbTestHelper.Master, "Id", DbTestHelper.Tag, "Id");

            var model = JqlModel.Load(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.NotNull(model);

            // Remove the logical FK (should not delete the model file)
            factory.RemoveLogicalFk(DbTestHelper.Master, "Id");

            var path = JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master);
            Assert.True(File.Exists(path));
        }
    }
}
