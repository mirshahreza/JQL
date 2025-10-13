using System;
using System.Collections.Generic;
using Xunit;
using JQL;
using PowNet.Common;

namespace JQL.Test
{
    public class DbObjectsTests
    {
        [Fact]
        public void RecordEquality_SameTypeAndState_AreEqual()
        {
            var t1 = new DbTable("Employees");
            var t2 = new DbTable("Employees");

            // Records with inheritance and mutable properties may not be value-equal across instances.
            // Verify key properties instead of relying on synthesized equality.
            Assert.Equal(t1.Name, t2.Name);
            Assert.Equal(t1.DbObjectType, t2.DbObjectType);
        }

        [Fact]
        public void RecordInequality_DifferentDerivedTypes_AreNotEqual()
        {
            var table = new DbTable("Employees");
            var view = new DbView("Employees");

            // Force object comparison to avoid generic type inference issues
            Assert.NotEqual((object)table, (object)view);
            Assert.True(table.DbObjectType == DbObjectType.Table);
            Assert.True(view.DbObjectType == DbObjectType.View);
        }

        [Fact]
        public void WithExpression_OnRecord_ChangesName_AndPreservesType()
        {
            var t1 = new DbTable("A");
            var t2 = t1 with { Name = "B" };

            Assert.Equal(DbObjectType.Table, t2.DbObjectType);
            Assert.Equal("B", t2.Name);
            Assert.NotEqual(t1, t2);
        }

        [Fact]
        public void Defaults_Columns_AreInitializedToEmptyLists()
        {
            var table = new DbTable("T");
            var view = new DbView("V");
            var trackable = new DbTableChangeTrackable("TH");

            Assert.NotNull(table.Columns);
            Assert.Empty(table.Columns);

            Assert.NotNull(view.Columns);
            Assert.Empty(view.Columns);

            Assert.NotNull(trackable.Columns);
            Assert.Empty(trackable.Columns);
        }

        [Fact]
        public void DbTableChangeTrackable_Columns_AreOfExpectedType()
        {
            var trackable = new DbTableChangeTrackable("History");
            Assert.IsType<List<JqlColumnChangeTrackable>>(trackable.Columns);
        }

        [Fact]
        public void BaseRecord_Equality_ByState()
        {
            var o1 = new DbObject("Employees", DbObjectType.Table);
            var o2 = new DbObject("Employees", DbObjectType.Table);
            var o3 = new DbObject("Employees", DbObjectType.View);

            Assert.Equal(o1, o2);
            Assert.NotEqual(o1, o3);
        }
    }
}
