namespace JQL
{
    public class JqlColumnChangeTrackable(string name) : JqlColumn(name)
    {
		public string State { set; get; } = "";
        public string InitialName { set; get; } = "";
    }

}
