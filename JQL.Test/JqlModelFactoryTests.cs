using System;
using Xunit;
using JQL;

namespace JQL.Test
{
    public class JqlModelFactoryTests
    {
        [Fact]
        public void GetValueSharpHelpers_ReturnExpectedFormats()
        {
            Assert.Equal("#Resize:Photo,75", JqlModelFactory.GetValueSharpForImage("Photo_xs"));
            Assert.Equal("#Now", JqlModelFactory.GetValueSharpForNow());
            Assert.Equal("#Context:UserId", JqlModelFactory.GetValueSharpForContext("UserId"));
        }
    }
}
