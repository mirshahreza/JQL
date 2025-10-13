using PowNet.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JQL
{
	public static class JqlUtils
    {
        // Constants and rule sets to reduce repetition and centralize policies
        private const int FileCentricMaxColumns = 8;
        private const string DateSuffix = "On";
        private const string DeleteSkipSuffix = "_xs";

        private static readonly HashSet<string> DisplayColumnNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "Title", "FirstName", "FatherName", "GrandFatherName", "LastName", "UserName"
        };

        private static readonly HashSet<string> SortableColumnNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "Title", "CreatedOn", "UpdatedOn"
        };
        
        // Cached sets for external DbUtils fields for O(1) lookups
        private static readonly HashSet<string> CreatedFieldsSet =
                CreatedFields is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(CreatedFields, StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> AuditingFieldsSet =
            AuditingFields is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(AuditingFields, StringComparer.OrdinalIgnoreCase);
        
        public static bool ColumnIsForDisplay(this JqlColumn dbColumn)
        {
            return DisplayColumnNames.Contains(dbColumn.Name);
        }
		public static bool ColumnIsSortable(this JqlColumn dbColumn)
		{
            return SortableColumnNames.Contains(dbColumn.Name);
		}
		public static bool ColumnIsForReadByKey(this JqlColumn dbColumn)
        {
            // todo : implemention required
            return true;
        }
        public static bool ColumnIsForReadList(this JqlColumn dbColumn)
        {
			if (dbColumn.IsFileOrRelatedColumns()) return false;
			if (dbColumn.Name.ContainsIgnoreCase("password")) return false;
            return true;
        }
        public static bool ColumnIsForAggregatedReadList(this JqlColumn dbColumn)
        {
            var name = dbColumn.Name;
            if (dbColumn.IsPrimaryKey) return false;
            if (dbColumn.IsFileOrRelatedColumns()) return false;
            if (name.ContainsIgnoreCase("Name")) return false;
            if (name.ContainsIgnoreCase("Title")) return false;
            if (name.EndsWithIgnoreCase(DateSuffix)) return false;
			if (name.ContainsIgnoreCase("password")) return false;
            if (!dbColumn.IsNumerical())
            {
                if (dbColumn.Size is not null && int.TryParse(dbColumn.Size, out var sizeVal) && sizeVal > 256)
                    return false;
            }
            return true;
        }
        public static bool ColumnIsForDelete(this JqlColumn dbColumn)
        {
			var name = dbColumn.Name;
			if (name.EndsWithIgnoreCase(DeleteSkipSuffix)) return false;
			if (name.ContainsIgnoreCase("xml")) return false;
			if (name.ContainsIgnoreCase("html")) return false;
			if (dbColumn.IsFileOrRelatedColumns()) return false;
			if (name.ContainsIgnoreCase("password")) return false;
            return true;
        }
        public static bool ColumnIsForCreate(this JqlColumn dbColumn)
        {
            if (dbColumn.IsIdentity || dbColumn.DbDefault != null) return false;
			if (dbColumn.Name.ContainsIgnoreCase("password")) return false;
			return true;
        }
        public static bool ColumnIsForUpdateByKey(this JqlColumn dbColumn)
        {
            if (CreatedFieldsSet.Contains(dbColumn.Name)) return false;
			if (dbColumn.Name.ContainsIgnoreCase("password")) return false;
			return true;
        }

        public static string GenParamName(string objectName, string columnName, int? index = null)
        {
            var suffix = index is null ? string.Empty : "_" + index.Value.ToString(CultureInfo.InvariantCulture);
            return $"{objectName}_{columnName}" + suffix;
        }

        public static string GetSetColumnParamPair(string source, string columnName,int? index)
        {
            return $"[{source}].[{columnName}]=@{GenParamName(source, columnName, index)}";
        }

        public static string GetTypeSize(string dbType, object? size)
        {
            string dbT = dbType.ToUpperInvariant();
            if (size == null) return dbT;
            return $"{dbT}({size})";
        }

        public static bool ColumnsAreFileCentric(List<JqlColumn> dbColumns)
        {
            bool anyFileType = dbColumns.Any(i => i.DbType.EqualsIgnoreCase("IMAGE") || i.DbType.EqualsIgnoreCase("BINARY"));
            if (anyFileType && dbColumns.Count < FileCentricMaxColumns) return true;
            return false;
        }

		public static List<JqlColumn> RemoveAuditingColumns(this List<JqlColumn> dbColumns)
		{
            return [.. dbColumns.Where(i => !AuditingFieldsSet.Contains(i.Name))];
        }


        public static readonly List<string> AuditingFields = ["CreatedBy", "CreatedOn", "UpdatedBy", "UpdatedOn"];
        public static readonly List<string> UpdatedFields = ["UpdatedBy", "UpdatedOn"];
        public static readonly string UpdatedBy = "UpdatedBy";
        public static readonly string UpdatedOn = "UpdatedOn";
        public static readonly List<string> CreatedFields = ["CreatedBy", "CreatedOn"];
        public static readonly string CreatedBy = "CreatedBy";
        public static readonly string CreatedOn = "CreatedOn";

        public static readonly string ViewOrder = "ViewOrder";
        public static readonly string ReadByKey = "ReadByKey";
        public static readonly string Update = "Update";
        public static readonly string AsStr = " AS ";

    }
}
