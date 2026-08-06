namespace WS.Shared.Models
{
    public class IndexNowModel
    {
        public string? host { get; set; }
        public string? key { get; set; }
        public ISet<string> urlList { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}