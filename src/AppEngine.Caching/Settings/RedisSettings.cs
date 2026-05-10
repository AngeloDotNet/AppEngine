namespace AppEngine.Caching.Settings;

public class RedisSettings
{
    public string InstanceName { get; set; } = string.Empty;
    public string EndPoints { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public int? KeepAlive { get; set; }
    public bool? SslforAzure { get; set; }
    public bool AbortOnConnectFail { get; set; } = false;
}