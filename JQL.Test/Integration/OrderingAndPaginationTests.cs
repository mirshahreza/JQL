using System.Collections;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class OrderingAndPaginationTests
    {
        private readonly RealDbFixture _fx;
        public OrderingAndPaginationTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Multi_Order_And_Pagination_Caps_To_Max()
        {
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 10 };
            req.OrderClauses =
            [
                new JqlOrderClause("Name", OrderDirection.ASC),
                new JqlOrderClause("Id", OrderDirection.DESC)
            ];
            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void OrderSqlStatement_Overrides_Clauses()
        {
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.OrderSqlStatement = "ORDER BY [ITEST_Master].[Name] DESC";
            var r = req.Exec();
            Assert.NotNull(r);
        }
    }
}
