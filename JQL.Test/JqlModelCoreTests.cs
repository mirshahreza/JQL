using System;
using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlModelCoreTests
    {
        [Fact]
        public void GetPk_Returns_PrimaryKey()
        {
            var m = new JqlModel("Db","Tree","C:/tmp");
            m.Columns.AddRange(new[]
            {
                new JqlColumn("Id") { DbType = "int", IsPrimaryKey = true },
                new JqlColumn("ParentId") { DbType = "int", Fk = new JqlFk("FK","Tree","Id") },
                new JqlColumn("Name") { DbType = "nvarchar", Size = "50" }
            });
            var pk = m.GetPk();
            Assert.Equal("Id", pk.Name);
            Assert.True(m.IsTree());
            Assert.True(m.IsSelfReferenceColumn("ParentId"));
            Assert.Equal("ParentId", m.GetTreeParentColumnName());
        }

        [Fact]
        public void GetPk_Throws_When_Not_Defined()
        {
            var m = new JqlModel("Db","Users","C:/tmp");
            m.Columns.Add(new JqlColumn("Name") { DbType = "nvarchar", Size = "50" });
            Assert.Throws<System.Exception>(() => m.GetPk());
        }

        [Fact]
        public void GetRelationsForAQuery_Filters_By_Type_And_FileCentric()
        {
            var m = new JqlModel("Db","Master","C:/tmp");
            // Simulate ReadList behavior by exposing only MTM relation name in the query relations
            m.DbQueries.Add(new JqlQuery("ReadList", QueryType.ReadList) { Relations = new List<string>{ "R2" } });
            m.Relations = new List<JqlRelation>
            {
                new("Detail","Id","MasterId") { RelationName = "R1", RelationType = RelationType.OneToMany, IsFileCentric = false },
                new("MasterTag","Id","MasterId") { RelationName = "R2", RelationType = RelationType.ManyToMany, IsFileCentric = false }
            };
            var onlyOtm = m.GetRelationsForAQuery("ReadList", RelationType.OneToMany);
            var onlyMtm = m.GetRelationsForAQuery("ReadList", RelationType.ManyToMany);
            Assert.Empty(onlyOtm);
            Assert.Single(onlyMtm);
        }
    }
}
