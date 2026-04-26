namespace AppEngine.Settings;

public class RedisSettings
{
    public string InstanceName { get; set; } = string.Empty;
    public string EndPoints { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int KeepAlive { get; set; } = 180;
    public bool SslforAzure { get; set; } = false;
    public bool AbortOnConnectFail { get; set; } = false;
}