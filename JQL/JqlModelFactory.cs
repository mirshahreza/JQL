using PowNet.Common;
using PowNet.Configuration;
using PowNet.Extensions;
using PowNet.Services;
using System.Data;

namespace JQL
{
	public class JqlModelFactory
    {
        private string JqlModelFolderPath { init; get; }
        private string DbConfName { init; get; }
		private DatabaseConfiguration DbConf { init; get; }
		private DbSchemaUtils DbSchemaUtils { init; get; }
		
        private JqlRun _dbIo;
        public JqlRun DbIOInstance
        {
            get
            {
                return _dbIo ??= JqlRun.Instance(DbConf);
            }
        }

        public JqlModelFactory(string dbConfName)
        {
            JqlModelFolderPath = PowNetConfiguration.ServerPath;
            DbConfName = dbConfName;
            DbConf = DatabaseConfiguration.FromSettings(DbConfName);
			DbSchemaUtils = new DbSchemaUtils(DbConfName);
			_dbIo = JqlRun.Instance(DbConf);
        }

        public void CreateNewUpdateByKey(string objectName, string readByKeyApiName, List<string> columnsToUpdate, string partialUpdateApiName, string byColumnName, string onColumnName, string historyTableName)
        {
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            if (columnsToUpdate.Count == 0) throw new PowNetException("YouMustIndicateAtleastOneColumnToCreateUpdateByKeyApi", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("ObjectName", objectName)
                    .GetEx();

            DbSchemaUtils dbSchemaUtils = new(DbConfName);
			JqlColumn pkCol = jqlModel.GetPk();
            List<string> finalColsForNewUpdateByKeyApi = [];
			if (!columnsToUpdate.Contains(pkCol.Name)) finalColsForNewUpdateByKeyApi.Add(pkCol.Name);
            finalColsForNewUpdateByKeyApi.AddRange(columnsToUpdate);

            if (!byColumnName.IsNullOrEmpty()) //  create UpdatedBy column if it is not empty
            {
                JqlColumn? byDbCol = jqlModel.Columns.FirstOrDefault(i => i.Name == byColumnName);
                if (byDbCol is null)
                {
                    dbSchemaUtils.CreateColumn(objectName, byColumnName, "INT", true);
                    byDbCol = new JqlColumn(byColumnName) { DbType = "INT", AllowNull = true };
                    jqlModel.Columns.Add(byDbCol);
                }
                if (!finalColsForNewUpdateByKeyApi.Contains(byColumnName)) finalColsForNewUpdateByKeyApi.Add(byColumnName);
            }

            if (!onColumnName.IsNullOrEmpty()) //  create UpdatedOn column if it is not empty
			{
                JqlColumn? onDbCol = jqlModel.Columns.FirstOrDefault(i => i.Name == onColumnName);
                if (onDbCol is null)
                {
                    dbSchemaUtils.CreateColumn(objectName, onColumnName, "DATETIME", true);
                    onDbCol = new JqlColumn(onColumnName) { DbType = "DATETIME", AllowNull = true };
                    jqlModel.Columns.Add(onDbCol);
                }
				if (!finalColsForNewUpdateByKeyApi.Contains(onColumnName)) finalColsForNewUpdateByKeyApi.Add(onColumnName);
            }


            // remove columns from UpdateByKey query
            JqlQuery? mainUpdateByKeyQ = jqlModel.DbQueries.FirstOrDefault(i => i.Name.EqualsIgnoreCase("UpdateByKey"));
            if (mainUpdateByKeyQ is not null)
            {
                foreach (string s in columnsToUpdate)
                {
                    JqlQueryColumn? qCol = mainUpdateByKeyQ.Columns?.FirstOrDefault(i => i.Name.EqualsIgnoreCase(s));
                    if (qCol is not null && mainUpdateByKeyQ.Columns is not null) mainUpdateByKeyQ.Columns.Remove(qCol);
                }

                JqlQueryColumn? qcBy = mainUpdateByKeyQ.Columns?.FirstOrDefault(i => i.Name.EqualsIgnoreCase(byColumnName));
                if (qcBy is not null && mainUpdateByKeyQ.Columns is not null) mainUpdateByKeyQ.Columns.Remove(qcBy);

                JqlQueryColumn? qcOn = mainUpdateByKeyQ.Columns?.FirstOrDefault(i => i.Name.EqualsIgnoreCase(onColumnName));
                if (qcOn is not null && mainUpdateByKeyQ.Columns is not null) mainUpdateByKeyQ.Columns.Remove(qcOn);
            }

            // Create/Alter ReadByKey query
            JqlQuery? readByKeyQ = jqlModel.DbQueries.FirstOrDefault(i => i.Name == readByKeyApiName);
            if (readByKeyQ is null) // create the new ReadByKey
            {
				readByKeyQ = GetReadByKeyQuery(jqlModel);
				readByKeyQ.Name = readByKeyApiName;
				jqlModel.DbQueries.Add(readByKeyQ);
				DynamicCodeService.CreateMethod($"{DbConfName}.{objectName}", readByKeyApiName);
			}

			readByKeyQ.Columns ??= [];
            if(!readByKeyApiName.EqualsIgnoreCase(JqlUtils.ReadByKey))
                readByKeyQ.Columns.RemoveAll(i => !i.Name.EqualsIgnoreCase(pkCol.Name));

			foreach (string s in columnsToUpdate)
				if (readByKeyQ.Columns.FirstOrDefault(i => i.Name.EqualsIgnoreCase(s)) is null) 
                    readByKeyQ.Columns.Add(new JqlQueryColumn() { Name = s });

			// gen/get Partial UpdateByKey query
			JqlQuery existingUpdateByKeyQ = GenOrGetUpdateByKeyQuery(jqlModel, partialUpdateApiName, finalColsForNewUpdateByKeyApi, byColumnName, onColumnName);
            if(!jqlModel.DbQueries.Contains(existingUpdateByKeyQ)) jqlModel.DbQueries.Add(existingUpdateByKeyQ);

			// refreshing UpdateGroup
			foreach (string col in finalColsForNewUpdateByKeyApi)
				if (!pkCol.Name.EqualsIgnoreCase(jqlModel.GetColumn(col).Name))
					jqlModel.GetColumn(col).UpdateGroup = partialUpdateApiName;

			// save JqlModel
			jqlModel.Save();

            // add related csharp method

            DynamicCodeService.Refresh();
            if (!DynamicCodeService.MethodExist($"{DbConfName}.{objectName}.{partialUpdateApiName}"))
                DynamicCodeService.CreateMethod($"{DbConfName}.{objectName}", partialUpdateApiName);

            // create log table and related server objects
            if (!historyTableName.IsNullOrEmpty())
            {
                existingUpdateByKeyQ.HistoryTable = historyTableName;
                jqlModel.Save();
                CreateOrAlterHistoryTable(objectName, partialUpdateApiName, historyTableName);
            }
            DynamicCodeService.Build();
        }
        public void CreateOrAlterHistoryTable(string objectName, string updateQueryName, string historyTableName)
        {
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            DbTable? historyTable = DbSchemaUtils.GetTables().FirstOrDefault(i => i.Name.EqualsIgnoreCase(historyTableName));

            JqlQuery? masterUpdateQ = jqlModel.DbQueries.FirstOrDefault(i => i.Name.EqualsIgnoreCase(updateQueryName));
            if (masterUpdateQ is null) return;

            JqlColumn pk = jqlModel.GetPk();
            DbTableChangeTrackable dbTable = new(historyTableName);

            dbTable.Columns.Add(SetAndGetColumnState(historyTable, new("FakeId") { DbType = "INT", AllowNull = false, IsIdentity = true, IdentityStart = "1", IdentityStep = "1", IsPrimaryKey = true }));
            dbTable.Columns.Add(SetAndGetColumnState(historyTable, new("Id") { DbType = pk.DbType, AllowNull = false, Fk = new("", objectName, pk.Name) }));
            dbTable.Columns.Add(SetAndGetColumnState(historyTable, new(JqlUtils.CreatedBy) { DbType = "INT", Size = null, AllowNull = false }));
            dbTable.Columns.Add(SetAndGetColumnState(historyTable, new(JqlUtils.CreatedOn) { DbType = "DATETIME", AllowNull = false }));

            if (masterUpdateQ.Columns is null) return;
            foreach (JqlQueryColumn dbQueryColumn in masterUpdateQ.Columns)
            {
                if (dbQueryColumn.Name is not null && dbQueryColumn.Name != pk.Name)
                {
                    JqlColumn dbColumn = jqlModel.GetColumn(dbQueryColumn.Name);
                    dbTable.Columns.Add(SetAndGetColumnState(historyTable, new(dbColumn.Name) { State = "n", DbType = dbColumn.DbType, Size = dbColumn.Size, AllowNull = dbColumn.AllowNull }));
                }
            }

            DbSchemaUtils.CreateOrAlterTable(dbTable);
            Thread.Sleep(250);
            historyTable = DbSchemaUtils.GetTables().FirstOrDefault(i => i.Name.EqualsIgnoreCase(historyTableName));
            RemoveServerObjectsFor(historyTableName);
            CreateServerObjectsFor(historyTable, false);
        }

		public void CreateQuery(string objectName, string methodType, string methodName)
		{
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            if (!Enum.TryParse<QueryType>(methodType, ignoreCase: true, out var queryType))
                throw new PowNetException("QueryTypeNotSupported", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("MethodType", methodType)
                    .GetEx();

			JqlQuery dbQ = queryType switch
			{
				QueryType.Create => GetCreateQuery(jqlModel),
				QueryType.ReadList => GetReadListQuery(jqlModel, JqlModelFolderPath),
				QueryType.AggregatedReadList => GetAggregatedReadListQuery(jqlModel, JqlModelFolderPath),
				QueryType.ReadByKey => GetReadByKeyQuery(jqlModel),
				QueryType.UpdateByKey => GenOrGetUpdateByKeyQuery(jqlModel, methodName),
				_ => throw new PowNetException("QueryTypeNotSupported", System.Reflection.MethodBase.GetCurrentMethod())
										.AddParam("QueryType", queryType)
                                        .GetEx(),
			};

            dbQ.Name = methodName;
			jqlModel.DbQueries.Add(dbQ);

			jqlModel.Save();
		}
		public void RemoveQuery(string objectName, string methodName)
        {
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            JqlQuery? dbQuery = jqlModel.DbQueries.FirstOrDefault(s => s.Name == methodName);
            if (dbQuery == null) return;

            if (dbQuery.Columns is not null)
                foreach (JqlQueryColumn dbQueryColumn in dbQuery.Columns)
                    if (dbQueryColumn.Name is not null && jqlModel.GetColumn(dbQueryColumn.Name).UpdateGroup == methodName)
                        jqlModel.GetColumn(dbQueryColumn.Name).UpdateGroup = "";

            if (!dbQuery.HistoryTable.IsNullOrEmpty()) RemoveServerObjectsFor(dbQuery.HistoryTable);

            jqlModel.DbQueries.Remove(dbQuery);
            jqlModel.Save();
            DynamicCodeService.RemoveMethod($"{DbConfName}.{objectName}.{methodName}");
            DynamicCodeService.Refresh();
        }
        public void DuplicateQuery(string objectName, string methodName, string methodCopyName)
        {
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            JqlQuery? dbQuery = jqlModel.DbQueries.FirstOrDefault(s => s.Name == methodName);
            if (dbQuery is null) return;

            string tempString = dbQuery.ToJsonStringByBuiltIn(true, false);
            JqlQuery? dbQueryCopy = JsonExtensions.TryDeserializeTo<JqlQuery>(tempString) ?? throw new PowNetException("DeserializeError", System.Reflection.MethodBase.GetCurrentMethod())
                    .AddParam("ObjectName", objectName)
                    .AddParam("MethodName", methodName)
                    .GetEx();
            dbQueryCopy.Name = methodCopyName;
            jqlModel.DbQueries.Add(dbQueryCopy);
            jqlModel.Save();
        }
        public void ReCreateMethodJson(DbObject? dbObject, string methodName)
        {
            if(dbObject == null) return;
            JqlModel jqlModel = JqlModel.Load(JqlModelFolderPath, DbConfName, dbObject.Name);
            var theQuery = jqlModel.DbQueries.FirstOrDefault(i => i.Name == methodName);
            if (theQuery is null) return;

            theQuery = theQuery.Type switch
            {
                QueryType.Create => GetCreateQuery(jqlModel),
                QueryType.ReadByKey => GetReadByKeyQuery(jqlModel),
                QueryType.ReadList => GetReadListQuery(jqlModel, JqlModelFolderPath),
                QueryType.UpdateByKey => GenOrGetUpdateByKeyQuery(jqlModel, methodName),
                QueryType.DeleteByKey => GetDeleteByKeyQuery(jqlModel),
                QueryType.Procedure => GetExecQuery(jqlModel, DbSchemaUtils),
                QueryType.TableFunction => GetSelectForTableFunction(jqlModel, DbSchemaUtils),
                QueryType.ScalarFunction => GetSelectForScalarFunction(jqlModel, DbSchemaUtils),
                _ => throw new PowNetException("QueryTypeNotSupported", System.Reflection.MethodBase.GetCurrentMethod())
                                        .AddParam("QueryType", theQuery.Type)
                                        .GetEx(),
            };
            jqlModel.Save();
        }


        public void CreateServerObjectsFor(DbObject? dbObject, bool? createAdditionalUpdateByKeyQueries = true)
        {
			if (dbObject == null) return;

			PowNetClassGenerator cGen = new(dbObject.Name, DbConfName);

            JqlModel jqlModel = new(DbConfName, dbObject.Name, JqlModelFolderPath)
            {
                ObjectName = dbObject.Name,
                ObjectType = dbObject.DbObjectType,
                DbConfName = DbConfName
            };

            if (dbObject.DbObjectType == DbObjectType.Table || dbObject.DbObjectType == DbObjectType.View)
            {
                List<JqlColumn> dbColumns = DbSchemaUtils.GetTableViewColumns(dbObject.Name);
                foreach (JqlColumn dbColumn in dbColumns)
                {
                    dbColumn.IsHumanId = dbColumn.ColumnIsForDisplay() ? true : null;
                    dbColumn.IsSortable = dbColumn.ColumnIsSortable() ? true : null;
                }

                jqlModel.Columns.AddRange(dbColumns);

                jqlModel.Relations = GetRelations(jqlModel, DbSchemaUtils);

                // set moreinfo items
                jqlModel.ObjectIcon = jqlModel.IsTree() ? "fa-tree" : "fa-list";
                if (jqlModel.GetTreeParentColumnName() != "") jqlModel.ParentColumn = jqlModel.GetTreeParentColumnName();
                if (jqlModel.TryGetColumn("Note") is not null) jqlModel.NoteColumn = "Note";
                if (jqlModel.TryGetColumn("ViewOrder") is not null) jqlModel.ViewOrderColumn = "ViewOrder";
                if (jqlModel.TryGetColumn("UiColor") is not null) jqlModel.UiColorColumn = "UiColor";
                if (jqlModel.TryGetColumn("UiIcon") is not null) jqlModel.UiIconColumn = "UiIcon";
            }

            if (dbObject.DbObjectType == DbObjectType.Table)
            {
                jqlModel.DbQueries.Add(GetReadListQuery(jqlModel, JqlModelFolderPath));
                jqlModel.DbQueries.Add(GetCreateQuery(jqlModel));
                jqlModel.DbQueries.Add(GetReadByKeyQuery(jqlModel));
                jqlModel.DbQueries.Add(GenOrGetUpdateByKeyQuery(jqlModel, "UpdateByKey"));
                jqlModel.DbQueries.Add(GetDelete(jqlModel));
                jqlModel.DbQueries.Add(GetDeleteByKeyQuery(jqlModel));

                cGen.JqlModelMethods.Add(nameof(QueryType.ReadList));
                cGen.JqlModelMethods.Add(nameof(QueryType.Create));
                cGen.JqlModelMethods.Add(nameof(QueryType.ReadByKey));
                cGen.JqlModelMethods.Add(nameof(QueryType.UpdateByKey));
                cGen.JqlModelMethods.Add(nameof(QueryType.Delete));
                cGen.JqlModelMethods.Add(nameof(QueryType.DeleteByKey));
            }
            else if (dbObject.DbObjectType == DbObjectType.View)
            {
                jqlModel.DbQueries.Add(GetReadListQuery(jqlModel, JqlModelFolderPath));
                cGen.JqlModelMethods.Add(nameof(QueryType.ReadList));
            }

            jqlModel.Save();

            // generating controller file
            string csharpFileContent = cGen.ToCode();
            string csharpFilePath = JqlModel.GetFullFilePath(JqlModelFolderPath, DbConfName, dbObject.Name).Replace(".jqlmodel.json", ".cs");
            File.WriteAllText(csharpFilePath, csharpFileContent);
            DynamicCodeService.Refresh();
        }
        public void RemoveServerObjectsFor(string? dbObjectName)
        {
            if (dbObjectName == null) return;
            string dbDialogFilePath = JqlModel.GetFullFilePath(JqlModelFolderPath, DbConfName, dbObjectName);
            string settingsFilePath = dbDialogFilePath.Replace(".jqlmodel.json", ".settings.json");
            string csharpFilePath = dbDialogFilePath.Replace(".jqlmodel.json", ".cs");
            if (File.Exists(dbDialogFilePath)) { File.Delete(dbDialogFilePath); }
            if (File.Exists(settingsFilePath)) { File.Delete(settingsFilePath); }
            if (File.Exists(csharpFilePath)) { File.Delete(csharpFilePath); }
        }
        public void SyncDbDialog(string objectName)
        {
            JqlModel? dbDialog = JqlModel.TryLoad(JqlModelFolderPath, DbConfName, objectName);
            if (dbDialog == null) return;
            List<JqlColumn> dbColumns = DbSchemaUtils.GetTableViewColumns(objectName);

            foreach (JqlColumn dbColumn in dbColumns)
            {
                var lst = dbDialog.Columns.Where(i => i.Name == dbColumn.Name).ToList();
                if (lst.Count == 0) dbDialog.Columns.Add(dbColumn);
            }

            List<JqlColumn> toRemove = [];
            foreach (JqlColumn dbColumn in dbDialog.Columns)
            {
                var lst = dbColumns.Where(i => i.Name == dbColumn.Name).ToList();
                if (lst.Count == 0)
                {
                    toRemove.Add(dbColumn);
                }
            }

            foreach (JqlColumn dbColumn in toRemove)
            {
                dbDialog.Columns.Remove(dbColumn);
                foreach (JqlQuery dbQuery in dbDialog.DbQueries)
                {
                    if (dbQuery.Columns?.Count > 0)
                    {
                        JqlQueryColumn? dbQueryColumn = dbQuery.Columns.FirstOrDefault(i => i.Name == dbColumn.Name);
                        if (dbQueryColumn != null) dbQuery.Columns.Remove(dbQueryColumn);
                    }
                }
            }

            dbDialog.Save();
        }
        public void RemoveRemovedRelationsFromDbQueries(string objectName)
        {
            JqlModel dbDialog = JqlModel.Load(JqlModelFolderPath, DbConfName, objectName);
            int initialDbQueriesCount = dbDialog.DbQueries.Count;
            foreach (JqlQuery dbQuery in dbDialog.DbQueries)
            {
                if (dbQuery.Relations is not null && dbQuery.Relations.Count > 0)
                {
                    List<string> toRemove = [];
                    foreach (string dbRelationName in dbQuery.Relations)
                    {
                        JqlRelation? dbRelation = dbDialog.Relations?.FirstOrDefault(i => i.RelationName == dbRelationName);
                        if (dbRelation == null) toRemove.Add(dbRelationName);
                    }
                    foreach (string s in toRemove) dbQuery.Relations.Remove(s);
                }
            }
            if (dbDialog.DbQueries.Count != initialDbQueriesCount) dbDialog.Save();
        }

        public void SynchDbDirectMethods()
        {
            string objectName = "DbDirect";
			PowNetClassGenerator cGen = new(objectName, DbConfName);

			DbSchemaUtils dbSchemaUtils = new(DbConfName);

			List<DbObject> procedures = dbSchemaUtils.GetObjects(DbObjectType.Procedure, "");
			foreach (DbObject o in procedures)
			{
				cGen.DbProducerMethods.Add(o.Name, DbParamsToCsharpParams(o.Name));
			}

			List<DbObject> scalarFunctions = dbSchemaUtils.GetObjects(DbObjectType.ScalarFunction, "");
			foreach (DbObject o in scalarFunctions)
			{
				cGen.DbScalarFunctionMethods.Add(o.Name, DbParamsToCsharpParams(o.Name));
			}

			List<DbObject> tableFunctions = dbSchemaUtils.GetObjects(DbObjectType.TableFunction, "");
			foreach (DbObject o in tableFunctions)
			{
				cGen.DbTableFunctionMethods.Add(o.Name, DbParamsToCsharpParams(o.Name));
			}

			// generating controller file
			string csharpFileContent = cGen.ToCode();
			string csharpFilePath = JqlModel.GetFullFilePath(JqlModelFolderPath, DbConfName, objectName).Replace(".jqlmodel.json", ".cs");
			File.WriteAllText(csharpFilePath, csharpFileContent);
			DynamicCodeService.Refresh();
		}

        private List<string> DbParamsToCsharpParams(string objectName)
        {
			List<string> inputParams = [];
			List<JqlParam>? dbParams = DbSchemaUtils.GetProceduresFunctionsParameters(objectName);
			if (dbParams != null)
			{
				dbParams = dbParams.Where(i => !i.Name.EqualsIgnoreCase("Returns")).ToList();
				if (dbParams.Count > 0)
				{
					foreach (JqlParam dbParam in dbParams)
					{
						inputParams.Add(DbIOInstance.DbParamToCSharpInputParam(dbParam));
					}
				}
			}
            return inputParams;
		}

		#region LogicalFk
		public void CreateLogicalFk(string fkName, string baseTable, string baseColumn, string targetTable, string targetColumn)
        {
            JqlModel dbDialog = JqlModel.Load(JqlModelFolderPath, DbConfName, baseTable);
            JqlColumn dbColumn = dbDialog.GetColumn(baseColumn);
            dbColumn.Fk = new(fkName, targetTable, targetColumn) { EnforceRelation = false };
            dbDialog.Save();
        }
        public void RemoveLogicalFk(string baseTable, string baseColumn)
        {
            JqlModel dbDialog = JqlModel.Load(JqlModelFolderPath, DbConfName, baseTable);
            JqlColumn dbColumn = dbDialog.GetColumn(baseColumn);
            dbColumn.Fk = null;
            dbDialog.Save();
        }
        #endregion

        public static List<JqlRelation>? GetRelations(JqlModel dbDialog,DbSchemaUtils dbSchemaUtils)
        {
            if (dbDialog.ObjectName.EndsWithIgnoreCase("BaseInfo")) return null;
            List<JqlRelation> list = [];
            List<DbTable> tables = dbSchemaUtils.GetTables();
            foreach (DbTable table in tables)
            {
                List<JqlColumn> dbColumns = dbSchemaUtils.GetTableViewColumns(table.Name).RemoveAuditingColumns();
                JqlColumn? tablePk = dbColumns.FirstOrDefault(i => i.IsPrimaryKey == true);
                if (tablePk != null)
                {
                    bool fileCentric = JqlUtils.ColumnsAreFileCentric(dbColumns);
                    JqlColumn? fkToThis = dbColumns.FirstOrDefault(i => i.Fk != null && i.Fk.TargetTable == dbDialog.ObjectName);
                    if (fkToThis != null)
                    {
                        JqlRelation otm = new(table.Name, tablePk.Name, fkToThis.Name)
                        {
                            RelationType = RelationType.OneToMany,
                            CreateQuery = "Create",
                            ReadListQuery = "ReadList",
                            UpdateByKeyQuery = "UpdateByKey",
                            DeleteQuery = "Delete",
                            DeleteByKeyQuery = "DeleteByKey",
                            IsFileCentric = fileCentric
                        };
                        if (dbColumns.Count == 3) // it is a ManyToMany table
                        {
                            JqlColumn? md3 = dbColumns.FirstOrDefault(i => i.Name != fkToThis.Name && i.Name != tablePk.Name);
                            if (md3 != null && md3.Fk is not null)
                            {
                                otm.RelationType = RelationType.ManyToMany;
                                otm.LinkingTargetTable = md3.Fk?.TargetTable;
                                otm.LinkingColumnInManyToMany = md3.Name;
                                otm.RelationUiWidget = md3.Fk?.TargetTable.ContainsIgnoreCase("tags") == true ? RelationUiWidget.AddableList : RelationUiWidget.CheckboxList;
                            }
                        }
                        else
                        {
                            if (fileCentric) otm.RelationUiWidget = RelationUiWidget.Cards;
                            else otm.RelationUiWidget = RelationUiWidget.Grid;
                        }
                        list.Add(otm);
                    }
                }
            }
            if(list.Count > 0) return list;
            return null;
        }
		public static JqlQuery GetAggregatedReadListQuery(JqlModel dbDialog, string dbDialogFolderPath)
		{
			JqlQuery dbQuery = new(nameof(QueryType.AggregatedReadList), QueryType.AggregatedReadList) { Columns = [] };
			foreach (JqlColumn col in dbDialog.Columns)
			{
				if (col.ColumnIsForAggregatedReadList())
				{
					JqlQueryColumn dbQueryColumn = new() { Name = col.Name };
					if (col.Fk is not null && JqlModel.Exist(dbDialogFolderPath, dbDialog.DbConfName, col.Fk?.TargetTable))
					{
						dbQueryColumn.RefTo = new(col.Fk.TargetTable, col.Fk.TargetColumn)
						{
							Columns = []
						};

						JqlModel dbDialogTarget = JqlModel.Load(dbDialogFolderPath, dbDialog.DbConfName, col.Fk?.TargetTable);
						foreach (var targetCol in dbDialogTarget.Columns)
						{
							if (targetCol.Name.ContainsIgnoreCase("Title") || targetCol.Name.ContainsIgnoreCase("Name"))
							{
								dbQueryColumn.RefTo.Columns.Add(new() { Name = targetCol.Name, As = $"{col.Name}_{targetCol.Name}" });
							}
						}
						if (dbQueryColumn.RefTo.Columns.Count == 0)
						{
							JqlColumn? dbColumn = dbDialogTarget.Columns.FirstOrDefault(i => !i.IsPrimaryKey);
							if (dbColumn == null)
							{
								dbQueryColumn.RefTo = null;
							}
							else
							{
								dbQueryColumn.RefTo?.Columns.Add(new() { Name = dbColumn.Name, As = $"{col.Name}_{dbColumn.Name}" });
							}
						}
					}
					dbQuery.Columns.Add(dbQueryColumn);
				}
			}
			dbQuery.PaginationMaxSize = 100;
			dbQuery.Aggregations = [new JqlAggregation("Count", "COUNT(*)")];
			return dbQuery;
		}
		public static JqlQuery GetReadListQuery(JqlModel dbDialog, string dbDialogFolderPath)
		{
			JqlQuery dbQuery = new(nameof(QueryType.ReadList), QueryType.ReadList) { Columns = [] };
			foreach (JqlColumn col in dbDialog.Columns)
			{
				if (col.ColumnIsForReadList())
				{
					JqlQueryColumn dbQueryColumn = new() { Name = col.Name };
					if (col.Fk is not null && JqlModel.Exist(dbDialogFolderPath, dbDialog.DbConfName, col.Fk?.TargetTable))
					{
						dbQueryColumn.RefTo = new(col.Fk.TargetTable, col.Fk.TargetColumn) { Columns = [] };

						JqlModel dbDialogTarget = JqlModel.Load(dbDialogFolderPath, dbDialog.DbConfName, col.Fk?.TargetTable);

						foreach (var targetCol in dbDialogTarget.Columns)
							if (targetCol.Name.ContainsIgnoreCase("Title") || targetCol.Name.ContainsIgnoreCase("Name"))
								dbQueryColumn.RefTo.Columns.Add(new() { Name = targetCol.Name, As = $"{col.Name}_{targetCol.Name}" });

						if (dbQueryColumn.RefTo.Columns.Count == 0)
						{
							JqlColumn? dbColumn = dbDialogTarget.Columns.FirstOrDefault(i => !i.IsPrimaryKey);
							if (dbColumn == null) dbQueryColumn.RefTo = null;
							else dbQueryColumn.RefTo?.Columns.Add(new() { Name = dbColumn.Name, As = $"{col.Name}_{dbColumn.Name}" });
						}
					}
					dbQuery.Columns.Add(dbQueryColumn);
				}
			}
			dbQuery.PaginationMaxSize = 100;
			dbQuery.Relations = GetRelationsForDbQueries(dbQuery, dbDialog.Relations);
			dbQuery.Aggregations = [new JqlAggregation("Count", "COUNT(*)")];
			return dbQuery;
		}
		public static JqlQuery GetSelectForScalarFunction(JqlModel dbDialog, DbSchemaUtils dbSchemaUtils)
		{
			return new("Calculate", QueryType.ScalarFunction) { Params = dbSchemaUtils.GetProceduresFunctionsParameters(dbDialog.ObjectName) };
		}
		public static JqlQuery GetSelectForTableFunction(JqlModel dbDialog, DbSchemaUtils dbSchemaUtils)
		{
			return new("Select", QueryType.TableFunction) { Params = dbSchemaUtils.GetProceduresFunctionsParameters(dbDialog.ObjectName) };
		}
		public static JqlQuery GetExecQuery(JqlModel dbDialog, DbSchemaUtils dbSchemaUtils)
		{
			return new("Exec", QueryType.Procedure) { Params = dbSchemaUtils.GetProceduresFunctionsParameters(dbDialog.ObjectName) };
		}
		public static JqlQuery GetCreateQuery(JqlModel dbDialog)
		{
			JqlQuery dbQuery = new(nameof(QueryType.Create), QueryType.Create) { Columns = [], Params = [] };
			foreach (JqlColumn col in dbDialog.Columns)
			{
				if (col.ColumnIsForCreate())
				{
                    JqlQueryColumn dbQueryColumn = new();
					if (col.Name.EqualsIgnoreCase(JqlUtils.CreatedBy) || col.Name.EqualsIgnoreCase(JqlUtils.UpdatedBy))
					{
                        dbQueryColumn.As = col.Name;
                        dbQueryColumn.Phrase = "$UserId$";
					}
					if (col.Name.EqualsIgnoreCase(JqlUtils.CreatedOn) || col.Name.EqualsIgnoreCase(JqlUtils.UpdatedOn))
					{
						dbQueryColumn.As = col.Name;
						dbQueryColumn.Phrase = "GETDATE()";
					}

                    if (dbQueryColumn.As.IsNullOrEmpty())
                    {
                        dbQueryColumn.Name = col.Name;
                    }

					dbQuery.Columns.Add(dbQueryColumn);
					if (col.Name.EndsWithIgnoreCase("_xs"))
						dbQuery.Params.Add(new JqlParam(col.Name, col.DbType) { ValueSharp = GetValueSharpForImage(col.Name), Size = col.Size, AllowNull = col.AllowNull });
				}
			}
			dbQuery.Relations = GetRelationsForDbQueries(dbQuery, dbDialog.Relations);
			return dbQuery;
		}
		public static JqlQuery GetReadByKeyQuery(JqlModel dbDialog)
		{
			JqlColumn pkColumn = dbDialog.GetPk();
			JqlQuery dbQuery = new(nameof(QueryType.ReadByKey), QueryType.ReadByKey) { Columns = [] };
			foreach (JqlColumn col in dbDialog.Columns) if (col.ColumnIsForReadByKey()) dbQuery.Columns.Add(new JqlQueryColumn() { Name = col.Name });
			dbQuery.Where = GetByPkWhere(pkColumn, dbDialog);
			dbQuery.Relations = GetRelationsForDbQueries(dbQuery, dbDialog.Relations);
			return dbQuery;
		}
        public static JqlQuery GenOrGetUpdateByKeyQuery(JqlModel dbDialog, string? UpdateByKeyApiName, List<string>? specificColumns = null, string? byColName = null, string? onColName = null)
        {
            if(UpdateByKeyApiName == null || UpdateByKeyApiName.IsNullOrEmpty()) throw new PowNetException("UpdateApiNameBanNotBeNullOrEmpty", System.Reflection.MethodBase.GetCurrentMethod())
					.AddParam("DbDialog", dbDialog)
					.GetEx();

			bool isMainUpdateByKey = specificColumns is null || specificColumns.Count == 0 ? true : false;
            JqlColumn pkColumn = dbDialog.GetPk();

            JqlQuery? existingUpdateByKeyQ = dbDialog.DbQueries.FirstOrDefault(i => i.Name.EqualsIgnoreCase(UpdateByKeyApiName));
            existingUpdateByKeyQ ??= new(nameof(QueryType.UpdateByKey), QueryType.UpdateByKey) { Columns = [], Params = [] };
            existingUpdateByKeyQ.Name = UpdateByKeyApiName;

			foreach (JqlColumn col in dbDialog.Columns)
			{
				if ((isMainUpdateByKey == true && col.ColumnIsForUpdateByKey()) || (isMainUpdateByKey == false && specificColumns.ContainsIgnoreCase(col.Name)))
				{
                    if(existingUpdateByKeyQ.Columns?.FirstOrDefault(c=>c.Name.EqualsIgnoreCase(col.Name)) is null)
                    {
                        JqlQueryColumn dbQueryColumn = new();
						if (col.Name.EqualsIgnoreCase(JqlUtils.CreatedBy) || col.Name.EqualsIgnoreCase(JqlUtils.UpdatedBy))
						{
							dbQueryColumn.As = col.Name;
							dbQueryColumn.Phrase = "$UserId$";
						}
						if (col.Name.EqualsIgnoreCase(JqlUtils.CreatedOn) || col.Name.EqualsIgnoreCase(JqlUtils.UpdatedOn))
						{
							dbQueryColumn.As = col.Name;
							dbQueryColumn.Phrase = "GETDATE()";
						}

						if (dbQueryColumn.As.IsNullOrEmpty())
						{
							dbQueryColumn.Name = col.Name;
						}

						existingUpdateByKeyQ.Columns?.Add(dbQueryColumn);
						if (col.Name.EndsWithIgnoreCase("_xs"))
                            existingUpdateByKeyQ.Params?.Add(new JqlParam(col.Name, col.DbType) { ValueSharp = GetValueSharpForImage(col.Name), Size = col.Size, AllowNull = col.AllowNull });
                    }
                }
			}
			existingUpdateByKeyQ.Where = GetByPkWhere(pkColumn, dbDialog);
			if (isMainUpdateByKey == true) existingUpdateByKeyQ.Relations = GetRelationsForDbQueries(existingUpdateByKeyQ, dbDialog.Relations);

			return existingUpdateByKeyQ;
        }
		public static JqlWhere GetByPkWhere(JqlColumn pkColumn, JqlModel dbDialog)
		{
			return new() { SimpleClauses = [new ComparePhrase(JqlUtils.GetSetColumnParamPair(dbDialog.ObjectName, pkColumn.Name, null))] };
		}
		public static List<string>? GetRelationsForDbQueries(JqlQuery dbQuery, List<JqlRelation>? dbRelations)
        {
            if (dbRelations == null) return null;
            if (dbQuery.Name == "ReadByKey" || dbQuery.Name == "UpdateByKey" || dbQuery.Name == "Create")
                return dbRelations.Select(i => i.RelationName).ToList();
            else if (dbQuery.Name == "ReadList")
                return dbRelations.Where(i => i.RelationType == RelationType.ManyToMany).Select(i => i.RelationName).ToList();
            else return null;
        }
		public static JqlQuery GetDelete(JqlModel dbDialog)
		{
			JqlQuery dbQuery = new(nameof(QueryType.Delete), QueryType.Delete) { Columns = [] };

			foreach (JqlColumn col in dbDialog.Columns)
				if (col.ColumnIsForDelete()) dbQuery.Columns.Add(new JqlQueryColumn() { Name = col.Name });

			return dbQuery;
		}
		public static JqlQuery GetDeleteByKeyQuery(JqlModel dbDialog)
		{
			JqlColumn pkColumn = dbDialog.GetPk();
			JqlQuery dbQuery = new(nameof(QueryType.DeleteByKey), QueryType.DeleteByKey)
			{
				Columns = [new JqlQueryColumn() { Name = pkColumn.Name }],
				Where = GetByPkWhere(pkColumn, dbDialog)
			};
			dbQuery.Relations = GetRelationsForDbQueries(dbQuery, dbDialog.Relations);

			return dbQuery;
		}
		public static string GetTemplateName(JqlModel dbDialog, JqlQuery dbQuery)
        {
            if (dbQuery.Type == QueryType.ReadList && dbDialog.IsTree()) return "ReadTreeList";
			return dbQuery.Type.ToString();
        }
        public static bool IsDbQueryTypeSuitableForClientUI(QueryType qT)
        {
            if (qT == QueryType.ReadList || qT == QueryType.AggregatedReadList || qT == QueryType.Create || qT == QueryType.UpdateByKey) return true;
            return false;
        }
		public static string GetValueSharpForImage(string columnName)
		{
			return $"#Resize:{columnName.Replace("_xs", "")},75";
		}
		public static string GetValueSharpForNow()
		{
			return $"#Now";
		}
		public static string GetValueSharpForContext(string contextName)
		{
			return $"#Context:{contextName}";
		}
        public static string GetClientUIComponentName(string dbConfName, string objectName, string endfixName)
        {
            if(dbConfName.EqualsIgnoreCase(PowNetConfiguration.GetConnectionStringByName(dbConfName))) return $"{objectName}_{endfixName}";
			return $"{dbConfName}_{objectName}_{endfixName}";
        }

        private static JqlColumnChangeTrackable SetAndGetColumnState(DbTable? dbObject, JqlColumnChangeTrackable col)
        {
            col.State = dbObject is null || dbObject.Columns.FirstOrDefault(i => i.Name.EqualsIgnoreCase(col.Name)) is null ? "n" : "u";
            return col;
        }

    }
}
