using System;
using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlRequestErrorsTests
    {
        [Fact]
        public void DeleteByKey_WithoutPkParam_ShouldThrow()
        {
            var ex = Assert.Throws<Exception>(() => JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.DeleteByKey"));
            Assert.Contains("RequestedQuery", ex.Message);
        }

        [Fact]
        public void GetInstanceByQueryText_Invalid_Json_ShouldThrow()
        {
            string invalid = "{ not-json }";
            var ex = Assert.Throws<Exception>(() => JqlRequest.GetInstanceByQueryText(invalid));
            Assert.Contains("DeserializeError", ex.Message);
        }
    }
}
