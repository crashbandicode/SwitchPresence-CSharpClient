namespace PresenceClient.Avalonia.Models
{
    public class AppSettings
    {
        public string IpAddress { get; set; } = "192.168.0.226";
        public string ClientId { get; set; } = "";
        public bool IgnoreHomeScreen { get; set; }
        public bool IsVerbose { get; set; } = true;
        public bool MinimizeToTray { get; set; }
    }
}