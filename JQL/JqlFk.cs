namespace JQL
{
    public class JqlFk(string fkName, string targetTable, string targetColumn)
	{
		public string FkName { set; get; } = fkName;
		public string TargetTable { set; get; } = targetTable;
		public string TargetColumn { set; get; } = targetColumn;
		public bool EnforceRelation { set; get; } = false;
        public JqlRequestRaw? Lookup { set; get; }
        public string? JsLookupParentId { set; get; }
    }
}
