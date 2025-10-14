using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlModelFactoryHelpersTests
    {
        [Fact]
        public void GetClientUIComponentName_Builds_With_ConfName()
        {
            var s1 = JqlModelFactory.GetClientUIComponentName("DefaultConnection", "Users", "List");
            Assert.Equal("DefaultConnection_Users_List", s1);

            Assert.Throws<PowNet.Common.PowNetConfigurationException>(() => JqlModelFactory.GetClientUIComponentName("TenantA", "Users", "List"));
        }

        [Fact]
        public void GetValueSharp_Generators_Return_Correct_Patterns()
        {
            Assert.Equal("#Now", JqlModelFactory.GetValueSharpForNow());
            Assert.Equal("#Context:UserId", JqlModelFactory.GetValueSharpForContext("UserId"));
            Assert.Equal("#Resize:Picture,75", JqlModelFactory.GetValueSharpForImage("Picture_xs"));
        }
    }
}
