using System.Reflection;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRequestPaginationMaxSizeTests
    {
        [Fact]
        public void CompilePagination_Respects_PaginationMaxSize_Cap()
        {
            var req = new JqlRequest { QueryFullName = "Dummy.Conf.Object.ReadList", Pagination = new JqlPagination { PageNumber = 2, PageSize = 100 } };
            var dbioField = typeof(JqlRequest).GetField("dbIO", BindingFlags.Instance | BindingFlags.NonPublic);
            dbioField!.SetValue(req, JqlRun.Instance("DefaultConnection"));
            var dqField = typeof(JqlRequest).GetField("dbQuery", BindingFlags.Instance | BindingFlags.NonPublic);

			// Replace the JqlQuery instantiation to provide required constructor arguments
			var jqlQuery = new JqlQuery("Dummy.Conf.Object.ReadList", QueryType.ReadList)
			{
				PaginationMaxSize = 7
			};
			dqField!.SetValue(req, jqlQuery);

            var mi = typeof(JqlRequest).GetMethod("CompilePagination", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);
            var s = (string)mi!.Invoke(req, null)!;
            Assert.Contains("7", s);
        }
    }
}
