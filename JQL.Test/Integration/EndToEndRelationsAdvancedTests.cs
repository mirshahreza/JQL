using System.Collections;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class EndToEndRelationsAdvancedTests
    {
        private readonly RealDbFixture _fx;
        public EndToEndRelationsAdvancedTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadList_With_MTM_Projection_Filter_Sort_Paging()
        {
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 5 };
            req.OrderClauses = [ new JqlOrderClause("Id", OrderDirection.DESC) ];
            req.Where = new JqlWhere { CompareClauses = [ new CompareClause("Id", 0) { CompareOperator = CompareOperator.MoreThan } ] };
            req.RelationsContainment = Containment.IncludeAll; // if MTM only are included by default, this is no-op
            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadByKey_With_All_Relations_And_ClientIndicatedColumns()
        {
            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.ColumnsContainment = Containment.IncludeIndicatedItems;
            rbk.ClientIndicatedColumns = ["Id","Name","IsActive"]; // restrict columns
            var r = rbk.Exec();
            Assert.NotNull(r);
        }
    }
}
