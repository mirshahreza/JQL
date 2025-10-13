using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlXRecordsTests
    {
        [Fact]
        public void JqlOrderClause_Ctor_SetsDefaults()
        {
            var o = new JqlOrderClause("Name");
            Assert.Equal("Name", o.Name);
            Assert.Equal(OrderDirection.ASC, o.OrderDirection);
        }

        [Fact]
        public void JqlParam_Ctor_SetsDefaults()
        {
            var p = new JqlParam("Name", "NVARCHAR");
            Assert.Equal("Name", p.Name);
            Assert.Equal("NVARCHAR", p.DbType);
            Assert.True(p.AllowNull);
            Assert.Null(p.Size);
            Assert.Null(p.Value);
            Assert.Null(p.ValueSharp);
        }

        [Fact]
        public void JqlFk_Ctor_SetsProps()
        {
            var fk = new JqlFk("FK_T_C","T","Id");
            Assert.Equal("FK_T_C", fk.FkName);
            Assert.Equal("T", fk.TargetTable);
            Assert.Equal("Id", fk.TargetColumn);
            Assert.False(fk.EnforceRelation);
        }

        [Fact]
        public void JqlRefTo_Sets_Targets_And_Columns()
        {
            var rt = new JqlRefTo("Users","Id") { Columns = new List<JqlQueryColumn> { new() { Name = "Name" } } };
            Assert.Equal("Users", rt.TargetTable);
            Assert.Equal("Id", rt.TargetColumn);
            Assert.Single(rt.Columns);
            Assert.Null(rt.RefTo);
        }

        [Fact]
        public void JqlWhere_Defaults()
        {
            var w = new JqlWhere();
            Assert.Equal(ConjunctiveOperator.AND, w.ConjunctiveOperator);
            Assert.Null(w.CompareClauses);
            Assert.Null(w.SimpleClauses);
            Assert.Null(w.ComplexClauses);
        }

        [Fact]
        public void CompareClause_And_Phrase_Ctors()
        {
            var c = new CompareClause("Name");
            Assert.Equal("Name", c.Name);
            Assert.Equal(CompareOperator.Equal, c.CompareOperator);
            Assert.Null(c.Value);

            var p = new ComparePhrase("A>B");
            Assert.Equal("A>B", p.Phrase);
        }

        [Fact]
        public void ClientQueryMetadata_Ctor_SetsCollections()
        {
            var m = new ClientQueryMetadata("Q","Table");
            Assert.Equal("Q", m.Name);
            Assert.Equal("Table", m.Type);
            Assert.Empty(m.QueryColumns);
            Assert.Empty(m.FastSearchColumns);
            Assert.Empty(m.ExpandableSearchColumns);
            Assert.Empty(m.OptionalQueries);
        }
    }
}
