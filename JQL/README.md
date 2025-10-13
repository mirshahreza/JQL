JQL
====

A lightweight query and model orchestration layer for .NET that builds SQL statements from strongly typed models and request objects, with first-class support for CRUD, custom procedures/functions, relations, paging, sorting, and change-tracking.

Key characteristics
- Target framework: .NET 10 (preview at the time of writing)
- Uses modern C# features (primary constructors, records)
- Relies on PowNet for configuration, data access, and utility extensions
- First-class support for Microsoft SQL Server (via DbIOMsSql)

Requirements
- A configured PowNet environment with at least one connection string (e.g., DefaultConnection).
- SQL Server with helper procs/views used by DbSchemaUtils:
  - Zz_SelectObjectsDetails, Zz_SelectTablesViewsColumns, Zz_SelectTablesFks
  - DDL wrappers like Zz_CreateColumn, Zz_AlterColumn, Zz_DropColumn, etc.

Core concepts
- JqlModel: Describes a DB object (table/view) and its queries/relations. Models persist as JSON files: ${DbConfName}.${ObjectName}.jqlmodel.json under PowNetConfiguration.ServerPath.
- JqlQuery: A query definition bound to a model. Common types: Create, ReadList, AggregatedReadList, ReadByKey, UpdateByKey, Delete, DeleteByKey, Procedure, TableFunction, ScalarFunction.
- JqlRequest: A runnable request composed from a JqlQuery that you enrich with runtime Params, Where, Order, Pagination, and relation inputs. It compiles final SQL and executes via JqlRun.
- JqlRun: DB executor abstraction. DbIOMsSql implements SQL Server specifics (parameter creation, SQL templates, etc.).
- DbSchemaUtils: Introspects DB objects/columns, generates/updates tables, FKs, and scripts.
- JqlModelFactory: High-level automation to generate queries, server objects, and history tables from a DB object.

Getting started
1) Ensure PowNetConfiguration.ServerPath points to a writable folder where model JSON files can be stored.
2) Use JqlModelFactory to generate model and standard queries, or author a JqlModel JSON manually.
3) Execute by name: DbConfName.ObjectName.QueryName.

Examples

Read list with filters, order, and pagination
```
using System.Collections;
using JQL;
using PowNet.Common;

var ctx = new Hashtable
{
    ["UserId"] = 1,
    ["UserName"] = "demo"
};

var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.ReadList", ctx);

req.Where = new JqlWhere
{
    ConjunctiveOperator = ConjunctiveOperator.AND,
    CompareClauses =
    [
        new CompareClause("IsActive", true) { CompareOperator = CompareOperator.Equal },
        new CompareClause("Name", "Ali") { CompareOperator = CompareOperator.StartsWith }
    ]
};

req.OrderClauses = [ new JqlOrderClause("CreatedOn", OrderDirection.DESC) ];
req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 20 };

var result = req.Exec(); // DataTables (or DataSet when aggregations requested)
```

Read by key
```
var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.ReadByKey");
req.Params = [ new JqlParamRaw("Id", 123) ];
var dt = req.Exec();
```

Create (returns new primary key)
```
var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.Create");
req.Params =
[
    new JqlParamRaw("Name", "Jane"),
    new JqlParamRaw("IsActive", true)
];
var newId = req.Exec(); // scalar
```

Update by key with relations (One-to-Many / Many-to-Many)
```
var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.UpdateByKey");
req.Params =
[
    new JqlParamRaw("Id", 123),
    new JqlParamRaw("Name", "Updated")
];

// Attach relation rows (e.g., Many-to-Many tags)
req.Relations = new()
{
    ["UserTags"] =
    [
        // create
        [ new JqlParamRaw("TagId", 10) ],
        // delete (mark row with _flag_ = d)
        [ new JqlParamRaw("TagId", 11), new JqlParamRaw("_flag_", "d") ]
    ]
};

req.Exec();
```

Delete by key
```
var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.Users.DeleteByKey");
req.Params = [ new JqlParamRaw("Id", 123) ];
req.Exec();
```

Call a stored procedure
```
var req = JqlRequest.GetInstanceByQueryName("DefaultConnection.DbDirect.Exec");
// assuming a DbDirect model with an Exec query
req.Params = [ new JqlParamRaw("CustomerId", 5) ];
var ds = req.Exec(); // DataSet
```

Table/scalar function
```
var tf = JqlRequest.GetInstanceByQueryName("DefaultConnection.Reports.Select");
tf.Params = [ new JqlParamRaw("Year", 2024) ];
var rows = tf.Exec(); // DataTables

var sf = JqlRequest.GetInstanceByQueryName("DefaultConnection.Formulas.Calculate");
sf.Params = [ new JqlParamRaw("x", 2) ];
var value = sf.Exec(); // Scalar
```

Building Where, Order, and Pagination
- Where: build with JqlWhere, CompareClause, CompareOperator, ConjunctiveOperator.
- Order: provide JqlOrderClause items; if omitted, engine falls back to PK (or first selected column/aggregation in aggregated queries).
- Pagination: set JqlPagination; query can define PaginationMaxSize to cap page size.

Controlling selected columns, aggregations, relations
JqlRequest provides containment flags and name lists:
- ColumnsContainment: IncludeIndicatedItems, ExcludeIndicatedItems, IncludeAll
- AggregationsContainment: same idea
- RelationsContainment: include, exclude, or exclude all
- ClientIndicatedColumns, ClientIndicatedAggregations, ClientIndicatedRelations

Relations
- Define in JqlModel.Relations via JqlRelation (e.g., OneToMany, ManyToMany).
- GetRelationsForDbQueries includes all for ReadByKey, UpdateByKey, Create and only ManyToMany for ReadList by default.
- At runtime, narrow via RelationsContainment and ClientIndicatedRelations.

UI helpers
JqlColumn.CalculateBestUiWidget() suggests UI widgets like Checkbox, DatePicker, ImageView, Htmlbox, etc. Useful for UI generators.

Auditing and rules
- Auditing fields: JqlUtils.AuditingFields. Helper rules define column participation in queries: ColumnIsForReadList, ColumnIsForCreate, ColumnIsForUpdateByKey, etc.
- File-centric detection: JqlUtils.ColumnsAreFileCentric helps classify relations and UI.

Generating models and queries with JqlModelFactory
```
var factory = new JqlModelFactory("DefaultConnection");

// Create standard queries and controller for a table
factory.CreateServerObjectsFor(new DbTable("Users"));

// Create/refresh a history table and related artifacts for a partial update API
factory.CreateOrAlterHistoryTable(
    objectName: "Users",
    updateQueryName: "UpdateByKey_Partial",
    historyTableName: "UsersHistory");

// Create a new partial UpdateByKey that updates specific columns and keeps history
factory.CreateNewUpdateByKey(
    objectName: "Users",
    readByKeyApiName: "ReadByKey_Compact",
    columnsToUpdate: new() { "Name", "IsActive" },
    partialUpdateApiName: "UpdateByKey_Partial",
    byColumnName: JqlUtils.UpdatedBy,
    onColumnName: JqlUtils.UpdatedOn,
    historyTableName: "UsersHistory");
```

Direct DB objects sync (DbDirect)
JqlModelFactory.SynchDbDirectMethods() scans procedures and functions and generates method signatures (C# param mapping inferred by DbIOMsSql.DbParamToCSharpInputParam).

Sharp parameters (ValueSharp)
JqlParam.ValueSharp directives:
- #Now -> current timestamp
- #Context:Key -> value from UserContext
- #Resize:ColName,Size -> resize image bytes from another param
- #ToMD5: / #ToMD4: -> hash a string

Error handling
- APIs throw PowNetException enriched with context (method, parameters). Catch and inspect for diagnostics.
- JqlRequest.Exec() wraps SQL exceptions and includes the final SQL string when available.

Testing
- Unit tests cover columns, utils, records, query flags, and request behavior.
- Integration tests that need a real DB are marked [Fact(Skip = ...)] by default. Enable by configuring PowNet and a reachable database.

FAQ
- Add a custom query? Use JqlModelFactory.CreateQuery(objectName, methodType, methodName) to scaffold common query types, then edit the model JSON as needed.
- Where are model files? Under PowNetConfiguration.ServerPath, named ${DbConfName}.${ObjectName}.jqlmodel.json.
- Can I use it without models? Most features rely on JqlModel/JqlQuery metadata; generate or author the models first.

Notes
- .NET 10 is currently preview; pin the SDK and monitor breaking changes.
- Ensure required DB helper procs exist for DbSchemaUtils.
