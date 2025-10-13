using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlRelationTests
    {
        [Fact]
        public void Constructor_Sets_Defaults()
        {
            var r = new JqlRelation("Orders","Id","CustomerId")
            {
                RelationType = RelationType.OneToMany,
                CreateQuery = "Create",
                ReadListQuery = "ReadList",
                UpdateByKeyQuery = "UpdateByKey",
                DeleteQuery = "Delete",
                DeleteByKeyQuery = "DeleteByKey",
                IsFileCentric = false,
                RelationUiWidget = RelationUiWidget.Grid,
                MinN = "0",
                MaxN = "*"
            };

            Assert.Equal("To_Orders_On_CustomerId", r.RelationName);
            Assert.Equal("Orders", r.RelationTable);
            Assert.Equal("Id", r.RelationPkColumn);
            Assert.Equal("CustomerId", r.RelationFkColumn);
        }
    }
}
