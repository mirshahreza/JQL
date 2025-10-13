using System.Text.Json;

namespace JQL
{
    public class JqlRequestRaw
    {
        public string Id { set; get; } = "";
        public string Method { set; get; } = "";
        public JsonElement Inputs { set; get; }
    }
}
