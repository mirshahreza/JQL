using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlWhereOperatorsCompileTests
    {
        private readonly JqlRun _run = JqlRun.Instance("DefaultConnection");

        [Fact]
        public void Compile_NotIn_And_IsNotNull()
        {
            var notIn = new CompareClause("Id", "[1,2,3]") { CompareOperator = CompareOperator.NotIn };
            var sql1 = _run.CompileWhereCompareClause(notIn, "T", "[T].[Id]", "@P", "INT");
            Assert.Contains("NOT", sql1);
            Assert.Contains("IN", sql1);

            var isNotNull = new CompareClause("Name", null) { CompareOperator = CompareOperator.IsNotNull };
            var sql2 = _run.CompileWhereCompareClause(isNotNull, "T", "[T].[Name]", "@P", "NVARCHAR");
            Assert.Contains("IS", sql2);
            Assert.Contains("NOT", sql2);
            Assert.Contains("NULL", sql2);
        }

        [Fact]
        public void Compile_Less_And_MoreEqual()
        {
            var lt = new CompareClause("Id", 10) { CompareOperator = CompareOperator.LessThan };
            var sql1 = _run.CompileWhereCompareClause(lt, "T", "[T].[Id]", "@P", "INT");
            Assert.Contains("<", sql1);

            var ge = new CompareClause("Id", 5) { CompareOperator = CompareOperator.MoreThanOrEqual };
            var sql2 = _run.CompileWhereCompareClause(ge, "T", "[T].[Id]", "@P", "INT");
            Assert.Contains(">=", sql2);
        }
    }
}
