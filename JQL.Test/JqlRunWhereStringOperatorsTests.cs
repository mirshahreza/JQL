using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRunWhereStringOperatorsTests
    {
        private readonly JqlRun _run = JqlRun.Instance("DefaultConnection");

        [Fact]
        public void Compile_StartsWith_EndsWith_Contains_Produce_Like()
        {
            var sw = new CompareClause("Name", "Ab") { CompareOperator = CompareOperator.StartsWith };
            var ew = new CompareClause("Name", "yz") { CompareOperator = CompareOperator.EndsWith };
            var ct = new CompareClause("Name", "mm") { CompareOperator = CompareOperator.Contains };

            var s1 = _run.CompileWhereCompareClause(sw, "T", "[T].[Name]", "@P", "NVARCHAR");
            var s2 = _run.CompileWhereCompareClause(ew, "T", "[T].[Name]", "@P", "NVARCHAR");
            var s3 = _run.CompileWhereCompareClause(ct, "T", "[T].[Name]", "@P", "NVARCHAR");

            Assert.Contains("LIKE", s1);
            Assert.Contains("LIKE", s2);
            Assert.Contains("LIKE", s3);
        }
    }
}
