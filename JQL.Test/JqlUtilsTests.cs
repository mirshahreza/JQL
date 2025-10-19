using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlUtilsTests
    {
        [Fact]
        public void GenParamName_And_SetPair_Work_AsExpected()
        {
            var p = JqlUtils.GenParamName("Users", "Id", null);
            Assert.Equal("Users_Id", p);

            var setPair = JqlUtils.GetSetColumnParamPair("Users", "Id", null);
            Assert.Equal("[Users].[Id]=@Users_Id", setPair);
        }

        [Fact]
        public void GetTypeSize_Uppercases_And_Appends_Size()
        {
            Assert.Equal("NVARCHAR(50)", JqlUtils.GetTypeSize("nvarchar", 50));
            Assert.Equal("INT", JqlUtils.GetTypeSize("int", null));
        }

        [Fact]
        public void ColumnSelectionRules_Work_AsExpected()
        {
            var file = new JqlColumn("picture") { DbType = "image" };
            var password = new JqlColumn("PasswordHash") { DbType = "nvarchar", Size = "128" };
            var regular = new JqlColumn("Name") { DbType = "nvarchar", Size = "50" };
            var bigText = new JqlColumn("Description") { DbType = "nvarchar", Size = "300" };
            var createBlocked = new JqlColumn("C") { DbType = "int", DbDefault = "1" };

            Assert.False(file.SuggestedForReadList());
            Assert.False(password.SuggestedForReadList());
            Assert.True(regular.SuggestedForReadList());

            Assert.False(bigText.SuggestedForAggregatedReadList());
            Assert.False(file.SuggestedForAggregatedReadList());

            Assert.False(createBlocked.SuggestedForCreate());
            Assert.True(regular.SuggestedForCreate());
        }

        [Fact]
        public void ColumnsAreFileCentric_And_RemoveAuditingColumns()
        {
            var cols = new List<JqlColumn>
            {
                new("Id") { DbType = "int", IsPrimaryKey = true },
                new(JqlUtils.CreatedOn) { DbType = "datetime" },
                new("picture_xs") { DbType = "image" },
            };

            Assert.True(JqlUtils.ColumnsAreFileCentric(cols));

            var cleaned = JqlUtils.RemoveAuditingColumns(cols);
            Assert.DoesNotContain(cleaned, c => c.Name == JqlUtils.CreatedOn);
        }
    }
}
