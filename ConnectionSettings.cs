namespace AutoRequestStore
{
    public class ConnectionSettings
    {
        public const string Section = "Connection";
        public string Endpoint { get; set; }
        public string Query { get; set; }
        public string Authz { get; set; }
    }
}
