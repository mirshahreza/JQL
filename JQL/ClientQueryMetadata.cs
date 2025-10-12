namespace JQL
{
    public class ClientQueryMetadata(string name, string objectTyme)
	{
		public List<JqlColumn> ParentObjectColumns { set; get; } = [];
		public string? ParentObjectName { set; get; }
		public string? ParentObjectType { set; get; }

		public string Name { get; set; } = name;
		public string Type { get; set; } = objectTyme;
		public List<string> QueryColumns { set; get; } = [];

		public List<JqlColumn> FastSearchColumns { set; get; } = [];
		public List<JqlColumn> ExpandableSearchColumns { set; get; } = [];

		public List<string> OptionalQueries { set; get; } = [];
	}
}
