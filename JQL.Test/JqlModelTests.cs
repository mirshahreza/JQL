using System.Collections.Generic;
using System.Linq;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlModelTests
    {
        private static JqlModel BuildTreeModel()
        {
            var model = new JqlModel("Db", "Nodes", modelsFolder: "C:/tmp");
            model.Columns.AddRange(new[]
            {
                new JqlColumn("Id") { DbType = "int", IsPrimaryKey = true },
                new JqlColumn("ParentId") { DbType = "int", Fk = new JqlFk("FK","Nodes","Id") },
                new JqlColumn("Name") { DbType = "nvarchar", Size = "50", IsHumanId = true }
            });

            model.Relations = new List<JqlRelation>
            {
                new("Children","Id","ParentId") { RelationName = "To_Children_On_ParentId", RelationType = RelationType.OneToMany, IsFileCentric = false }
            };

            model.DbQueries.Add(new JqlQuery("ReadByKey", QueryType.ReadByKey)
            {
                Relations = new List<string> { "To_Children_On_ParentId" }
            });

            return model;
        }

        [Fact]
        public void TreeHelpers_Work()
        {
            var model = BuildTreeModel();
            Assert.True(model.IsTree());
            Assert.Equal("ParentId", model.GetTreeParentColumnName());
            Assert.True(model.IsSelfReferenceColumn("ParentId"));
        }

        [Fact]
        public void HumanIdHelpers_Work()
        {
            var model = BuildTreeModel();
            Assert.Equal("Name", model.GetHumanIds());
            Assert.Contains("Name", model.GetHumanIdsList());
            Assert.Single(model.GetHumanIdsOrig());
        }

        [Fact]
        public void RelationsHelpers_Work()
        {
            var model = BuildTreeModel();
            var rels = model.GetRelationsForAQuery("ReadByKey", RelationType.OneToMany);
            Assert.Single(rels);
            Assert.Equal("To_Children_On_ParentId", rels[0].RelationName);

            Assert.NotNull(model.TryGetRelation("To_Children_On_ParentId"));
            Assert.Equal("To_Children_On_ParentId", model.GetRelation("To_Children_On_ParentId").RelationName);
        }
    }
}
