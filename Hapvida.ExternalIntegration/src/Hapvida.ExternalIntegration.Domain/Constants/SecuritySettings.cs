namespace Hapvida.ExternalIntegration.Domain.Constants;

public class SecuritySettings
{
    public JwtSettings JwtSettings { get; set; } = new();
    public RateLimitingSettings RateLimiting { get; set; } = new();
    public HttpsRedirectionSettings HttpsRedirection { get; set; } = new();
    public SecurityHeadersSettings Headers { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public class RateLimitingSettings
{
    public bool EnableRateLimiting { get; set; } = false; // Desabilitado por padrão
    public int PermitLimit { get; set; } = 100;
    public string Window { get; set; } = "00:01:00";
    public int SegmentsPerWindow { get; set; } = 1;
}

public class HttpsRedirectionSettings
{
    public bool EnableHttpsRedirection { get; set; } = true;
    public int HttpsPort { get; set; } = 443;
}

public class SecurityHeadersSettings
{
    public bool EnableSecurityHeaders { get; set; } = true;
    public string XFrameOptions { get; set; } = "DENY";
    public string XContentTypeOptions { get; set; } = "nosniff";
    public string XssProtection { get; set; } = "1; mode=block";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
}

