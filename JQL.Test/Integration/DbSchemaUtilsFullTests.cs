using System;
using System.Linq;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class DbSchemaUtilsFullTests
    {
        private readonly RealDbFixture _fx;
        public DbSchemaUtilsFullTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")]
        public void Metadata_GetObjects_ByType_And_Name_Filters()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            var allTables = utils.GetObjects(DbObjectType.Table);
            Assert.NotNull(allTables);

            var likeMaster = utils.GetObjects(DbObjectType.Table, "ITEST_");
            Assert.Contains(likeMaster, o => o.Name.StartsWith("ITEST_", StringComparison.OrdinalIgnoreCase));

            var exact = utils.GetObjects(DbObjectType.Table, DbTestHelper.Master, exactNameSearch: true);
            Assert.True(exact.Any(o => o.Name.Equals(DbTestHelper.Master, StringComparison.OrdinalIgnoreCase)));
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void All_Supported_Types_CreateOrAlterTable_And_CreateColumn()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            string tSchema = "ITEST_AllTypesA";
            string tDirect = "ITEST_AllTypesB";

            // Define a wide set of SQL Server data types
            var cols = new (string Name, string Type, string? Size)[]
            {
                ("C_INT", "INT", null),
                ("C_BIGINT", "BIGINT", null),
                ("C_SMALLINT", "SMALLINT", null),
                ("C_TINYINT", "TINYINT", null),
                ("C_BIT", "BIT", null),
                ("C_UNIQUEIDENTIFIER", "UNIQUEIDENTIFIER", null),
                ("C_DECIMAL", "DECIMAL", "18,2"),
                ("C_NUMERIC", "NUMERIC", "10,3"),
                ("C_MONEY", "MONEY", null),
                ("C_SMALLMONEY", "SMALLMONEY", null),
                ("C_FLOAT", "FLOAT", null),
                ("C_REAL", "REAL", null),
                ("C_DATE", "DATE", null),
                ("C_TIME", "TIME", null),
                ("C_DATETIME", "DATETIME", null),
                ("C_SMALLDATETIME", "SMALLDATETIME", null),
                ("C_DATETIME2", "DATETIME2", null),
                ("C_DATETIMEOFFSET", "DATETIMEOFFSET", null),
                ("C_CHAR", "CHAR", "10"),
                ("C_NCHAR", "NCHAR", "10"),
                ("C_VARCHAR", "VARCHAR", "50"),
                ("C_NVARCHAR", "NVARCHAR", "50"),
                ("C_VARCHAR_MAX", "VARCHAR", "MAX"),
                ("C_NVARCHAR_MAX", "NVARCHAR", "MAX"),
                ("C_TEXT", "TEXT", null),
                ("C_NTEXT", "NTEXT", null),
                ("C_BINARY", "BINARY", "16"),
                ("C_VARBINARY", "VARBINARY", "50"),
                ("C_VARBINARY_MAX", "VARBINARY", "MAX"),
                ("C_IMAGE", "IMAGE", null),
                ("C_XML", "XML", null)
            };

            // Create with schema change tracking (n)
            var schema = new DbTableChangeTrackable(tSchema)
            {
                Columns = [ new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", DbType = "INT", AllowNull = false } ]
            };
            foreach (var c in cols)
            {
                schema.Columns.Add(new JqlColumnChangeTrackable(c.Name)
                {
                    DbType = c.Type,
                    Size = c.Size,
                    AllowNull = true,
                    State = "n"
                });
            }
            utils.CreateOrAlterTable(schema);

            var metaA = utils.GetTableViewColumns(tSchema).Select(x => x.Name).ToList();
            foreach (var c in cols) Assert.Contains(c.Name, metaA);

            // Create via direct CreateColumn on a fresh table with just PK
            var schemaB = new DbTableChangeTrackable(tDirect)
            {
                Columns = [ new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", DbType = "INT", AllowNull = false } ]
            };
            utils.CreateOrAlterTable(schemaB);

            foreach (var c in cols)
            {
                var typeSize = c.Size is null ? c.Type.ToUpperInvariant() : $"{c.Type.ToUpperInvariant()}({c.Size})";
                utils.CreateColumn(tDirect, c.Name, typeSize, allowNull: true);
            }

            var metaB = utils.GetTableViewColumns(tDirect).Select(x => x.Name).ToList();
            foreach (var c in cols) Assert.Contains(c.Name, metaB);

            utils.DropTable(tSchema);
            utils.DropTable(tDirect);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void RenameColumn_Using_InitialName_Path()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            string t = "ITEST_Rename";

            var schema = new DbTableChangeTrackable(t)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", DbType = "INT", AllowNull = false },
                    new JqlColumnChangeTrackable("C1") { DbType = "INT", AllowNull = true, State = "n" }
                ]
            };
            utils.CreateOrAlterTable(schema);

            // rename C1 -> C2
            var rename = new DbTableChangeTrackable(t)
            {
                Columns = [ new JqlColumnChangeTrackable("C2") { DbType = "INT", AllowNull = true, State = "u", InitialName = "C1" } ]
            };
            utils.CreateOrAlterTable(rename);

            var cols = utils.GetTableViewColumns(t).Select(c => c.Name).ToList();
            Assert.Contains("C2", cols);
            Assert.DoesNotContain("C1", cols);

            utils.DropTable(t);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void Create_Table_With_Guid_Primary_Key()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            string t = "ITEST_GuidPk";

            var schema = new DbTableChangeTrackable(t)
            {
                Columns = [ new JqlColumnChangeTrackable("Id") { IsPrimaryKey = true, DbType = "UNIQUEIDENTIFIER", AllowNull = false } ]
            };
            utils.CreateOrAlterTable(schema);

            var tables = utils.GetObjects(DbObjectType.Table, t, exactNameSearch: true);
            Assert.Contains(tables, o => o.Name == t);

            utils.DropTable(t);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void Direct_CreateColumn_And_DropColumn()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            string table = "ITEST_DCols";

            var schema = new DbTableChangeTrackable(table)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id")
                    {
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        IdentityStart = "1",
                        IdentityStep = "1",
                        DbType = "INT",
                        AllowNull = false
                    }
                ]
            };
            utils.CreateOrAlterTable(schema);

            utils.CreateColumn(table, "C3", "NVARCHAR(20)", allowNull: false);
            // Drop column via change tracking (public API)
            var drop = new DbTableChangeTrackable(table)
            {
                Columns = [ new JqlColumnChangeTrackable("C3") { DbType = "NVARCHAR", Size = "20", State = "d" } ]
            };
            utils.CreateOrAlterTable(drop);

            utils.DropTable(table);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void GetCreateOrAlterObject_For_View_Proc_Scalar_Table_Function()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            utils.CreateEmptyView("ITEST_V_OBJ");
            string vs = utils.GetCreateOrAlterObject("ITEST_V_OBJ");
            Assert.Contains("VIEW", vs, StringComparison.OrdinalIgnoreCase);
            utils.DropView("ITEST_V_OBJ");

            utils.CreateEmptyProcedure("ITEST_P_OBJ");
            string ps = utils.GetCreateOrAlterObject("ITEST_P_OBJ");
            Assert.Contains("PROCEDURE", ps, StringComparison.OrdinalIgnoreCase);
            utils.DropProcedure("ITEST_P_OBJ");

            utils.CreateEmptyScalarFunction("ITEST_SF_OBJ");
            string sfs = utils.GetCreateOrAlterObject("ITEST_SF_OBJ");
            Assert.Contains("FUNCTION", sfs, StringComparison.OrdinalIgnoreCase);
            utils.DropFunction("ITEST_SF_OBJ");

            utils.CreateEmptyTableFunction("ITEST_TF_OBJ");
            string tfs = utils.GetCreateOrAlterObject("ITEST_TF_OBJ");
            Assert.Contains("FUNCTION", tfs, StringComparison.OrdinalIgnoreCase);
            utils.DropFunction("ITEST_TF_OBJ");
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void AlterObjectScript_InvalidSql_Throws()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            var ex = Record.Exception(() => utils.AlterObjectScript("THIS IS NOT VALID SQL"));
            Assert.NotNull(ex);
        }
        [Fact(Skip = "Un-skip to run against real DB")]
        public void GetTables_And_GetTableViewColumns_Return_Metadata()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            string t = "ITEST_MetaA";
            var schema = new DbTableChangeTrackable(t)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id")
                    {
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        IdentityStart = "1",
                        IdentityStep = "1",
                        DbType = "INT",
                        AllowNull = false
                    },
                    new JqlColumnChangeTrackable("N1") { DbType = "NVARCHAR", Size = "50", AllowNull = true, State = "n" },
                    new JqlColumnChangeTrackable("B1") { DbType = "BIT", AllowNull = true, State = "n" }
                ]
            };
            utils.CreateOrAlterTable(schema);

            var tables = utils.GetTables();
            var found = tables.FirstOrDefault(x => x.Name.Equals(t, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(found);
            Assert.True(found!.Columns.Count >= 3);

            var cols = utils.GetTableViewColumns(t);
            Assert.True(cols.Any(c => c.Name == "Id" && c.IsIdentity));

            utils.DropTable(t);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void GetViews_Procedures_TableFunctions_ScalarFunctions_With_Filters()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            // View
            utils.CreateEmptyView("ITEST_V_Filter");
            var viewsLike = utils.GetViews("ITEST_V_");
            var viewsExact = utils.GetViews("ITEST_V_Filter", exactNameSearch: true);
            Assert.Contains(viewsLike, v => v.Name.StartsWith("ITEST_V_", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(viewsExact, v => v.Name == "ITEST_V_Filter");
            utils.DropView("ITEST_V_Filter");

            // Proc
            utils.CreateEmptyProcedure("ITEST_P_Filter");
            var procsLike = utils.GetProcedures("ITEST_P_");
            var procsExact = utils.GetProcedures("ITEST_P_Filter", exactNameSearch: true);
            Assert.Contains(procsLike, p => p.Name.StartsWith("ITEST_P_", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(procsExact, p => p.Name == "ITEST_P_Filter");
            utils.DropProcedure("ITEST_P_Filter");

            // Scalar function
            utils.CreateEmptyScalarFunction("ITEST_SF_Filter");
            var sfsLike = utils.GetScalarFunctions("ITEST_SF_");
            var sfsExact = utils.GetScalarFunctions("ITEST_SF_Filter", exactNameSearch: true);
            Assert.Contains(sfsLike, f => f.Name.StartsWith("ITEST_SF_", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(sfsExact, f => f.Name == "ITEST_SF_Filter");
            utils.DropFunction("ITEST_SF_Filter");

            // Table function
            utils.CreateEmptyTableFunction("ITEST_TF_Filter");
            var tfsLike = utils.GetTableFunctions("ITEST_TF_");
            var tfsExact = utils.GetTableFunctions("ITEST_TF_Filter", exactNameSearch: true);
            Assert.Contains(tfsLike, f => f.Name.StartsWith("ITEST_TF_", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(tfsExact, f => f.Name == "ITEST_TF_Filter");
            utils.DropFunction("ITEST_TF_Filter");
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void CreateOrAlterFk_With_Default_And_Explicit_Name_And_DropFk()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            string parent = "ITEST_FK_P";
            string child = "ITEST_FK_C";

            // Parent
            var pt = new DbTableChangeTrackable(parent)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id")
                    {
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        IdentityStart = "1",
                        IdentityStep = "1",
                        DbType = "INT",
                        AllowNull = false
                    }
                ]
            };
            utils.CreateOrAlterTable(pt);

            // Child with FK (default naming)
            var ct = new DbTableChangeTrackable(child)
            {
                Columns =
                [
                    new JqlColumnChangeTrackable("Id")
                    {
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        IdentityStart = "1",
                        IdentityStep = "1",
                        DbType = "INT",
                        AllowNull = false
                    },
                    new JqlColumnChangeTrackable("ParentId") { DbType = "INT", AllowNull = false, State = "n", Fk = new JqlFk("", parent, "Id") { EnforceRelation = false } }
                ]
            };
            utils.CreateOrAlterTable(ct);

            var fks1 = utils.GetTableFks(child);
            Assert.True(fks1.Rows.Count >= 1);
            string defaultFkName = $"{child}_ParentId_{parent}_Id";
            utils.DropFk(child, defaultFkName);

            // Re-create with explicit name
            ct.Columns[1].Fk = new JqlFk("FK_Explicit_Name", parent, "Id") { EnforceRelation = false };
            utils.CreateOrAlterTable(ct);
            var fks2 = utils.GetTableFks(child);
            Assert.True(fks2.Rows.Count >= 1);
            utils.DropFk(child, "FK_Explicit_Name");

            utils.DropTable(child);
            utils.DropTable(parent);
        }
    }
}
