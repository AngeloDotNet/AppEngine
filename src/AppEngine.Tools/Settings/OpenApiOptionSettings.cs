namespace AppEngine.Tools.Settings;

public class OpenApiOptionSettings
{
    public bool RemoveServerList { get; set; } = true;
    public bool AddSimpleAuthentication { get; set; } = false;
    public string SimpleAuthenticationSectionName { get; set; } = "JwtSettings";
    public bool AddKeyCloakAuthentication { get; set; } = false;
    public bool AddAcceptLanguageHeader { get; set; } = false;
    public bool AddDefaultProblemDetailsResponse { get; set; } = true;
    public bool AddOperationParameters { get; set; } = false;
    public bool WriteNumberAsString { get; set; } = false;
    public bool DescribeAllParametersInCamelCase { get; set; } = false;
    public bool AddTimeExamples { get; set; } = false;
}