using System;
using System.Collections;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class NegativeCasesTests
    {
        private readonly RealDbFixture _fx;
        public NegativeCasesTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Missing_Model_Or_Query_Should_Fail_Gracefully()
        {
            Assert.Throws<PowNetException>(() => JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.NoSuchObject.ReadList"));
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Invalid_Param_Type_Should_Be_Wrapped()
        {
            int id = DbTestHelper.SeedMaster(_fx.ConnectionName, "NEG", true);
            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.Params = [ new JqlParamRaw("Id", "not-int") ];
            var ex = Record.Exception(() => rbk.Exec());
            Assert.NotNull(ex);
        }
    }
}
