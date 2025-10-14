using System;
using JQL;
using Xunit;

namespace JQL.Test
{
    public class DbSchemaUtilsUnitTests
    {
        [Fact]
        public void GetTableViewColumns_Throws_On_NullOrEmpty()
        {
            var utils = new DbSchemaUtils("DefaultConnection");
            Assert.Throws<System.Exception>(() => utils.GetTableViewColumns(null!));
            Assert.Throws<System.Exception>(() => utils.GetTableViewColumns(""));
        }

        [Fact]
        public void Constructor_And_DbIOInstance_NotNull()
        {
            var utils = new DbSchemaUtils("DefaultConnection");
            var io = utils.DbIOInstance;
            Assert.NotNull(io);
        }
    }
}
