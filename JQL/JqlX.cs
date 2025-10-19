using PowNet.Common;
using System.Text.Json;

namespace JQL
{
    public record class JqlColumn(string Name)
    {
        public string DevNote { set; get; } = "";
        public bool IsPrimaryKey { set; get; } = false;
        public string DbType { set; get; } = "VARCHAR";
        public string? Size { set; get; }
        public bool IsIdentity { set; get; } = false;
        public string? IdentityStart { set; get; }
        public string? IdentityStep { set; get; }
        public bool AllowNull { set; get; }
        public string? DbDefault { set; get; }
        public JqlFk? Fk { set; get; }
        public bool? IsHumanId { set; get; }
        public bool? IsSortable { set; get; }

        public string? UpdateGroup { set; get; } = "";
    }
    
    public record class JqlColumnChangeTrackable(string Name) : JqlColumn(Name)
    {
        public string State { set; get; } = "";
        public string InitialName { set; get; } = "";
    }

    public record class JqlPagination
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public record class JqlOrderClause
    {
        public string Name { get; set; }
        public OrderDirection OrderDirection { get; set; }

        public JqlOrderClause(string name, OrderDirection orderDirection = OrderDirection.ASC)
        {
            Name = name;
            OrderDirection = orderDirection;
        }
    }

    public record class JqlParam
    {
        public string Name { get; set; }
        public string DbType { get; set; }
        public string? Size { get; set; }
        public bool AllowNull { get; set; } = true;
        public string? ValueSharp { get; set; }
        public object? Value { get; set; }

        public JqlParam(string name, string dbType)
        {
            Name = name;
            DbType = dbType;
        }
    }

    public record class JqlFk
    {
        public string FkName { get; set; }
        public string TargetTable { get; set; }
        public string TargetColumn { get; set; }
        public bool EnforceRelation { get; set; } = false;
        public object? Lookup { get; set; }
        public string? JsLookupParentId { get; set; }

        public JqlFk(string fkName, string targetTable, string targetColumn)
        {
            FkName = fkName;
            TargetTable = targetTable;
            TargetColumn = targetColumn;
        }
    }

    public record class JqlAggregation(string Name, string Phrase);
    public record class JqlParamRaw(string Name, object? Value = null);

    public record class JqlRefTo
    {
        public string TargetTable { get; set; }
        public string TargetColumn { get; set; }
        public List<JqlQueryColumn> Columns { get; set; } = [];
        public JqlRefTo? RefTo { get; set; }

        public JqlRefTo(string targetTable, string targetColumn)
        {
            TargetTable = targetTable;
            TargetColumn = targetColumn;
        }
    }

    public record class JqlRequestRaw
    {
        public string Id { get; init; } = "";
        public string Method { get; init; } = "";
        public JsonElement Inputs { get; init; }
    }
    public record class JqlQueryColumn
    {
        public bool? Hidden { get; set; }
        public string? Name { get; set; }
        public string? Phrase { get; set; }
        public string? As { get; set; }
        public JqlRefTo? RefTo { get; set; }
    }

    public record class JqlWhere
    {
        public ConjunctiveOperator ConjunctiveOperator { get; set; } = ConjunctiveOperator.AND;
        public List<CompareClause>? CompareClauses { get; set; }
        public List<ComparePhrase>? SimpleClauses { get; set; }
        public List<JqlWhere>? ComplexClauses { get; set; }
    }

    public record class CompareClause
    {
        public string Name { get; set; }
        public object? Value { get; set; }
        public CompareOperator CompareOperator { get; set; } = CompareOperator.Equal;

        public CompareClause(string name, object? value = null)
        {
            Name = name;
            Value = value;
        }
    }

    public record class ComparePhrase
    {
        public string Phrase { get; set; }
        public ComparePhrase(string phrase) => Phrase = phrase;
    }

    public record class ClientQueryMetadata
    {
        public List<JqlColumn> ParentObjectColumns { get; set; } = [];
        public string? ParentObjectName { get; set; }
        public string? ParentObjectType { get; set; }

        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> QueryColumns { get; set; } = [];

        public List<JqlColumn> FastSearchColumns { get; set; } = [];
        public List<JqlColumn> ExpandableSearchColumns { get; set; } = [];

        public List<string> OptionalQueries { get; set; } = [];

        public ClientQueryMetadata(string name, string objectTyme)
        {
            Name = name;
            Type = objectTyme;
        }
    }

    public record class DbObject(string name, DbObjectType dbObjectType)
    {
        public string Name { get; init; } = name;
        public DbObjectType DbObjectType { get; init; } = dbObjectType;
    }

    public record class DbTable(string name) : DbObject(name, DbObjectType.Table)
    {
        public List<JqlColumn> Columns { set; get; } = [];
    }
    public record class DbTableChangeTrackable(string name) : DbObject(name, DbObjectType.Table)
    {
        public List<JqlColumnChangeTrackable> Columns { set; get; } = [];
    }

    public record class DbTableFunction(string name) : DbObject(name, DbObjectType.TableFunction)
    {
    }

    public record class DbScalarFunction(string name) : DbObject(name, DbObjectType.ScalarFunction)
    {
    }

    public record class DbView(string name) : DbObject(name, DbObjectType.View)
    {
        public List<JqlColumn> Columns { set; get; } = [];
    }

    public record class DbProcedure(string name) : DbObject(name, DbObjectType.Procedure)
    {
    }

}
