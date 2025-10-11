using System.Text.Json;

namespace JQL
{
    public class ClientRequest
    {
        public string Id { set; get; } = "";
        public string Method { set; get; } = "";
        public JsonElement Inputs { set; get; }
      
    }
}
