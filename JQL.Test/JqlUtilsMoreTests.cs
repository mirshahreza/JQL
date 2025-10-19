using System.Collections.Generic;
using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlUtilsMoreTests
    {
        [Fact]
        public void GenParamName_And_GetSetPair_Work()
        {
            Assert.Equal("Users_Name", JqlUtils.GenParamName("Users","Name", null));
            Assert.Equal("Users_Name_2", JqlUtils.GenParamName("Users","Name", 2));
            Assert.Equal("[Users].[Id]=@Users_Id", JqlUtils.GetSetColumnParamPair("Users","Id", null));
            Assert.Equal("[Users].[Id]=@Users_Id_3", JqlUtils.GetSetColumnParamPair("Users","Id", 3));
        }

        [Fact]
        public void GetTypeSize_Normalizes_Casing_And_Size()
        {
            Assert.Equal("NVARCHAR(50)", JqlUtils.GetTypeSize("nvarchar", 50));
            Assert.Equal("INT", JqlUtils.GetTypeSize("int", null));
        }

        [Fact]
        public void Column_Include_Exclude_Rules_Work()
        {
            var pk = new JqlColumn("Id") { DbType = "int", IsPrimaryKey = true };
            var img = new JqlColumn("Picture") { DbType = "image" };
            var pass = new JqlColumn("PasswordHash") { DbType = "nvarchar", Size = "256" };
            var bigText = new JqlColumn("Html") { DbType = "nvarchar", Size = "1024" };
            var num = new JqlColumn("Amount") { DbType = "decimal" };

            Assert.False(pk.SuggestedForAggregatedReadList());
            Assert.False(img.SuggestedForAggregatedReadList());
            Assert.False(pass.SuggestedForAggregatedReadList());
            Assert.False(bigText.SuggestedForAggregatedReadList());
            Assert.True(num.SuggestedForAggregatedReadList());

            Assert.False(img.SuggestedForReadList());
            Assert.False(pass.SuggestedForReadList());
            Assert.True(num.SuggestedForReadList());

            Assert.False(new JqlColumn("File_xs") { DbType = "image" }.SuggestedForDelete());
        }

        [Fact]
        public void ColumnsAreFileCentric_And_RemoveAuditingColumns_Work()
        {
            var cols = new List<JqlColumn>
            {
                new("Id") { DbType = "int", IsPrimaryKey = true },
                new("CreatedOn") { DbType = "datetime" },
                new("Picture") { DbType = "image" }
            };

            Assert.True(JqlUtils.ColumnsAreFileCentric(cols));

            var filtered = cols.RemoveAuditingColumns();
            Assert.DoesNotContain(filtered, c => c.Name == JqlUtils.CreatedOn);
        }

        [Fact]
        public void ColumnIsForCreate_Excludes_Identity_Default_And_Password()
        {
            var idCol = new JqlColumn("Id") { DbType = "INT", IsIdentity = true };
            Assert.False(JqlUtils.SuggestedForCreate(idCol));

            var defCol = new JqlColumn("CreatedOn") { DbType = "DATETIME", DbDefault = "GETDATE()" };
            Assert.False(JqlUtils.SuggestedForCreate(defCol));

            var passCol = new JqlColumn("UserPassword") { DbType = "NVARCHAR" };
            Assert.False(JqlUtils.SuggestedForCreate(passCol));

            var okCol = new JqlColumn("Name") { DbType = "NVARCHAR" };
            Assert.True(JqlUtils.SuggestedForCreate(okCol));
        }

        [Fact]
        public void ColumnIsForUpdateByKey_Excludes_CreatedFields_And_Password()
        {
            var createdBy = new JqlColumn("CreatedBy") { DbType = "INT" };
            Assert.False(JqlUtils.SuggestedForUpdateByKey(createdBy));

            var passCol = new JqlColumn("PasswordHash") { DbType = "NVARCHAR" };
            Assert.False(JqlUtils.SuggestedForUpdateByKey(passCol));

            var ok = new JqlColumn("Title") { DbType = "NVARCHAR" };
            Assert.True(JqlUtils.SuggestedForUpdateByKey(ok));
        }

        [Fact]
        public void ColumnIsForReadList_Allows_Normal_Text_Columns()
        {
            var ok = new JqlColumn("Name") { DbType = "NVARCHAR" };
            Assert.True(JqlUtils.SuggestedForReadList(ok));
        }

        [Fact]
        public void GetSetColumnParamPair_Formats_Correctly()
        {
            var s = JqlUtils.GetSetColumnParamPair("Users", "Name", 3);
            Assert.Equal("[Users].[Name]=@Users_Name_3", s);
        }
    }
}
