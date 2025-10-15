using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRunDeeperTests
    {
        private readonly JqlRun _run = JqlRun.Instance("DefaultConnection");

        [Fact]
        public void Pagination_Group_Order_LeftJoin_Tran_Templates_Are_NotEmpty()
        {
            Assert.Contains("OFFSET", _run.GetPaginationSqlTemplate());
            Assert.Contains("GROUP BY", _run.GetGroupSqlTemplate());
            Assert.Contains("ORDER BY", _run.GetOrderSqlTemplate());
            var leftJoin = _run.GetLeftJoinSqlTemplate();
            Assert.Contains("LEFT", leftJoin);
            Assert.Contains("JOIN", leftJoin);
            var tran = _run.GetTranBlock();
            Assert.Contains("BEGIN", tran);
            Assert.Contains("COMMIT", tran);
        }

        [Fact]
        public void CompileWhereCompareClause_Generates_Operator_And_Param()
        {
            var clause = new CompareClause("Id", 5) { CompareOperator = CompareOperator.MoreThan };
            var sql = _run.CompileWhereCompareClause(clause, "T", "[T].[Id]", "@P", "INT");
            Assert.Contains("[T].[Id]", sql);
            Assert.Contains(">", sql);
            Assert.Contains("@P", sql);
        }
    }
}
