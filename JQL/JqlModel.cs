using PowNet.Common;
using PowNet.Extensions;
using PowNet.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JQL
{
	public class JqlModel : IDisposable
	{
		private const string FileSuffix = ".jqlmodel.json";
		private const string CachePrefix = "JqlModel :: ";
		private string? _jqlModelsRoot;
		public string DevNote { set; get; } = string.Empty;
		public string DbConfName { set; get; } = string.Empty;
		public string ObjectName { set; get; }
        public DbObjectType ObjectType { set; get; } = DbObjectType.Table;

		public string ObjectIcon { set; get; } = string.Empty;
		public string ObjectColor { set; get; } = string.Empty;

		public string ParentColumn { set; get; } = string.Empty;
		public string NoteColumn { set; get; } = string.Empty;
		public string UiIconColumn { set; get; } = string.Empty;
		public string UiColorColumn { set; get; } = string.Empty;
		public string ViewOrderColumn { set; get; } = string.Empty;

		public List<JqlColumn> Columns { set; get; } = [];
        public List<JqlRelation>? Relations { set; get; }
        public List<JqlQuery> DbQueries { set; get; } = [];

		public bool PreventBuildUI { set; get; } = false;
		public bool PreventAlterServerObjects { set; get; } = false;

		[JsonConstructor]
        public JqlModel() { }

        public JqlModel(string dbConfName, string objectName, string modelsFolder)
        {
            DbConfName = dbConfName;
            ObjectName = objectName;
            _jqlModelsRoot = modelsFolder;
        }

		public JqlColumn GetPk()
		{
			JqlColumn? dbColumn = Columns.FirstOrDefault(i => i.IsPrimaryKey == true) ?? throw new PowNetException("PrimaryKeyColumnIsNotDefined", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", ObjectName)
					.GetEx();
			return dbColumn;
		}


		public JqlColumn? GetFirstFileColumn() => Columns.FirstOrDefault(i => i.DbType.EqualsIgnoreCase("IMAGE"));

		public List<JqlColumn> GetAuditingColumns()
		{
			List<JqlColumn> dbColumns = [];
			JqlColumn? createdOn = Columns.FirstOrDefault(i => i.Name.EqualsIgnoreCase(LibSV.CreatedOn));
			JqlColumn? UpdatedOn = Columns.FirstOrDefault(i => i.Name.EqualsIgnoreCase(LibSV.UpdatedOn));
			if (createdOn is not null) dbColumns.Add(createdOn);
			if (UpdatedOn is not null) dbColumns.Add(UpdatedOn);
			return dbColumns;
		}

		public List<JqlRelation> GetRelationsForAQuery(string queryName, RelationType? relationType = null, bool fileCentric = false)
		{
			JqlQuery? dbQuery = DbQueries.FirstOrDefault(i => i.Name == queryName);
			if (dbQuery is null || dbQuery.Relations is null || Relations is null) return [];
			List<JqlRelation> dbRelations = Relations.Where(o => ((relationType == null || o.RelationType == relationType) && o.IsFileCentric == fileCentric) && dbQuery.Relations.ContainsIgnoreCase(o.RelationName)).ToList();
			return [.. dbRelations];
		}

		public JqlRelation GetRelation(string relationName)
        {
            JqlRelation? dbRelation = Relations?.FirstOrDefault(i => i.RelationName == relationName);
			return dbRelation ?? throw new PowNetException("DbRelationIsNotDefined", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", ObjectName)
					.AddParam("DbRelation", relationName)
					.GetEx();
		}
		public JqlColumn GetColumn(string columnName)
        {
            JqlColumn? dbColumn = Columns?.FirstOrDefault(i => i.Name == columnName);
			return dbColumn ?? throw new PowNetException("ColumnIsNotExist", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", ObjectName)
					.AddParam("ColumnName", columnName)
					.GetEx();
		}
		public JqlColumn? TryGetColumn(string columnName)
        {
            return Columns?.FirstOrDefault(i => i.Name == columnName);
        }
		
		public void Save()
        {
			if (_jqlModelsRoot is null) throw new PowNetException("ModelSaveWithNoPathIsNotPossible", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", ObjectName)
                    .GetEx();
			File.WriteAllText(GetFullFilePath(_jqlModelsRoot, DbConfName, ObjectName), this.ToJsonStringByBuiltIn(true, false));
			// Invalidate both new and legacy cache keys
			MemoryService.SharedMemoryCache.TryRemove(GenCacheKey(DbConfName, ObjectName));
		}

		public bool IsTree() => Columns.FirstOrDefault(i => i.Fk != null && i.Fk.TargetTable == ObjectName) != null;

		public JqlColumn GetTreeParentColumn()
		{
			JqlColumn? dbColumn = Columns.FirstOrDefault(i => i.Fk != null && i.Fk.TargetTable == ObjectName);
			return dbColumn is null
				? throw new PowNetException("ModelIsNotTree", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", ObjectName)
					.GetEx() : dbColumn;
		}
		public string GetHumanIds()
		{
			return string.Join(", ", Columns.Where(i => i.IsHumanId == true).Select(i => i.Name));
		}
		public List<JqlColumn> GetHumanIdsOrig()
		{
			return Columns.Where(i => i.IsHumanId == true).ToList();
		}
		public JqlColumn? TryGetTreeParentColumn() => Columns.FirstOrDefault(i => i.Fk != null && i.Fk.TargetTable == ObjectName);
		public string GetTreeParentColumnName()
		{
			JqlColumn? dbColumn = Columns.FirstOrDefault(i => i.Fk != null && i.Fk.TargetTable == ObjectName);
            if (dbColumn is null) return string.Empty;
            return dbColumn.Name;
		}

		public bool IsSelfReferenceColumn(string columnName)
		{
            JqlColumn dbColumn = GetColumn(columnName);
            return dbColumn.Fk is not null && dbColumn.Fk.TargetTable == ObjectName;
		}

		public string GetModelFolder() => _jqlModelsRoot ?? string.Empty;

        public ClientQueryMetadata GetReadListClientQueryMetadata(string queryName)
        {
			ClientQueryMetadata cqm = new(ObjectName, ObjectType.ToString())
			{
				ParentObjectColumns= Columns.Where(i => !i.Name.ContainsIgnoreCase("password")).ToList(),
				FastSearchColumns = Columns.Where(i => i.UiProps?.SearchType == SearchType.Fast && !i.Name.ContainsIgnoreCase("password")).ToList(),
				ExpandableSearchColumns = Columns.Where(i => i.UiProps?.SearchType == SearchType.Expandable && !i.Name.ContainsIgnoreCase("password")).ToList()
			};

			JqlQuery? dbQuery = DbQueries.FirstOrDefault(i => i.Name == queryName);

			if (dbQuery is not null)
            {
                cqm.Type = dbQuery.Type.ToString();
                cqm.Name = dbQuery.Name;
                if (dbQuery.Columns is not null)
                {
					foreach (JqlQueryColumn dbQueryColumn in dbQuery.Columns)
					{
						string columnName = dbQueryColumn.Name ?? dbQueryColumn.As ?? "";
						if (!columnName.IsNullOrEmpty())
						{
							// Ensure unique column names in a case-insensitive way
							if (!cqm.QueryColumns.Any(s => s.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
							{
								cqm.QueryColumns.Add(columnName);
							}
						}
					}
				}
			}
			return cqm;
        }


		private bool _disposed = false;

		// Other properties and methods...

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
				// Dispose managed resources
				// Example: if you have any disposable members, dispose them here
				// _someDisposableMember?.Dispose();
			}

			// Dispose unmanaged resources (if any)

			_disposed = true;
		}


		public static JqlModel Load(string jqlModelsRoot, string dbConfName, string? objectName)
        {
			string fp = GetFullFilePath(jqlModelsRoot, dbConfName, objectName);
			// Fallback to legacy path if the new path does not exist
			if (!File.Exists(fp))
			{
                throw new PowNetException("FilePathIsNotExist", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("Model", objectName.ToStringEmpty())
                    .AddParam("FilePath", fp)
                    .GetEx();
            }

            JqlModel? jqlModel;

			jqlModel = JsonSerializer.Deserialize<JqlModel>(File.ReadAllText(fp)) ?? throw new PowNetException("DeserializeError", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("Model", objectName.ToStringEmpty())
					.AddParam("FilePath", fp)
					.GetEx();
			jqlModel._jqlModelsRoot = jqlModelsRoot;
			return jqlModel;
		}
        public static JqlModel? TryLoad(string jqlModelsRoot, string dbConfName, string? objectName)
        {
            JqlModel? jqlModel = null;
            if (JqlModel.Exist(jqlModelsRoot, dbConfName, objectName))
            {
                jqlModel = JqlModel.Load(jqlModelsRoot, dbConfName, objectName);
            }
            return jqlModel;
        }

        public static string GetFullFilePath(string jqlModelsRoot, string dbConfName, string? objectName)
        {
			return Path.Combine(jqlModelsRoot, $"{dbConfName}.{(objectName is null ? "db" : objectName)}{FileSuffix}");
        }


        public static bool Exist(string jqlModelsRoot, string dbConfName, string? objectName)
        {
			return File.Exists(GetFullFilePath(jqlModelsRoot, dbConfName, objectName));
        }

		public static bool IsColumnInParams(JqlQuery dbQuery, string columnName)
		{
			if (dbQuery.Params is null) return false;
			JqlParam? dbParam = dbQuery.Params.FirstOrDefault(i => i.Name == columnName);
			return dbParam != null;
		}

		private static string GenCacheKey(string dbConfName, string? objectName)
		{
			if (!string.IsNullOrEmpty(objectName))
				return $"{CachePrefix}{dbConfName}.{objectName}";
			else
                return $"{CachePrefix}{dbConfName}";
        }

    }
}
