using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlAggregationCompileTests
    {
        [Fact]
        public void Aggregation_Containment_IncludeExclude_Multiple()
        {
            var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.ReadList");
            req.AggregationsContainment = Containment.IncludeIndicatedItems;
            req.ClientIndicatedAggregations = new List<string> { "Count", "Sum", "Max" };
            var ex = Record.Exception(() => req.Exec());
            Assert.NotNull(ex);

            req.AggregationsContainment = Containment.ExcludeIndicatedItems;
            req.ClientIndicatedAggregations = new List<string> { "Sum" };
            ex = Record.Exception(() => req.Exec());
            Assert.NotNull(ex);
        }
    }
}
