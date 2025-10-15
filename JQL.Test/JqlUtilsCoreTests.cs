using System.Collections.Generic;
using JQL;
using PowNet.Common;
using PowNet.Extensions;
using Xunit;

namespace JQL.Test
{
    public class JqlUtilsCoreTests
    {
        [Fact]
        public void GetTypeSize_Builds_Type_And_Size_Correctly()
        {
            Assert.Equal("NVARCHAR(50)", JqlUtils.GetTypeSize("nvarchar", 50));
            Assert.Equal("NVARCHAR(50)", JqlUtils.GetTypeSize("nvarchar", "50"));
            Assert.Equal("INT", JqlUtils.GetTypeSize("int", null));
        }

        [Fact]
        public void GenParamName_And_GetSetColumnParamPair_Work()
        {
            Assert.Equal("Users_Name", JqlUtils.GenParamName("Users", "Name", null));
            Assert.Equal("Users_Name_2", JqlUtils.GenParamName("Users", "Name", 2));
            Assert.Equal("[Users].[Name]=@Users_Name_3", JqlUtils.GetSetColumnParamPair("Users", "Name", 3));
        }

        [Fact]
        public void ColumnsAreFileCentric_Returns_True_For_Small_Image_Tables()
        {
            var cols1 = new List<JqlColumn>
            {
                new("Id") { DbType = "INT", IsPrimaryKey = true },
                new("Blob") { DbType = "IMAGE" },
                new("Note") { DbType = "NVARCHAR", Size = "100" }
            };
            Assert.True(JqlUtils.ColumnsAreFileCentric(cols1));

            var cols2 = new List<JqlColumn>
            {
                new("Id") { DbType = "INT", IsPrimaryKey = true },
                new("Blob") { DbType = "BINARY" },
                new("C1") { DbType = "NVARCHAR", Size = "50" },
                new("C2") { DbType = "NVARCHAR", Size = "50" },
                new("C3") { DbType = "NVARCHAR", Size = "50" },
                new("C4") { DbType = "NVARCHAR", Size = "50" },
                new("C5") { DbType = "NVARCHAR", Size = "50" },
                new("C6") { DbType = "NVARCHAR", Size = "50" },
                new("C7") { DbType = "NVARCHAR", Size = "50" }
            };
            Assert.False(JqlUtils.ColumnsAreFileCentric(cols2));
        }

        [Fact]
        public void RemoveAuditingColumns_Removes_Known_Auditing_Names()
        {
            var cols = new List<JqlColumn>
            {
                new("Id") { DbType = "INT", IsPrimaryKey = true },
                new(JqlUtils.CreatedBy) { DbType = "INT" },
                new(JqlUtils.CreatedOn) { DbType = "DATETIME" },
                new(JqlUtils.UpdatedBy) { DbType = "INT" },
                new(JqlUtils.UpdatedOn) { DbType = "DATETIME" },
                new("Name") { DbType = "NVARCHAR", Size = "50" }
            };
            var cleaned = cols.RemoveAuditingColumns();
            Assert.DoesNotContain(cleaned, c => c.Name.EqualsIgnoreCase(JqlUtils.CreatedBy));
            Assert.DoesNotContain(cleaned, c => c.Name.EqualsIgnoreCase(JqlUtils.CreatedOn));
            Assert.DoesNotContain(cleaned, c => c.Name.EqualsIgnoreCase(JqlUtils.UpdatedBy));
            Assert.DoesNotContain(cleaned, c => c.Name.EqualsIgnoreCase(JqlUtils.UpdatedOn));
            Assert.Contains(cleaned, c => c.Name.EqualsIgnoreCase("Id"));
            Assert.Contains(cleaned, c => c.Name.EqualsIgnoreCase("Name"));
        }
    }
}
