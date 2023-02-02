namespace JdLoginTool.Wpf.Model.Qinglong
{
    public class QlTokenResult
    {
        public int code { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public string token { get; set; }
        public string token_type { get; set; }
        public int expiration { get; set; }
    }
    public class GetCookiesResult
    {
        public int code { get; set; }
        public Datum[] data { get; set; }
    }

    public class Datum
    {
        public string value { get; set; }
        public int id { get; set; }
        public long created { get; set; }
        public int status { get; set; }
        public string timestamp { get; set; }
        public float position { get; set; }
        public string name { get; set; }
        public string remarks { get; set; }
    }
}