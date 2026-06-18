namespace WS.API.Core.Models
{
    public class LogModel
    {
        public string? Message { get; set; }
        public string? Params { get; set; } //query parameters or other context info
        public string? AppVersion { get; set; }
        public string? Ip { get; set; }
    }
}