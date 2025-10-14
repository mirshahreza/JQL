using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRunTemplatesTests
    {
        [Fact]
        public void DbParamToCSharpInputParam_Maps_Many_Types()
        {
            var run = JqlRun.Instance("DefaultConnection");
            Assert.Equal("string S", run.DbParamToCSharpInputParam(new JqlParam("S","NVARCHAR")));
            Assert.Equal("Int64 B", run.DbParamToCSharpInputParam(new JqlParam("B","BIGINT")));
            Assert.Equal("int I", run.DbParamToCSharpInputParam(new JqlParam("I","INT")));
            Assert.Equal("DateTime D", run.DbParamToCSharpInputParam(new JqlParam("D","DATE")));
            Assert.Equal("Boolean F", run.DbParamToCSharpInputParam(new JqlParam("F","BIT")));
            Assert.Equal("decimal M", run.DbParamToCSharpInputParam(new JqlParam("M","DECIMAL")));
            Assert.Equal("float R", run.DbParamToCSharpInputParam(new JqlParam("R","FLOAT")));
            Assert.Equal("byte[] IMG", run.DbParamToCSharpInputParam(new JqlParam("IMG","IMAGE")));
        }

        [Fact]
        public void GetSqlTemplate_Returns_Templates_For_All_QueryTypes()
        {
            var run = JqlRun.Instance("DefaultConnection");
            Assert.Contains("INSERT INTO", run.GetSqlTemplate(QueryType.Create));
            Assert.Contains("SELECT", run.GetSqlTemplate(QueryType.ReadList));
            Assert.Contains("SELECT", run.GetSqlTemplate(QueryType.AggregatedReadList));
            Assert.Contains("SELECT", run.GetSqlTemplate(QueryType.ReadByKey));
            Assert.Contains("UPDATE", run.GetSqlTemplate(QueryType.UpdateByKey));
            Assert.Contains("DELETE", run.GetSqlTemplate(QueryType.Delete));
            Assert.Contains("DELETE", run.GetSqlTemplate(QueryType.DeleteByKey));
            Assert.Contains("EXEC", run.GetSqlTemplate(QueryType.Procedure));
            Assert.Contains("SELECT * FROM", run.GetSqlTemplate(QueryType.TableFunction));
            Assert.Contains("SELECT", run.GetSqlTemplate(QueryType.ScalarFunction));
        }
    }
}
