using Hapvida.ExternalIntegration.Domain.Resources;
using System.Globalization;

namespace Hapvida.ExternalIntegration.Application.Helpers;

public static class ResourceHelper
{
    public static string GetResource(string key, string inCaseBlank)
    {
        var resource = HapvidaExternalIntegrationResource.ResourceManager.GetString(
                       key,
                       CultureInfo.CurrentCulture)
                       ?? inCaseBlank;

        return resource;
    }
}

