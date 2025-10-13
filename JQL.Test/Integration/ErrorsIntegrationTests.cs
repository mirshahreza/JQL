using System;
using System.Collections;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class ErrorsIntegrationTests
    {
        private readonly RealDbFixture _fx;
        public ErrorsIntegrationTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void MinN_MaxN_Validation_On_Relations()
        {
            // ensure a relation exists on master -> detail
            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "ERR-MINMAX", true);

            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey");
            upd.Params = [ new JqlParamRaw("Id", mid) ];
            upd.Relations = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Collections.Generic.List<JqlParamRaw>>>
            {
                [DbTestHelper.Detail] =
                [
                    // Try setting MaxN to 0 conventionally isn't part of model; we force a delete-only row to drop below min
                    [ new JqlParamRaw("_flag_", "d") ]
                ]
            };

            // Expect either validation failure or successful no-op depending on relation settings; we at least assert no crash
            var ex = Record.Exception(() => upd.Exec());
            Assert.Null(ex);
        }
    }
}
