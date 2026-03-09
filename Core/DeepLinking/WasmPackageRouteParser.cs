using NuGet.Versioning;

namespace NuGetPackageExplorer.Core.DeepLinking;

public abstract record WasmPackageRoute;

public sealed record WasmPackageLandingRoute : WasmPackageRoute;

public sealed record WasmPackageInvalidRoute : WasmPackageRoute;

public sealed record WasmPackageSearchRoute(string Query) : WasmPackageRoute;

public sealed record WasmPackageIdentityRoute(string Id, NuGetVersion? Version) : WasmPackageRoute;

public static class WasmPackageRouteParser
{
    public static WasmPackageRoute Parse(Uri location, string applicationBasePath = "/")
    {
        ArgumentNullException.ThrowIfNull(location);

        var normalizedBasePath = NormalizeBasePath(applicationBasePath);
        var normalizedPath = NormalizeRequestPath(location.AbsolutePath, normalizedBasePath);
        var segments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (segments.Length == 0)
        {
            return new WasmPackageLandingRoute();
        }

        if (!"packages".Equals(segments[0], StringComparison.OrdinalIgnoreCase))
        {
            return new WasmPackageLandingRoute();
        }

        if (segments.Length >= 4)
        {
            return new WasmPackageInvalidRoute();
        }

        if (segments.Length >= 2)
        {
            if (segments.Length == 2)
            {
                return new WasmPackageIdentityRoute(segments[1], null);
            }

            if (!NuGetVersion.TryParse(segments[2], out var version))
            {
                return new WasmPackageInvalidRoute();
            }

            return new WasmPackageIdentityRoute(segments[1], version);
        }

        return new WasmPackageSearchRoute(GetQueryValue(location.Query, "q") ?? string.Empty);
    }

    private static string NormalizeBasePath(string applicationBasePath)
    {
        if (string.IsNullOrWhiteSpace(applicationBasePath) || applicationBasePath == "/")
        {
            return "/";
        }

        var trimmed = applicationBasePath.Trim();

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        if (!trimmed.EndsWith('/'))
        {
            trimmed += "/";
        }

        return trimmed;
    }

    private static string NormalizeRequestPath(string absolutePath, string normalizedBasePath)
    {
        if (normalizedBasePath == "/")
        {
            return absolutePath;
        }

        if (absolutePath.Equals(normalizedBasePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (absolutePath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return "/" + absolutePath[normalizedBasePath.Length..].TrimStart('/');
        }

        return absolutePath;
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = pair.Split('=', 2);
            if (!key.Equals(DecodeQueryComponent(pieces[0]), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return pieces.Length == 2 ? DecodeQueryComponent(pieces[1]) : string.Empty;
        }

        return null;
    }

    private static string DecodeQueryComponent(string value) => Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
}
