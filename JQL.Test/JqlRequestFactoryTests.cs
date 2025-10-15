using System.Text.Json;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRequestFactoryTests
    {
        [Fact]
        public void GetInstanceByQueryText_Basic_Works()
        {
            var json = "{\"QueryFullName\":\"DefaultConnection.Users.ReadList\"}";
            var req = JqlRequest.GetInstanceByQueryText(json);
            Assert.NotNull(req);
            Assert.Equal("DefaultConnection.Users.ReadList", req.QueryFullName);
        }

        [Fact]
        public void GetInstanceByQueryJson_Basic_Works()
        {
            using var doc = JsonDocument.Parse("{\"QueryFullName\":\"DefaultConnection.Users.ReadList\"}");
            var req = JqlRequest.GetInstanceByQueryJson(doc.RootElement);
            Assert.NotNull(req);
            Assert.Equal("DefaultConnection.Users.ReadList", req.QueryFullName);
        }

        [Fact]
        public void GetInstanceByQueryObject_Copies_Client_Options()
        {
            var client = new JqlRequest
            {
                QueryFullName = "DefaultConnection.Users.ReadList",
                ColumnsContainment = Containment.IncludeIndicatedItems,
                ClientIndicatedColumns = new System.Collections.Generic.List<string> { "Id" },
                AggregationsContainment = Containment.ExcludeAll,
                RelationsContainment = Containment.ExcludeAll
            };
            var req = JqlRequest.GetInstanceByQueryObject(client);
            Assert.Equal(Containment.IncludeIndicatedItems, req.ColumnsContainment);
            Assert.NotNull(req.ClientIndicatedColumns);
            Assert.Equal(Containment.ExcludeAll, req.AggregationsContainment);
            Assert.Equal(Containment.ExcludeAll, req.RelationsContainment);
        }
    }
}
