using PowNet.Common;
using PowNet.Configuration;
using PowNet.Extensions;
using System.Data;

namespace JQL
{
	public class DbSchemaUtils
    {
        public string DbInfoName { init; get; }
        private DatabaseConfiguration DbInfo { init; get; }
        private JqlRun dbIO;
        public JqlRun DbIOInstance
        {
            get
            {
                dbIO ??= JqlRun.Instance(DbInfo);
                return dbIO;
            }
        }
        public DbSchemaUtils(string dbInfoName)
        {
            DbInfoName = dbInfoName;
            DbInfo = DatabaseConfiguration.FromSettings(DbInfoName);
            dbIO = JqlRun.Instance(DbInfo);
        }

        public List<DbTable> GetTables()
        {
            List<DbObject> dbObjects = GetObjects(DbObjectType.Table, null, true);
            List<DbTable> tables = [];
            foreach (DbObject dbObject in dbObjects)
            {
                DbTable dbTable = new(dbObject.Name);
                foreach (JqlColumn dbColumn in GetTableViewColumns(dbObject.Name)) dbTable.Columns.Add(dbColumn.ToChangeTrackable());
                tables.Add(dbTable);
            }
            return tables;
        }

        public List<DbView> GetViews(string? objectName = null, bool? exactNameSearch = false)
        {
            List<DbObject> dbObjects = GetObjects(DbObjectType.View, objectName, exactNameSearch);
            List<DbView> views = [];
            foreach (DbObject dbObject in dbObjects) views.Add(new DbView(dbObject.Name));
            return views;
        }

        public List<DbProcedure> GetProcedures(string? objectName = null, bool? exactNameSearch = false)
        {
            List<DbObject> dbObjects = GetObjects(DbObjectType.Procedure, objectName, exactNameSearch);
            List<DbProcedure> dbProcedures = [];
            foreach (DbObject dbObject in dbObjects) dbProcedures.Add(new DbProcedure(dbObject.Name));
            return dbProcedures;
        }

        public List<DbTableFunction> GetTableFunctions(string? objectName = null, bool? exactNameSearch = false)
        {
            List<DbObject> dbObjects = GetObjects(DbObjectType.TableFunction, objectName, exactNameSearch);
            List<DbTableFunction> dbTableFunctions = [];
            foreach (DbObject dbObject in dbObjects)
            {
                dbTableFunctions.Add(new DbTableFunction(dbObject.Name));
            }
            return dbTableFunctions;
        }
        public List<DbScalarFunction> GetScalarFunctions(string? objectName = null, bool? exactNameSearch = false)
        {
            List<DbObject> dbObjects = GetObjects(DbObjectType.ScalarFunction, objectName, exactNameSearch);
            List<DbScalarFunction> dbScalarFunctions = [];
            foreach (DbObject dbObject in dbObjects)
            {
                dbScalarFunctions.Add(new DbScalarFunction(dbObject.Name));
            }
            return dbScalarFunctions;
        }

        public List<DbObject> GetObjects(DbObjectType? objectType = null, string? objectName = null, bool? exactNameSearch = false)
        {
            string whereObjectType = objectType is null ? "" : $"ObjectType='{objectType}'";
            string whereObjectName = objectName is null ? "" : (exactNameSearch == false ? $"ObjectName LIKE '%{objectName}%'" : $"ObjectName = '{objectName}'");
            string and = "";
            if (whereObjectType != "" && whereObjectName != "") { and = " AND "; }
            string finalWhere = whereObjectType + and + whereObjectName;
            finalWhere = finalWhere.Trim() != "" ? $" WHERE {finalWhere}" : "";
            DataTable dataTable = DbIOInstance.ToDataTables($"SELECT * FROM Zz_SelectObjectsDetails{finalWhere}").FirstOrDefault().Value;
            List<DbObject> objects = [];
            foreach (DataRow row in dataTable.Rows)
            {
                var obj = new DbObject(row["ObjectName"].ToStringEmpty(), (DbObjectType)Enum.Parse(typeof(DbObjectType), row["ObjectType"].ToStringEmpty()));
                objects.Add(obj);
            }
            return objects;
        }
        public List<JqlColumn> GetTableViewColumns(string objectName)
        {
            if(objectName is null || objectName=="") throw new PowNetException("ObjectNameCanNotBeNullOrEmpty", System.Reflection.MethodBase.GetCurrentMethod()).GetEx();

			string where = " WHERE ParentObjectName='" + objectName.ToString() + "'";
            DataTable dataTable = DbIOInstance.ToDataTables("SELECT * FROM Zz_SelectTablesViewsColumns" + where + " ORDER BY ViewOrder").FirstOrDefault().Value;
            DataTable dtFks = GetTableFks(objectName);
            List<JqlColumn> columns = [];
            foreach (DataRow row in dataTable.Rows)
            {
                JqlColumn dbColumn = new((string)row["ColumnName"])
                {
                    IsPrimaryKey = (bool)row["IsPrimaryKey"],
                    DbType = (string)row["ColumnType"],
                    Size = row["MaxLen"] == DBNull.Value ? null : row["MaxLen"].ToString(),
                    AllowNull = (bool)row["AllowNull"],
                    DbDefault = row["DbDefault"] == DBNull.Value ? null : (string)row["DbDefault"],
                    IsIdentity = (bool)row["IsIdentity"],
                    IdentityStart = row["IdentityStart"] == DBNull.Value ? null : row["IdentityStart"].ToString(),
                    IdentityStep = row["IdentityStep"] == DBNull.Value ? null : row["IdentityStep"].ToString()
                };
                foreach (DataRow dataRow in dtFks.Rows)
                {
                    if (dataRow["ColumnName"].ToString() == dbColumn.Name)
	                    dbColumn.Fk = new((string)dataRow["FkName"], (string)dataRow["TargetTable"], (string)dataRow["TargetColumn"])
	                    {
		                    EnforceRelation = (bool)dataRow["EnforceRelation"]
	                    };
                }
                columns.Add(dbColumn);
            }
            return columns;
        }
        public DataTable GetTableFks(string objectName)
        {
            string where = objectName is null ? "" : " WHERE TableName='" + objectName.ToString() + "'";
            return DbIOInstance.ToDataTables("SELECT * FROM Zz_SelectTablesFks" + where).FirstOrDefault().Value;
        }
        public List<JqlParam>? GetProceduresFunctionsParameters(string objectName)
        {
            string where = objectName is null ? "" : " WHERE ObjectName='" + objectName.ToString() + "' AND Direction='Input'";
            DataTable dt = DbIOInstance.ToDataTables($"SELECT * FROM Zz_SelectProceduresFunctionsParameters {where} ORDER BY ViewOrder").FirstOrDefault().Value;
            if (dt.Rows.Count == 0) return null;
            List<JqlParam> dbParams = [];
            foreach (DataRow r in dt.Rows) 
            {
                dbParams.Add
                (
                    new JqlParam(r["ParameterName"].ToStringEmpty().Replace("@", ""), r["ParameterDataType"].ToStringEmpty().ToUpperInvariant())
                    {
                        Size = r["Size"].ToString() == "" ? null : r["Size"].ToString()?.ToUpperInvariant(),
                        AllowNull = false
                    }
                );
            }
            return dbParams;
        }

        public void CreateOrAlterTable(DbTable dbTable)
        {
            JqlColumnChangeTrackable? pkColumn = dbTable.Columns.FirstOrDefault(i => i.IsPrimaryKey == true) ?? throw new PowNetException("PrimaryKeyIsNotExist", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("TableName", dbTable.Name)
                    .GetEx();


			CreateMinTableIfNotExist(dbTable, pkColumn);
            foreach (var f in dbTable.Columns)
            {
                if(!f.IsPrimaryKey)
                {
                    string st = f.State.FixNullOrEmpty("");
                    if (st.Equals("n"))
                    {
                        CreateColumn(dbTable.Name, f.Name, JqlUtils.GetTypeSize(f.DbType, f.Size), f.AllowNull);
                        if (f.Fk is not null) CreateOrAlterFk(dbTable.Name, f);
                    }
                    else if (st.Equals("u"))
                    {
                        if (!f.InitialName.IsNullOrEmpty() && f.InitialName != f.Name) DbIOInstance.ToNonQuery($"EXEC dbo.Zz_RenameColumn '{dbTable.Name}','{f.InitialName}','{f.Name}';");
                        AlterColumn(dbTable.Name, f.Name, JqlUtils.GetTypeSize(f.DbType, f.Size), f.AllowNull, f.DbDefault);
                        if (f.Fk is not null) CreateOrAlterFk(dbTable.Name, f);
                    }
                    else if (st.Equals("d"))
                    {
                        DropColumn(dbTable.Name, f.Name);
                    }
                }
            }
        }

        public string GetCreateOrAlterObject(string objectName)
        {
            return DbIOInstance.ToScalar($"EXEC dbo.Zz_GetCreateOrAlter '{objectName}';").ToStringEmpty();
        }

        public void AlterObjectScript(string objectScript)
        {
            try
            {
                DbIOInstance.ToNonQuery(objectScript);
            }
            catch (Exception ex)
            {
                throw new PowNetException(ex.Message, System.Reflection.MethodBase.GetCurrentMethod()).GetEx();
            }
        }

        private void CreateMinTableIfNotExist(DbTable dbTable, JqlColumnChangeTrackable pk)
        {
            string fn;
            if (pk.DbType.EqualsIgnoreCase("GUID") || pk.DbType.EqualsIgnoreCase("UNIQUEIDENTIFIER"))
            {
                fn = $"EXEC dbo.Zz_CreateTableGuid '{dbTable.Name}','{pk.Name}',1;";
            }
            else
            {
                fn = $"EXEC dbo.Zz_CreateTableIdentity '{dbTable.Name}','{pk.Name}','{pk.DbType}',{pk.IdentityStart.FixNull("1")},{pk.IdentityStep.FixNull("1")},1;";
            }
            DbIOInstance.ToNonQuery(fn);
        }
        public void TruncateTable(string tableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_TruncateTable '{tableName}';");
        }
        public void DropTable(string tableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropTable '{tableName}';");
        }
        public void RenameTable(string tableName,string newTableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_RenameTable '{tableName}','{newTableName}';");
        }

        public void DropView(string viewName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropView '{viewName}';");
        }

        public void DropProcedure(string procedureName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropProcedure '{procedureName}';");
        }

        public void DropFunction(string functionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropFunction '{functionName}';");
        }

        public void CreateEmptyView(string viewName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateEmptyView '{viewName}';");
        }

        public void CreateEmptyProcedure(string procedureName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateEmptyProcedure '{procedureName}';");
        }

        public void CreateEmptyTableFunction(string tableFunctionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateEmptyTableFunction '{tableFunctionName}';");
        }

        public void CreateEmptyScalarFunction(string scalarFunctionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateEmptyScalarFunction '{scalarFunctionName}';");
        }
        
        public void CreateColumn(string tableName, string columnName, string columnTypeSize, bool? allowNull = true)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateColumn '{tableName}','{columnName}','{columnTypeSize}',{(allowNull == true ? "1" : "0")};");
        }
        private void AlterColumn(string tableName, string columnName, string columnTypeSize, bool? allowNull = true, string? DefaultExp = null)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_AlterColumn '{tableName}','{columnName}','{columnTypeSize}',{(allowNull == true ? "1" : "0")},N'{(DefaultExp is null ? "" : DefaultExp)}';");
        }
        private void DropColumn(string tableName, string columnName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropColumn '{tableName}','{columnName}';");
        }
        private void CreateOrAlterFk(string tableName, JqlColumnChangeTrackable tableColumn)
        {
            if (tableColumn.Fk?.FkName == "")
                tableColumn.Fk.FkName = $"{tableName}_{tableColumn.Name}_{tableColumn.Fk.TargetTable}_{tableColumn.Fk.TargetColumn}";
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_CreateOrAlterFk '{tableColumn.Fk?.FkName}','{tableName}','{tableColumn.Name}','{tableColumn.Fk?.TargetTable}','{tableColumn.Fk?.TargetColumn}',{(tableColumn.Fk?.EnforceRelation == true ? "1" : "0")};");
        }
        public void DropFk(string tableName, string fkName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.Zz_DropFk '{fkName}','{tableName}'");
        }


    }
}
