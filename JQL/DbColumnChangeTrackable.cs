namespace JQL
{
    public class DbColumnChangeTrackable : JqlColumn
    {
		public DbColumnChangeTrackable(string name) : base(name)
		{
		}

		public string State { set; get; } = "";
        public string InitialName { set; get; } = "";
    }

}
