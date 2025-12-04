namespace Hapvida.ExternalIntegration.Domain.Constants;

public static class Configuration
{
    public const int DEFAULT_SUCCESS_CODE = 200;
    public const int PAGE_SIZE = 20;
    public const int DEFAULT_PAGE = 1;
    public static string ConnectionString { get; set; } = string.Empty;
    public static string FrontEndUrl { get; set; } = string.Empty;
    public static string BackendUrl { get; set; } = string.Empty;
}

