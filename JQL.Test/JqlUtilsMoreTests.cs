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

            Assert.False(pk.ColumnIsForAggregatedReadList());
            Assert.False(img.ColumnIsForAggregatedReadList());
            Assert.False(pass.ColumnIsForAggregatedReadList());
            Assert.False(bigText.ColumnIsForAggregatedReadList());
            Assert.True(num.ColumnIsForAggregatedReadList());

            Assert.False(img.ColumnIsForReadList());
            Assert.False(pass.ColumnIsForReadList());
            Assert.True(num.ColumnIsForReadList());

            Assert.False(new JqlColumn("File_xs") { DbType = "image" }.ColumnIsForDelete());
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
    }
}
