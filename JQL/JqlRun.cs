using Microsoft.Data.SqlClient;
using PowNet.Common;
using PowNet.Configuration;
using PowNet.Extensions;
using PowNet.Data;
using System.Data;
using System.Data.Common;

namespace JQL
{
    public abstract class JqlRun(DatabaseConfiguration dbConf) : DbCommandExecutor(dbConf)
    {
		public static new JqlRun Instance(DatabaseConfiguration dbConf)
		{
			if (dbConf.ServerType == ServerType.MsSql) return new DbIOMsSql(dbConf);
			throw new PowNetException($"DbServerTypeNotImplementedYet", System.Reflection.MethodBase.GetCurrentMethod())
				.AddParam("ServerType", dbConf.ServerType)
				.GetEx();
		}

		public static new JqlRun Instance(string connectionName = "DefaultConnection")
		{
            var dbConf = DatabaseConfiguration.FromSettings(connectionName);
			if (dbConf.ServerType == ServerType.MsSql) return new DbIOMsSql(dbConf);
			throw new PowNetException($"ServerTypeNotImplementedYet", System.Reflection.MethodBase.GetCurrentMethod())
				.AddParam("ServerType", dbConf.ServerType)
				.GetEx();
		}

        public static new bool TestConnection(DatabaseConfiguration dbConf) => DbCommandExecutor.TestConnection(dbConf);

        public abstract string GetSqlTemplate(QueryType dbQueryType, bool isForSubQuery = false);
        public abstract string GetPaginationSqlTemplate();
        public abstract string GetGroupSqlTemplate();
        public abstract string GetOrderSqlTemplate();
        public abstract string GetLeftJoinSqlTemplate();
        public abstract string GetTranBlock();
        public abstract string CompileWhereCompareClause(CompareClause whereCompareClause, string source, string columnFullName, string dbParamName, string dbType);
		public abstract string DbParamToCSharpInputParam(JqlParam dbParam);
	}

    public class DbIOMsSql(DatabaseConfiguration dbInfo) : JqlRun(dbInfo)
    {
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

            if (wcc.CompareOperator == CompareOperator.StartsWith) return $"{columnFullName} LIKE @{JqlUtils.GenParamName(source, dbParamName, null)} + {N}'%'";
            if (wcc.CompareOperator == CompareOperator.EndsWith) return $"{columnFullName} LIKE {N}'%' + @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.Contains) return $"{columnFullName} LIKE {N}'%' + @{JqlUtils.GenParamName(source, dbParamName, null)} + {N}'%'";

            if (wcc.CompareOperator == CompareOperator.Equal) return $"{columnFullName} = @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.NotEqual) return $"{columnFullName} != @{JqlUtils.GenParamName(source, dbParamName, null)}";

            if (wcc.CompareOperator == CompareOperator.IsNull) return $"{columnFullName} IS NULL";
            if (wcc.CompareOperator == CompareOperator.IsNotNull) return $"{columnFullName} IS NOT NULL";

            if (wcc.CompareOperator == CompareOperator.LessThan) return $"{columnFullName} < @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.LessThanOrEqual) return $"{columnFullName} <= @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.MoreThan) return $"{columnFullName} > @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.MoreThanOrEqual) return $"{columnFullName} >= @{JqlUtils.GenParamName(source, dbParamName, null)}";

            if (wcc.CompareOperator == CompareOperator.In) return $"{columnFullName} IN @{JqlUtils.GenParamName(source, dbParamName, null)}";
            if (wcc.CompareOperator == CompareOperator.NotIn) return $"{columnFullName} NOT IN @{JqlUtils.GenParamName(source, dbParamName, null)}";

            return "";
        }

        public override string DbParamToCSharpInputParam(JqlParam dbParam)
        {
            // cover char,nchar,varchar,nvarchar,text,ntext, uniqueidentifier
            if (dbParam.DbType.ContainsIgnoreCase("char") || dbParam.DbType.ContainsIgnoreCase("text") || dbParam.DbType.ContainsIgnoreCase("uniqueidentifier"))
                return $"string {dbParam.Name}";

            if (dbParam.DbType.ContainsIgnoreCase("bigint"))
                return $"Int64 {dbParam.Name}";

            if (dbParam.DbType.ContainsIgnoreCase("int"))
                return $"int {dbParam.Name}";

            if (dbParam.DbType.ContainsIgnoreCase("date"))
                return $"DateTime {dbParam.Name}";

            if (dbParam.DbType.EqualsIgnoreCase("bit"))
                return $"Boolean {dbParam.Name}";

            if (dbParam.DbType.EqualsIgnoreCase("decimal") || dbParam.DbType.EqualsIgnoreCase("money") || dbParam.DbType.EqualsIgnoreCase("numeric") || dbParam.DbType.EqualsIgnoreCase("real"))
                return $"decimal {dbParam.Name}";

            if (dbParam.DbType.EqualsIgnoreCase("float"))
                return $"float {dbParam.Name}";

            if (dbParam.DbType.EqualsIgnoreCase("image") || dbParam.DbType.EqualsIgnoreCase("binary"))
				return $"byte[] {dbParam.Name}";

			return $"string {dbParam.Name}";
		}

	}

}
