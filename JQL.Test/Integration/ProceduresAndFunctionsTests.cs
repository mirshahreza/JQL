using System;
using System.Collections;
using System.Collections.Generic;
using JQL;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class ProceduresAndFunctionsTests
    {
        private readonly RealDbFixture _fx;
        public ProceduresAndFunctionsTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Procedure_With_Inputs_Works()
        {
            var ctx = new Hashtable();
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.DbDirect.Exec", ctx);
            req.Params = new List<JqlParamRaw>
            {
                new("SomeParam", 42)
            };
            var res = req.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void TableFunction_And_ScalarFunction_Works()
        {
            var ctx = new Hashtable();

            var tf = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.Reports.Select", ctx);
            tf.Params = [ new JqlParamRaw("Year", DateTime.Now.Year) ];
            var rows = tf.Exec();
            Assert.NotNull(rows);

            var sf = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.Formulas.Calculate", ctx);
            sf.Params = [ new JqlParamRaw("x", 2) ];
            var val = sf.Exec();
            Assert.NotNull(val);
        }
    }
}
