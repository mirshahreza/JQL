using System;
using System.Linq;
using JQL;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class DbSchemaUtilsIntegrationTests
    {
        private readonly RealDbFixture _fx;
        public DbSchemaUtilsIntegrationTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void GetObjects_Tables_Views_Procs_Functions_Return()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);
            var tables = utils.GetTables();
            var views = utils.GetViews();
            var procs = utils.GetProcedures();
            var tfs = utils.GetTableFunctions();
            var sfs = utils.GetScalarFunctions();

            Assert.NotNull(tables);
            Assert.NotNull(views);
            Assert.NotNull(procs);
            Assert.NotNull(tfs);
            Assert.NotNull(sfs);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void DDL_Create_Alter_Drop_Columns_And_FKs()
        {
            var utils = new DbSchemaUtils(_fx.ConnectionName);

            var tbl = new DbTableChangeTrackable("ITEST_Temp")
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
                    new JqlColumnChangeTrackable("Name") { DbType = "NVARCHAR", Size = "50", AllowNull = true }
                ]
            };
            utils.CreateOrAlterTable(tbl);

            utils.CreateColumn("ITEST_Temp", "X", "INT", true);

            // Alter
            // private method AlterColumn is not accessible; use CreateOrAlterTable update state
            tbl.Columns.Add(new JqlColumnChangeTrackable("Name") { DbType = "NVARCHAR", Size = "100", AllowNull = true, State = "u" });
            utils.CreateOrAlterTable(tbl);

            // Drop col
            var drop = new DbTableChangeTrackable("ITEST_Temp") { Columns = [ new JqlColumnChangeTrackable("X") { DbType = "INT", State = "d" } ] };
            utils.CreateOrAlterTable(drop);

            utils.DropTable("ITEST_Temp");
        }
    }
}
