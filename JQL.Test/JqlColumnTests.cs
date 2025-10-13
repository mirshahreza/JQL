using System;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class JqlColumnTests
    {
        [Fact]
        public void CalculateBestUiWidget_ReturnsExpectedWidgets()
        {
            Assert.Equal(UiWidget.NoWidget, new JqlColumn("Id") { IsIdentity = true }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.DisabledTextbox, new JqlColumn(JqlUtils.CreatedOn) { DbType = "DATETIME" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.Combo, new JqlColumn("CatId") { Fk = new JqlFk("FK", "Cat", "Id") }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.Checkbox, new JqlColumn("IsActive") { DbType = "bit" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.ImageView, new JqlColumn("picture_xs") { DbType = "image" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.FileView, new JqlColumn("file") { DbType = "image" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.DateTimePicker, new JqlColumn("d") { DbType = "datetime2" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.DatePicker, new JqlColumn("d") { DbType = "date" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.TimePicker, new JqlColumn("t") { DbType = "time" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.Htmlbox, new JqlColumn("html_note") { DbType = "nvarchar", Size = "100" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.MultilineTextbox, new JqlColumn("txt") { DbType = "text" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.MultilineTextbox, new JqlColumn("ntxt") { DbType = "ntext" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.MultilineTextbox, new JqlColumn("desc") { DbType = "nvarchar", Size = "200" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.Textbox, new JqlColumn("qty") { DbType = "int" }.CalculateBestUiWidget());
            Assert.Equal(UiWidget.Textbox, new JqlColumn("name") { DbType = "nvarchar", Size = "50" }.CalculateBestUiWidget());
        }

        [Fact]
        public void CalculateIsDisabled_ReturnsTrueForIdentityOrAuditing()
        {
            Assert.True(new JqlColumn("Id") { IsIdentity = true }.CalculateIsDisabled());
            Assert.True(new JqlColumn(JqlUtils.CreatedOn) { DbType = "DATETIME" }.CalculateIsDisabled());
            Assert.False(new JqlColumn("Name") { DbType = "NVARCHAR", Size = "50" }.CalculateIsDisabled());
        }

        [Fact]
        public void IsLargeContent_DetectsLargeTypes()
        {
            Assert.True(new JqlColumn("C1") { DbType = "text" }.IsLargeContent());
            Assert.True(new JqlColumn("C2") { DbType = "ntext" }.IsLargeContent());
            Assert.True(new JqlColumn("C3") { DbType = "nvarchar", Size = "513" }.IsLargeContent());
            Assert.True(new JqlColumn("C4") { DbType = "image" }.IsLargeContent());
            Assert.False(new JqlColumn("C5_xs") { DbType = "image" }.IsLargeContent());
            Assert.False(new JqlColumn("C6") { DbType = "nvarchar", Size = "100" }.IsLargeContent());
        }

        [Fact]
        public void ToChangeTrackable_CopiesRelevantProperties()
        {
            var c = new JqlColumn("Name")
            {
                DevNote = "d",
                IsPrimaryKey = false,
                DbType = "NVARCHAR",
                Size = "50",
                IsIdentity = false,
                IdentityStart = null,
                IdentityStep = null,
                AllowNull = true,
                DbDefault = "'x'",
                Fk = new JqlFk("FK", "T", "Id"),
                IsHumanId = true,
            };

            var t = c.ToChangeTrackable();

            Assert.Equal(c.Name, t.Name);
            Assert.Equal(c.DbType, t.DbType);
            Assert.Equal(c.Size, t.Size);
            Assert.Equal(c.AllowNull, t.AllowNull);
            Assert.Equal(c.DbDefault, t.DbDefault);
            Assert.Equal(c.IsIdentity, t.IsIdentity);
            Assert.Equal(c.IdentityStart, t.IdentityStart);
            Assert.Equal(c.IdentityStep, t.IdentityStep);
            Assert.Equal(c.IsPrimaryKey, t.IsPrimaryKey);
            Assert.NotNull(t.Fk);
            Assert.Equal("", t.InitialName);
            Assert.Equal("", t.State);
        }
    }
}
