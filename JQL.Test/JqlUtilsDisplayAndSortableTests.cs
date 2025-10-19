using JQL;
using Xunit;

namespace JQL.Test
{
    public class JqlUtilsDisplayAndSortableTests
    {
        [Fact]
        public void ColumnIsForDisplay_Known_Display_Columns_Return_True()
        {
            Assert.True(new JqlColumn("Name") { DbType = "NVARCHAR" }.SuggestedForDisplay());
            Assert.True(new JqlColumn("Title") { DbType = "NVARCHAR" }.SuggestedForDisplay());
            Assert.True(new JqlColumn("FirstName") { DbType = "NVARCHAR" }.SuggestedForDisplay());
            Assert.False(new JqlColumn("UnknownX") { DbType = "NVARCHAR" }.SuggestedForDisplay());
        }

        [Fact]
        public void ColumnIsSortable_Known_Sortable_Columns_Return_True()
        {
            Assert.True(new JqlColumn("Name") { DbType = "NVARCHAR" }.ColumnIsSortable());
            Assert.True(new JqlColumn("Title") { DbType = "NVARCHAR" }.ColumnIsSortable());
            Assert.True(new JqlColumn("CreatedOn") { DbType = "DATETIME" }.ColumnIsSortable());
            Assert.True(new JqlColumn("UpdatedOn") { DbType = "DATETIME" }.ColumnIsSortable());
            Assert.False(new JqlColumn("Blob") { DbType = "IMAGE" }.ColumnIsSortable());
        }
    }
}
