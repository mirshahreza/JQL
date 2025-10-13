using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;
using PowNet.Configuration;
using System.IO;

namespace JQL.Test
{
    public class JqlRequestCompileTests
    {
        private static JqlModel BuildModel()
        {
            var root = PowNetConfiguration.ServerPath;
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            var m = new JqlModel("DefaultConnection","Users", root);
            m.Columns.AddRange(new[]
            {
                new JqlColumn("Id") { DbType = "int", IsPrimaryKey = true },
                new JqlColumn("Name") { DbType = "nvarchar", Size = "50" },
                new JqlColumn("IsActive") { DbType = "bit" }
            });
            m.DbQueries.Add(new JqlQuery("ReadList", QueryType.ReadList)
            {
                Columns = new List<JqlQueryColumn>{ new() { Name = "Id" }, new() { Name = "Name" } },
                Aggregations = new List<JqlAggregation>{ new("Count","COUNT(*)") }
            });
            m.Save();
            return m;
        }

        [Fact(Skip = "Integration-like: requires real DB connection")]
        public void CompileAggregations_Include_Exclude_Works()
        {
            var m = BuildModel();
            var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.ReadList");

            // include indicated
            req.AggregationsContainment = Containment.IncludeIndicatedItems;
            req.ClientIndicatedAggregations = new List<string>{ "Count" };
            var list = req.Exec();
            Assert.NotNull(list);

            // exclude all
            req.AggregationsContainment = Containment.ExcludeAll;
            list = req.Exec();
            Assert.NotNull(list);
        }

        [Fact(Skip = "Integration-like: requires real DB connection")]
        public void CompileOrder_And_Pagination_Defaults_Work()
        {
            var m = BuildModel();
            var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.ReadList");
            req.Pagination = new JqlPagination { PageNumber = 0, PageSize = 0 };
            var r = req.Exec();
            Assert.NotNull(r);
        }
    }
}
