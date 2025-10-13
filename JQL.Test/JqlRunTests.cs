using Xunit;
using PowNet.Configuration;

namespace JQL.Test
{
    public class JqlRunTests
    {
        [Fact(Skip = "Integration: requires PowNet settings and a real DB configuration")]
        public void DbParamToCSharpInputParam_Maps_CommonTypes()
        {
            var dbConf = DatabaseConfiguration.FromSettings("DefaultConnection");
            var run = JqlRun.Instance(dbConf);

            Assert.Equal("string Name", run.DbParamToCSharpInputParam(new JqlParam("Name","NVARCHAR")));
            Assert.Equal("int Age", run.DbParamToCSharpInputParam(new JqlParam("Age","INT")));
            Assert.Equal("Int64 Big", run.DbParamToCSharpInputParam(new JqlParam("Big","BIGINT")));
            Assert.Equal("DateTime Dt", run.DbParamToCSharpInputParam(new JqlParam("Dt","DATE")));
            Assert.Equal("Boolean Flag", run.DbParamToCSharpInputParam(new JqlParam("Flag","BIT")));
            Assert.Equal("decimal Amount", run.DbParamToCSharpInputParam(new JqlParam("Amount","DECIMAL")));
            Assert.Equal("float Ratio", run.DbParamToCSharpInputParam(new JqlParam("Ratio","FLOAT")));
            Assert.Equal("byte[] Blob", run.DbParamToCSharpInputParam(new JqlParam("Blob","IMAGE")));
        }
    }
}
