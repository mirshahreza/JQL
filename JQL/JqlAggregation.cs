namespace JQL
{
    public class JqlAggregation(string name, string phrase)
	{
		public string Name { set; get; } = name;
		public string Phrase { set; get; } = phrase;
	}
}
