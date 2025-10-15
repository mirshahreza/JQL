using System;
using System.Reflection;
using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlRequestCompileRefToTests
    {
        [Fact]
        public void CompileRefTo_Builds_LeftJoin_And_Columns()
        {
            var req = new JqlRequest { QueryFullName = "Dummy.Conn.Object.ReadList" };
            // set dbIO private field
            var dbioField = typeof(JqlRequest).GetField("dbIO", BindingFlags.Instance | BindingFlags.NonPublic);
            dbioField!.SetValue(req, JqlRun.Instance("DefaultConnection"));

            var refTo = new JqlRefTo("RefTbl", "Id")
            {
                Columns = [ new JqlQueryColumn { Name = "RefName", As = "RefNameAlias" } ]
            };
            var mi = typeof(JqlRequest).GetMethod("CompileRefTo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);
            var res = mi!.Invoke(req, new object?[]{ "MainTbl", "MainId", refTo });
            Assert.NotNull(res);
            var tuple = ((Tuple<string,string>)res!);
            Assert.Contains("RefTbl_MainId", tuple.Item1);
            Assert.Contains("RefNameAlias", tuple.Item1);
            Assert.Contains("JOIN", tuple.Item2, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RefTbl", tuple.Item2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
