using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlUtilsUnitTests
    {
        [Fact]
        public void GetTypeSize_Uppercases_And_Appends_Size()
        {
            Assert.Equal("INT", JqlUtils.GetTypeSize("int", null));
            Assert.Equal("NVARCHAR(50)", JqlUtils.GetTypeSize("nvarchar", 50));
            Assert.Equal("DECIMAL(18,2)", JqlUtils.GetTypeSize("decimal", "18,2"));
        }

        [Fact]
        public void GenParamName_With_And_Without_Index()
        {
            Assert.Equal("Users_Name", JqlUtils.GenParamName("Users", "Name", null));
            Assert.Equal("Users_Name_2", JqlUtils.GenParamName("Users", "Name", 2));
        }

        [Fact]
        public void ColumnIsForDelete_Respects_Skip_Suffix_And_Others()
        {
            var c1 = new JqlColumn("Picture_xs") { DbType = "NVARCHAR" };
            Assert.False(JqlUtils.SuggestedForDelete(c1));

            var c2 = new JqlColumn("SomeXml") { DbType = "NVARCHAR" };
            Assert.False(JqlUtils.SuggestedForDelete(c2));

            var c3 = new JqlColumn("HtmlBody") { DbType = "NVARCHAR" };
            Assert.False(JqlUtils.SuggestedForDelete(c3));

            var c4 = new JqlColumn("Pwd") { DbType = "NVARCHAR" };
            Assert.True(JqlUtils.SuggestedForDelete(c4));
        }

        [Fact]
        public void ColumnIsForAggregatedReadList_Filters_By_Rules()
        {
            var pk = new JqlColumn("Id") { IsPrimaryKey = true, DbType = "INT" };
            Assert.False(JqlUtils.SuggestedForAggregatedReadList(pk));

            var name = new JqlColumn("Name") { DbType = "NVARCHAR", Size = "100" };
            Assert.False(JqlUtils.SuggestedForAggregatedReadList(name));

            var bigText = new JqlColumn("Desc") { DbType = "NVARCHAR", Size = "1000" };
            Assert.False(JqlUtils.SuggestedForAggregatedReadList(bigText));

            var smallText = new JqlColumn("Short") { DbType = "NVARCHAR", Size = "50" };
            Assert.True(JqlUtils.SuggestedForAggregatedReadList(smallText));
        }

        [Fact]
        public void ColumnsAreFileCentric_True_When_Image_And_Few_Columns()
        {
            var cols = new List<JqlColumn>
            {
                new JqlColumn("Img") { DbType = "IMAGE" },
                new JqlColumn("Name") { DbType = "NVARCHAR", Size = "100" }
            };
            Assert.True(JqlUtils.ColumnsAreFileCentric(cols));

            // Many columns should flip to false
            for (int i = 0; i < 10; i++) cols.Add(new JqlColumn($"C{i}") { DbType = "INT" });
            Assert.False(JqlUtils.ColumnsAreFileCentric(cols));
        }

        [Fact]
        public void RemoveAuditingColumns_Removes_Known_Ones()
        {
            var cols = new List<JqlColumn>
            {
                new JqlColumn("CreatedBy") { DbType = "INT" },
                new JqlColumn("CreatedOn") { DbType = "DATETIME" },
                new JqlColumn("UpdatedBy") { DbType = "INT" },
                new JqlColumn("UpdatedOn") { DbType = "DATETIME" },
                new JqlColumn("Name") { DbType = "NVARCHAR", Size = "50" }
            };
            var filtered = JqlUtils.RemoveAuditingColumns(cols);
            Assert.DoesNotContain(filtered, c => c.Name == "CreatedBy");
            Assert.DoesNotContain(filtered, c => c.Name == "CreatedOn");
            Assert.DoesNotContain(filtered, c => c.Name == "UpdatedBy");
            Assert.DoesNotContain(filtered, c => c.Name == "UpdatedOn");
            Assert.Contains(filtered, c => c.Name == "Name");
        }
    }
}
