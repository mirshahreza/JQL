namespace JQL
{
    public class JqlRefTo(string targetTable, string targetColumn)
	{
		public string TargetTable { set; get; } = targetTable;
		public string TargetColumn { set; get; } = targetColumn;
		public List<JqlQueryColumn> Columns { set; get; } = [];
        public JqlRefTo? RefTo { set; get; }
	}
}
