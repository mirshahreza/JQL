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

        private static string Q(string s) => s.Replace("'", "''");

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
            string whereObjectName = objectName is null ? "" : (exactNameSearch == false ? $"ObjectName LIKE '%{Q(objectName)}%'" : $"ObjectName = '{Q(objectName)}'");
            string and = "";
            if (whereObjectType != "" && whereObjectName != "") { and = " AND "; }
            string finalWhere = whereObjectType + and + whereObjectName;
            finalWhere = finalWhere.Trim() != "" ? $" WHERE {finalWhere}" : "";
            DataTable dataTable = DbIOInstance.ToDataTables($"SELECT * FROM ZzSelectObjectsDetails{finalWhere}").FirstOrDefault().Value;
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

			string where = " WHERE ParentObjectName='" + Q(objectName.ToString()) + "'";
            DataTable dataTable = DbIOInstance.ToDataTables("SELECT * FROM ZzSelectTablesViewsColumns" + where + " ORDER BY ViewOrder").FirstOrDefault().Value;
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
            string where = objectName is null ? "" : " WHERE TableName='" + Q(objectName.ToString()) + "'";
            return DbIOInstance.ToDataTables("SELECT * FROM ZzSelectTablesFks" + where).FirstOrDefault().Value;
        }
        public List<JqlParam>? GetProceduresFunctionsParameters(string objectName)
        {
            string where = objectName is null ? "" : " WHERE ObjectName='" + Q(objectName.ToString()) + "' AND Direction='Input'";
            DataTable dt = DbIOInstance.ToDataTables($"SELECT * FROM ZzSelectProceduresFunctionsParameters {where} ORDER BY ViewOrder").FirstOrDefault().Value;
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

        public void CreateOrAlterTable(DbTableChangeTrackable dbTable)
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
                        if (!f.InitialName.IsNullOrEmpty() && f.InitialName != f.Name) DbIOInstance.ToNonQuery($"EXEC dbo.ZzRenameColumn '{Q(dbTable.Name)}','{Q(f.InitialName)}','{Q(f.Name)}';");
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
            return DbIOInstance.ToScalar($"EXEC dbo.ZzGetCreateOrAlter '{Q(objectName)}';").ToStringEmpty();
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

        private void CreateMinTableIfNotExist(DbTableChangeTrackable dbTable, JqlColumnChangeTrackable pk)
        {
            string fn;
            if (pk.DbType.EqualsIgnoreCase("GUID") || pk.DbType.EqualsIgnoreCase("UNIQUEIDENTIFIER"))
            {
                fn = $"EXEC dbo.ZzCreateTableGuid '{Q(dbTable.Name)}','{Q(pk.Name)}',1;";
            }
            else
            {
                fn = $"EXEC dbo.ZzCreateTableIdentity '{Q(dbTable.Name)}','{Q(pk.Name)}','{Q(pk.DbType)}',{pk.IdentityStart.FixNull("1")},{pk.IdentityStep.FixNull("1")},1;";
            }
            DbIOInstance.ToNonQuery(fn);
        }
        public void TruncateTable(string tableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzTruncateTable '{Q(tableName)}';");
        }
        public void DropTable(string tableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropTable '{Q(tableName)}';");
        }
        public void RenameTable(string tableName,string newTableName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzRenameTable '{Q(tableName)}','{Q(newTableName)}';");
        }

        public void DropView(string viewName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropView '{Q(viewName)}';");
        }

        public void DropProcedure(string procedureName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropProcedure '{Q(procedureName)}';");
        }

        public void DropFunction(string functionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropFunction '{Q(functionName)}';");
        }

        public void CreateEmptyView(string viewName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateEmptyView '{Q(viewName)}';");
        }

        public void CreateEmptyProcedure(string procedureName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateEmptyProcedure '{Q(procedureName)}';");
        }

        public void CreateEmptyTableFunction(string tableFunctionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateEmptyTableFunction '{Q(tableFunctionName)}';");
        }

        public void CreateEmptyScalarFunction(string scalarFunctionName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateEmptyScalarFunction '{Q(scalarFunctionName)}';");
        }
        
        public void CreateColumn(string tableName, string columnName, string columnTypeSize, bool? allowNull = true)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateColumn '{Q(tableName)}','{Q(columnName)}','{Q(columnTypeSize)}',{(allowNull == true ? "1" : "0")};");
        }
        private void AlterColumn(string tableName, string columnName, string columnTypeSize, bool? allowNull = true, string? DefaultExp = null)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzAlterColumn '{Q(tableName)}','{Q(columnName)}','{Q(columnTypeSize)}',{(allowNull == true ? "1" : "0")},N'{(DefaultExp is null ? "" : Q(DefaultExp))}';");
        }
        private void DropColumn(string tableName, string columnName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropColumn '{Q(tableName)}','{Q(columnName)}';");
        }
        private void CreateOrAlterFk(string tableName, JqlColumnChangeTrackable tableColumn)
        {
            if (tableColumn.Fk?.FkName == "")
                tableColumn.Fk.FkName = $"{tableName}_{tableColumn.Name}_{tableColumn.Fk.TargetTable}_{tableColumn.Fk.TargetColumn}";
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzCreateOrAlterFk '{Q(tableColumn.Fk?.FkName ?? "")}','{Q(tableName)}','{Q(tableColumn.Name)}','{Q(tableColumn.Fk?.TargetTable ?? "")}','{Q(tableColumn.Fk?.TargetColumn ?? "")}',{(tableColumn.Fk?.EnforceRelation == true ? "1" : "0")};");
        }
        public void DropFk(string tableName, string fkName)
        {
            DbIOInstance.ToNonQuery($"EXEC dbo.ZzDropFk '{Q(fkName)}','{Q(tableName)}'");
        }


    }
}
