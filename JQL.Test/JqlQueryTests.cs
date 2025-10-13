using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlQueryTests
    {
        [Fact]
        public void HasFlags_Work_Correctly()
        {
            var q = new JqlQuery("ReadList", QueryType.ReadList)
            {
                Columns = new List<JqlQueryColumn> { new() { Name = "Id" } },
                Params = new List<JqlParam> { new("P", "INT") },
                Aggregations = new List<JqlAggregation> { new("Count", "COUNT(*)") },
                Relations = new List<string> { "R1" }
            };

            Assert.True(q.HasAggregations);
            Assert.True(q.HasParams);
            Assert.True(q.HasRelations);
        }

        [Fact]
        public void Dispose_Nulls_Collections()
        {
            var q = new JqlQuery("ReadByKey", QueryType.ReadByKey)
            {
                Columns = new List<JqlQueryColumn> { new() { Name = "Id" } },
                Params = new List<JqlParam> { new("P", "INT") },
                Where = new JqlWhere(),
                Relations = new List<string> { "R" },
                HistoryTable = "HT"
            };

            q.Dispose();

            Assert.Null(q.Columns);
            Assert.Null(q.Params);
            Assert.Null(q.Where);
            Assert.Null(q.Relations);
            Assert.Null(q.HistoryTable);
        }
    }
}
