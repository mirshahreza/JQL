using System;
using System.Reflection;
using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlRequestSqlContainsTests
    {
        [Fact]
        public void ReadList_OrderSqlStatement_Override_Is_Used_In_CompileOrder()
        {
            var req = new JqlRequest { QueryFullName = "Dummy.Conn.Object.ReadList" };
            req.OrderSqlStatement = "ORDER BY 1";
            var mi = typeof(JqlRequest).GetMethod("CompileOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);
            var res = mi!.Invoke(req, null) as string;
            Assert.Equal("ORDER BY 1", res);
        }

        [Fact]
        public void Pagination_Template_Contains_Fetch_And_Offset()
        {
            var run = JqlRun.Instance("DefaultConnection");
            var tpl = run.GetPaginationSqlTemplate();
            Assert.Contains("FETCH", tpl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OFFSET", tpl, StringComparison.OrdinalIgnoreCase);
        }
    }
}
