namespace WS.Shared.Models
{
    public class IndexNowModel
    {
        public string? host { get; set; }
        public string? key { get; set; }
        public HashSet<string> urlList { get; set; } = [];
    }
}