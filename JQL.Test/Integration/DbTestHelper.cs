using System;
using System.Collections.Generic;
using System.Linq;
using JQL;
using PowNet.Common;
using PowNet.Extensions;

namespace JQL.Test.Integration
{
    internal static class DbTestHelper
    {
        public const string Master = "ITEST_Master";
        public const string Detail = "ITEST_Detail";
        public const string History = "ITEST_MasterHistory";
        public const string Tag = "ITEST_Tag";
        public const string MasterTag = "ITEST_MasterTag";

        public static void EnsureProvisioned(string connectionName)
        {
            var schema = new DbSchemaUtils(connectionName);

            // Create/ensure Master
            var masterTable = new DbTableChangeTrackable(Master)
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
                    new JqlColumnChangeTrackable("Name") { DbType = "NVARCHAR", Size = "50", AllowNull = true },
                    new JqlColumnChangeTrackable("IsActive") { DbType = "BIT", AllowNull = true },
                    new JqlColumnChangeTrackable("CreatedBy") { DbType = "INT", AllowNull = true },
                    new JqlColumnChangeTrackable("UpdatedBy") { DbType = "INT", AllowNull = true },
                    new JqlColumnChangeTrackable("UpdatedOn") { DbType = "DATETIME", AllowNull = true },
                    new JqlColumnChangeTrackable("CreatedOn") { DbType = "DATETIME", AllowNull = true },
                    new JqlColumnChangeTrackable("Picture") { DbType = "IMAGE", AllowNull = true },
                    new JqlColumnChangeTrackable("Picture_xs") { DbType = "IMAGE", AllowNull = true }
                ]
            };
            schema.CreateOrAlterTable(masterTable);

            // Create/ensure Detail with FK to Master
            var detailTable = new DbTableChangeTrackable(Detail)
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
                    new JqlColumnChangeTrackable("MasterId") { DbType = "INT", AllowNull = false, Fk = new JqlFk("FK_ITEST_Detail_Master","ITEST_Master","Id") { EnforceRelation = false } },
                    new JqlColumnChangeTrackable("Title") { DbType = "NVARCHAR", Size = "100", AllowNull = true }
                ]
            };
            schema.CreateOrAlterTable(detailTable);

            // Create/ensure Tag
            var tagTable = new DbTableChangeTrackable(Tag)
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
                    new JqlColumnChangeTrackable("Title") { DbType = "NVARCHAR", Size = "50", AllowNull = true }
                ]
            };
            schema.CreateOrAlterTable(tagTable);

            // Create/ensure Many-To-Many (exactly 3 columns -> detected automatically)
            var mtmTable = new DbTableChangeTrackable(MasterTag)
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
                    new JqlColumnChangeTrackable("MasterId") { DbType = "INT", AllowNull = false, Fk = new JqlFk("FK_ITEST_MasterTag_Master","ITEST_Master","Id") { EnforceRelation = false } },
                    new JqlColumnChangeTrackable("TagId") { DbType = "INT", AllowNull = false, Fk = new JqlFk("FK_ITEST_MasterTag_Tag","ITEST_Tag","Id") { EnforceRelation = false } }
                ]
            };
            schema.CreateOrAlterTable(mtmTable);

            // Generate models and default queries
            var factory = new JqlModelFactory(connectionName);
            factory.CreateServerObjectsFor(new DbObject(Master, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(Detail, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(Tag, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(MasterTag, DbObjectType.Table));
        }

        public static int SeedMaster(string connectionName, string name, bool isActive = true)
        {
            var req = JqlRequest.GetInstanceByQueryName($"{connectionName}.{Master}.Create");
            req.Params = [ new JqlParamRaw("Name", name), new JqlParamRaw("IsActive", isActive) ];
            var idObj = req.Exec();
            return Convert.ToInt32(idObj);
        }

        public static void SeedDetail(string connectionName, int masterId, string title)
        {
            var req = JqlRequest.GetInstanceByQueryName($"{connectionName}.{Detail}.Create");
            req.Params = [ new JqlParamRaw("MasterId", masterId), new JqlParamRaw("Title", title) ];
            req.Exec();
        }

        public static int SeedTag(string connectionName, string title)
        {
            var req = JqlRequest.GetInstanceByQueryName($"{connectionName}.{Tag}.Create");
            req.Params = [ new JqlParamRaw("Title", title) ];
            var idObj = req.Exec();
            return Convert.ToInt32(idObj);
        }

        public static void LinkMasterTag(string connectionName, int masterId, int tagId)
        {
            var req = JqlRequest.GetInstanceByQueryName($"{connectionName}.{MasterTag}.Create");
            req.Params = [ new JqlParamRaw("MasterId", masterId), new JqlParamRaw("TagId", tagId) ];
            req.Exec();
        }
    }
}
