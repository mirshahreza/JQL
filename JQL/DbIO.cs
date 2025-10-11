using Microsoft.Data.SqlClient;
using PowNet.Common;
using PowNet.Configuration;
using PowNet.Extensions;
using System.Data;
using System.Data.Common;

namespace JQL
{
    public abstract class DbIO : IDisposable
    {
        private readonly DbConnection dbConnection;
        
        public DatabaseConfiguration DbConf { get; init; }
        public DbIO(DatabaseConfiguration dbConf)
        {
            DbConf = dbConf;
            dbConnection = CreateConnection();
        }

		public static DbIO Instance(DatabaseConfiguration dbConf)
		{
			if (dbConf.ServerType == ServerType.MsSql) return new DbIOMsSql(dbConf);
			throw new PowNetException($"DbServerTypeNotImplementedYet", System.Reflection.MethodBase.GetCurrentMethod())
				.AddParam("ServerType", dbConf.ServerType)
				.GetEx();
		}

		public static DbIO Instance(string connectionName = "DefaultConnection")
		{
            var dbConf = DatabaseConfiguration.FromSettings(connectionName);
			if (dbConf.ServerType == ServerType.MsSql) return new DbIOMsSql(dbConf);
			throw new PowNetException($"ServerTypeNotImplementedYet", System.Reflection.MethodBase.GetCurrentMethod())
				.AddParam("ServerType", dbConf.ServerType)
				.GetEx();
		}

		public Dictionary<string, DataTable> ToDataSet(string commandString, List<DbParameter>? dbParameters = null, List<string>? TableNames = null)
        {
            try
            {
                using DbCommand command = CreateDbCommand(commandString, dbConnection, dbParameters);
                using DataSet ds = new();
                var adapter = CreateDataAdapter(command);
                adapter.Fill(ds);
                var dic = new Dictionary<string, DataTable>(ds.Tables.Count); // Pre-size dictionary
                for (int ind = 0; ind < ds.Tables.Count; ind++)
                {
                    DataTable dt = ds.Tables[ind];
                    string tableName = (TableNames is not null && ind < TableNames.Count) 
                        ? TableNames[ind] 
                        : $"T{ind}";
                    dic.Add(tableName, dt);
                }
                return dic;
            }
            catch (Exception ex)
            {
                var contentBuilder = new System.Text.StringBuilder(ex.Message.Length + commandString.Length + 100);
                contentBuilder.Append(ex.Message).Append(StringExtensions.NL)
                             .Append(commandString).Append(StringExtensions.NL)
                             .Append(dbParameters.ToJsonStringByNewtonsoft()).Append(StringExtensions.NL);
                
				throw new PowNetException("NameAndPhraseCanNotBeUnknownTogether", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Message", ex.Message)
					.AddParam("Query", contentBuilder.ToString())
					.GetEx();
            }
		}

        public DataTable ToDataTable(string commandString, List<DbParameter>? dbParameters = null)
        {
            return ToDataTables(commandString, dbParameters: dbParameters, tableName: "MainDT")["MainDT"];
        }

        public Dictionary<string, DataTable> ToDataTables(string commandString, List<DbParameter>? dbParameters = null, string? tableName = null)
        {
            try
            {
                using DbCommand command = CreateDbCommand(commandString, dbConnection, dbParameters);
                using DbDataReader sdr = command.ExecuteReader();
                DataTable dt = new();
                dt.Load(sdr);
                Dictionary<string, DataTable> dic = new() { { tableName ?? "Master", dt } };
                return dic;
            }
            catch (Exception ex)
            {
                var contentBuilder = new System.Text.StringBuilder(ex.Message.Length + commandString.Length + 100);
                contentBuilder.Append(ex.Message).Append(StringExtensions.NL)
                             .Append(commandString).Append(StringExtensions.NL)
                             .Append(dbParameters.ToJsonStringByNewtonsoft()).Append(StringExtensions.NL);
                
				throw new PowNetException(ex.Message, System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Query", contentBuilder.ToString())
					.GetEx();
			}
		}
        public object? ToScalar(string commandString, List<DbParameter>? dbParameters = null)
        {
            try
            {
                using DbCommand command = CreateDbCommand(commandString, dbConnection, dbParameters);
                var s = command.ExecuteScalar();
                return s;
            }
            catch (Exception ex)
            {
                var contentBuilder = new System.Text.StringBuilder(ex.Message.Length + commandString.Length + 100);
                contentBuilder.Append(ex.Message).Append(StringExtensions.NL)
                             .Append(commandString).Append(StringExtensions.NL)
                             .Append(dbParameters.ToJsonStringByNewtonsoft()).Append(StringExtensions.NL);
                
				throw new PowNetException("NameAndPhraseCanNotBeUnknownTogether", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Message", ex.Message)
					.AddParam("Query", contentBuilder.ToString())
					.GetEx();
			}
		}
		public void ToNoneQuery(string commandString, List<DbParameter>? dbParameters = null)
		{
            try
            {
                using DbCommand command = CreateDbCommand(commandString, dbConnection, dbParameters);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var contentBuilder = new System.Text.StringBuilder(ex.Message.Length + commandString.Length + 100);
                contentBuilder.Append(ex.Message).Append(StringExtensions.NL)
                             .Append(commandString).Append(StringExtensions.NL)
                             .Append(dbParameters.ToJsonStringByNewtonsoft()).Append(StringExtensions.NL);
                
				throw new PowNetException("NameAndPhraseCanNotBeUnknownTogether", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Message", ex.Message)
					.AddParam("Query", contentBuilder.ToString())
					.GetEx();
			}
		}
		public void ToNoneQueryAsync(string commandString, List<DbParameter>? dbParameters = null)
		{
            try
            {
                using DbCommand command = CreateDbCommand(commandString, dbConnection, dbParameters);
                command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                var contentBuilder = new System.Text.StringBuilder(ex.Message.Length + commandString.Length + 100);
                contentBuilder.Append(ex.Message).Append(StringExtensions.NL)
                             .Append(commandString).Append(StringExtensions.NL)
                             .Append(dbParameters.ToJsonStringByNewtonsoft()).Append(StringExtensions.NL);
                
				throw new PowNetException("NameAndPhraseCanNotBeUnknownTogether", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Message", ex.Message)
					.AddParam("Query", contentBuilder.ToString())
					.GetEx();
			}
		}

        public static bool TestConnection(DatabaseConfiguration dbConf)
        {
            try
            {
                using DbIO dbIO = Instance(dbConf);
                using DbConnection dbConnection = dbIO.CreateConnection();
                return dbConnection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                throw new PowNetException("TestConnectionFailed", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("Message", ex.Message)
                    .GetEx();
            }
		}   

		public abstract DbConnection CreateConnection();
        public abstract DbCommand CreateDbCommand(string commandText, DbConnection dbConnection, List<DbParameter>? dbParameters = null);
        public abstract DataAdapter CreateDataAdapter(DbCommand dbCommand);
        public abstract DbParameter CreateParameter(string columnName, string columnType, int? columnSize = null, object? value = null);
        public abstract string GetSqlTemplate(QueryType dbQueryType, bool isForSubQuery = false);
        public abstract string GetPaginationSqlTemplate();
        public abstract string GetGroupSqlTemplate();
        public abstract string GetOrderSqlTemplate();
        public abstract string GetLeftJoinSqlTemplate();
        public abstract string GetTranBlock();
        public abstract string CompileWhereCompareClause(CompareClause whereCompareClause, string source, string columnFullName, string dbParamName, string dbType);
		public abstract string DbParamToCSharpInputParam(DbParam dbParam);

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				dbConnection?.Close();
				dbConnection?.Dispose();
			}
			_disposed = true;
		}

		~DbIO()
		{
			Dispose(false);
		}

	}

    public class DbIOMsSql(DatabaseConfiguration dbInfo) : DbIO(dbInfo)
    {
        public override DbConnection CreateConnection()
        {
            DbConnection dbConnection = new SqlConnection(DbConf.ConnectionString);
            dbConnection.Open();
            return dbConnection;
        }

        public override DataAdapter CreateDataAdapter(DbCommand dbCommand)
        {
            return new SqlDataAdapter((SqlCommand)dbCommand);
        }

        public override DbCommand CreateDbCommand(string commandText, DbConnection dbConnection, List<DbParameter>? dbParameters = null)
        {
            List<string> paramsInSql = commandText.ExtractSqlParameters();
            List<string> notExistParams = paramsInSql.Where(i => dbParameters?.FirstOrDefault(p => p.ParameterName.EqualsIgnoreCase(i)) == null).ToList();
            if (notExistParams.Count > 0)
            {
                if (dbParameters is null) dbParameters = [];
                foreach (string p in notExistParams)
                {
                    if (!p.EqualsIgnoreCase("InsertedTable") && !p.EqualsIgnoreCase("MasterId"))
                        dbParameters.Add(CreateParameter(p, "NVARCHAR", 4000, null));
                }
            }
            SqlCommand sqlCommand = new(commandText, (SqlConnection)dbConnection);
            if (dbParameters is not null && dbParameters.Count > 0) sqlCommand.Parameters.AddRange(dbParameters.ToArray());
            return sqlCommand;
        }

        public override DbParameter CreateParameter(string columnName, string columnType, int? columnSize = null, object? value = null)
        {
            SqlParameter op = new()
            {
                IsNullable = true,
                ParameterName = columnName,
                SqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), columnType, true)
            };
            if (columnSize is not null) op.Size = (int)columnSize;
            op.Value = value is null ? DBNull.Value : value;
            return op;
        }

        public override string GetSqlTemplate(QueryType dbQueryType, bool isForSubQuery = false)
        {
            if (dbQueryType is QueryType.Create)
            {
                if (isForSubQuery == false)
                    return @"

DECLARE @InsertedTable TABLE (Id {PkTypeSize});
DECLARE @MasterId {PkTypeSize};
INSERT INTO [{TargetTable}] 
    ({Columns}) 
        OUTPUT INSERTED.{PkName} INTO @InsertedTable 
    VALUES 
    ({Values});
SELECT TOP 1 @MasterId=Id FROM @InsertedTable;
{SubQueries}
SELECT @MasterId;
";
                else
                    return @"
INSERT INTO [{TargetTable}] 
    ({Columns}) 
    VALUES 
    ({Values});
";

            }


            if (dbQueryType is QueryType.ReadList)
            {
                if (isForSubQuery == false)
                    return @"
SELECT 
	{Columns} 
	{Aggregations} 
	{SubQueries} 
	FROM [{TargetTable}] WITH(NOLOCK) 
	{Lefts} 
	{Where} 
	{Order} 
	{Pagination};
";
                else
                    return @"
SELECT 
	{Columns} 
	FROM [{TargetTable}] WITH(NOLOCK) 
	{Lefts} 
	{Where} 
	{Order}
    FOR JSON PATH
";
            }

            if (dbQueryType is QueryType.AggregatedReadList) return @"
SELECT 
	{Columns} 
	{Aggregations} 
	FROM [{TargetTable}] WITH(NOLOCK) 
	{Lefts} 
	{Where} 
	{GroupBy} 
	{Order} 
	{Pagination};
";

            if (dbQueryType is QueryType.ReadByKey) return @"
SELECT 
	{Columns} 
	{SubQueries} 
	FROM {TargetTable} WITH(NOLOCK) 
	{Lefts} 
	{Where};
";

            if (dbQueryType is QueryType.UpdateByKey)
            {
                if (isForSubQuery == false)
                    return @"
{PreQueries}
UPDATE [{TargetTable}] SET 
	{Sets} 
	{Where};
{SubQueries}
";
                else
                    return @"
UPDATE [{TargetTable}] SET 
	{Sets} 
	{Where};
";
            }

            if (dbQueryType is QueryType.Delete)
                return @"
DELETE [{TargetTable}] 
	{Where};
";

            if (dbQueryType is QueryType.DeleteByKey)
            {
                if (isForSubQuery == false)
                    return @"
{SubQueries}
DELETE [{TargetTable}] 
	{Where};
";
                else
                    return @"
DELETE [{TargetTable}] 
	{Where};
";
            }

            if (dbQueryType is QueryType.Procedure) return @"
EXEC [dbo].[{StoredProcedureName}] 
	{InputParams};
";

            if (dbQueryType is QueryType.TableFunction) return @"
SELECT * FROM [dbo].[{FunctionName}] 
	({InputParams});
";

            if (dbQueryType is QueryType.ScalarFunction) return @"
SELECT [dbo].[{FunctionName}] 
	({InputParams});
";

            throw new PowNetException("NotImplementedYet", System.Reflection.MethodBase.GetCurrentMethod())
                .AddParam("DbQueryType", dbQueryType.ToString())
                .GetEx();
        }

        public override string GetPaginationSqlTemplate()
        {
            return @"
	OFFSET {PageIndex} ROWS FETCH NEXT {PageSize} ROWS ONLY
";
        }

        public override string GetGroupSqlTemplate()
        {
            return @"
	GROUP BY {Groups}
";
        }
        public override string GetOrderSqlTemplate()
        {
            return @"
	ORDER BY {Orders}
";
        }

        public override string GetLeftJoinSqlTemplate()
        {
            return @"
	LEFT OUTER JOIN {TargetTable} AS {TargetTableAs} WITH(NOLOCK) ON [{TargetTableAs}].[{TargetColumn}]=[{MainTable}].[{MainColumn}]
";
        }
        public override string GetTranBlock()
        {
            return @"
BEGIN TRAN {TranName};
{SqlBody}
COMMIT TRAN {TranName};
";
        }

        public override string CompileWhereCompareClause(CompareClause wcc, string source, string columnFullName, string dbParamName, string dbType)
        {
            string N = "";
            if (dbType.EqualsIgnoreCase(SqlDbType.NChar.ToString()) ||
                dbType.EqualsIgnoreCase(SqlDbType.NVarChar.ToString()) ||
                dbType.EqualsIgnoreCase(SqlDbType.NText.ToString()))
            {
                N = "N";
            }

            if (wcc.CompareOperator == CompareOperator.StartsWith) return $"{columnFullName} LIKE @{DbUtils.GenParamName(source, dbParamName, null)} + {N}'%'";
            if (wcc.CompareOperator == CompareOperator.EndsWith) return $"{columnFullName} LIKE {N}'%' + @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.Contains) return $"{columnFullName} LIKE {N}'%' + @{DbUtils.GenParamName(source, dbParamName, null)} + {N}'%'";

            if (wcc.CompareOperator == CompareOperator.Equal) return $"{columnFullName} = @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.NotEqual) return $"{columnFullName} != @{DbUtils.GenParamName(source, dbParamName, null)}";

            if (wcc.CompareOperator == CompareOperator.IsNull) return $"{columnFullName} IS NULL";
            if (wcc.CompareOperator == CompareOperator.IsNotNull) return $"{columnFullName} IS NOT NULL";

            if (wcc.CompareOperator == CompareOperator.LessThan) return $"{columnFullName} < @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.LessThanOrEqual) return $"{columnFullName} <= @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.MoreThan) return $"{columnFullName} > @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.MoreThanOrEqual) return $"{columnFullName} >= @{DbUtils.GenParamName(source, dbParamName, null)}";

            if (wcc.CompareOperator == CompareOperator.In) return $"{columnFullName} IN @{DbUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.NotIn) return $"{columnFullName} NOT IN @{DbUtils.GenParamName(source, dbParamName, null)}";

            return "";
        }

        public override string DbParamToCSharpInputParam(DbParam dbParam)
        {
            // cover char,nchar,varchar,nvarchar,text,ntext, uniqueidentifier
            if (dbParam.DbType.Contains("char") || dbParam.DbType.Contains("text") || dbParam.DbType.Contains("uniqueidentifier"))
                return $"string {dbParam.Name}";

            if (dbParam.DbType.Contains("bigint"))
                return $"Int64 {dbParam.Name}";

            if (dbParam.DbType.Contains("int"))
                return $"int {dbParam.Name}";

            if (dbParam.DbType.Contains("date"))
                return $"DateTime {dbParam.Name}";

            if (dbParam.DbType.Equals("bit"))
                return $"Boolean {dbParam.Name}";

            if (dbParam.DbType.Equals("decimal") || dbParam.DbType.Equals("money") || dbParam.DbType.Equals("numeric") || dbParam.DbType.Equals("real"))
                return $"decimal {dbParam.Name}";

            if (dbParam.DbType.Equals("float"))
                return $"float {dbParam.Name}";

            if (dbParam.DbType.Equals("image") || dbParam.DbType.Equals("binary"))
				return $"byte[] {dbParam.Name}";

			return $"string {dbParam.Name}";
		}

	}

}
