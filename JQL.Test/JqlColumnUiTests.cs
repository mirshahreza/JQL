using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlColumnUiTests
    {
        [Fact]
        public void CalculateBestUiWidget_And_IsDisabled_Work()
        {
            var identity = new JqlColumn("Id") { DbType = "int", IsIdentity = true, IsPrimaryKey = true };
            Assert.Equal(UiWidget.NoWidget, identity.CalculateBestUiWidget());
            Assert.True(identity.CalculateIsDisabled());

            var audit = new JqlColumn("CreatedOn") { DbType = "datetime" };
            Assert.Equal(UiWidget.DisabledTextbox, audit.CalculateBestUiWidget());
            Assert.True(audit.CalculateIsDisabled());

            var fk = new JqlColumn("RoleId") { DbType = "int", Fk = new JqlFk("FK","Role","Id") };
            Assert.Equal(UiWidget.Combo, fk.CalculateBestUiWidget());

            var bit = new JqlColumn("IsActive") { DbType = "bit" };
            Assert.Equal(UiWidget.Checkbox, bit.CalculateBestUiWidget());

            var picture = new JqlColumn("pictureAvatar") { DbType = "image" };
            Assert.Equal(UiWidget.ImageView, picture.CalculateBestUiWidget());

            var file = new JqlColumn("Attachment") { DbType = "image" };
            Assert.Equal(UiWidget.FileView, file.CalculateBestUiWidget());

            var dt = new JqlColumn("When") { DbType = "datetime" };
            Assert.Equal(UiWidget.DateTimePicker, dt.CalculateBestUiWidget());

            var d = new JqlColumn("Birth") { DbType = "date" };
            Assert.Equal(UiWidget.DatePicker, d.CalculateBestUiWidget());

            var t = new JqlColumn("At") { DbType = "time" };
            Assert.Equal(UiWidget.TimePicker, t.CalculateBestUiWidget());

            var html = new JqlColumn("DescHtml") { DbType = "nvarchar", Size = "300" };
            Assert.Equal(UiWidget.Htmlbox, html.CalculateBestUiWidget());

            var ml = new JqlColumn("Notes") { DbType = "nvarchar", Size = "512" };
            Assert.Equal(UiWidget.MultilineTextbox, ml.CalculateBestUiWidget());

            var num = new JqlColumn("Amount") { DbType = "decimal" };
            Assert.Equal(UiWidget.Textbox, num.CalculateBestUiWidget());
        }

        [Fact]
        public void TypePredicates_Work()
        {
            var s1 = new JqlColumn("C1") { DbType = "nvarchar", Size = "50" };
            var d = new JqlColumn("D1") { DbType = "date" };
            var dt = new JqlColumn("DT1") { DbType = "datetime" };
            var img = new JqlColumn("Picture") { DbType = "image" };

            Assert.True(s1.IsString());
            Assert.True(d.IsDate());
            Assert.True(dt.IsDateTime());
            Assert.True(img.IsLargeContent());
        }
    }
}
