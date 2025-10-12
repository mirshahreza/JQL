namespace JQL
{
    public class JqlQueryColumn
    {
        public bool? Hidden { set; get; }
		public string? Name { set; get; }
		public string? Phrase { set; get; }
        public string? As { set; get; }
        public JqlRefTo? RefTo { set; get; }
        
    }
}
