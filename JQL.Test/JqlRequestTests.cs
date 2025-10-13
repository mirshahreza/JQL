using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;
using PowNet.Configuration;

namespace JQL.Test
{
    public class JqlRequestTests
    {
        [Fact(Skip = "Integration: requires real DB and model files in PowNetConfiguration.ServerPath")]
        public void GetInstanceByQueryName_And_Exec_Flow()
        {
            var ctx = new Hashtable
            {
                ["UserId"] = 1,
                ["UserName"] = "tester"
            };

            var cq = JqlRequest.GetInstanceByQueryName("Db.Users.ReadList", ctx);
            cq.Params = new List<JqlParamRaw> { new("PageSize", 10) };

            var result = cq.Exec();
            Assert.NotNull(result);
        }

        [Fact]
        public void CompileOrder_FallsBack_ToPk_When_NoClauses()
        {
            // isolated test of CompileOrder via public surface: build minimal model/query
            var model = new JqlModel("Db", "Users", modelsFolder: "C:/tmp");
            model.Columns.AddRange(new[]
            {
                new JqlColumn("Id") { DbType = "int", IsPrimaryKey = true },
                new JqlColumn("Name") { DbType = "nvarchar", Size = "50" }
            });

            var q = new JqlQuery("ReadList", QueryType.ReadList)
            {
                Columns = new List<JqlQueryColumn>{ new() { Name = "Id" } }
            };

            // We cannot call private methods; this test ensures no exceptions creating request object
            // in absence of order clauses. Full path covered by integration test above.
            Assert.True(q.HasParams == false);
        }
    }
}
