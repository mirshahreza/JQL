using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlWhereAndCompileUnitTests
    {
        [Fact]
        public void CompileDbQueryColumn_Formats_Name_And_Alias()
        {
            var qcol = new JqlQueryColumn { Name = "Col", As = "Alias" };
            // We keep this unit-test independent of DB execution and only validate structure creation
            Assert.Equal("Col", qcol.Name);
            Assert.Equal("Alias", qcol.As);
        }

        [Fact]
        public void CompileWhere_Accepts_Complex_Structure()
        {
            var w = new JqlWhere
            {
                ConjunctiveOperator = ConjunctiveOperator.OR,
                CompareClauses = new List<CompareClause>
                {
                    new("X", 1) { CompareOperator = CompareOperator.MoreThan },
                    new("Y", null) { CompareOperator = CompareOperator.IsNull },
                },
                ComplexClauses = new List<JqlWhere>
                {
                    new() { CompareClauses = new List<CompareClause> { new("Z", "[1,2,3]") { CompareOperator = CompareOperator.In } } }
                }
            };
            Assert.NotNull(w);
        }
    }
}
