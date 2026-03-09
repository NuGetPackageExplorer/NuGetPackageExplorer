using NuGet.Versioning;

using NuGetPackageExplorer.Core.DeepLinking;

namespace Core.Security.Tests;

public sealed class WasmPackageRouteParserTests
{
    [Fact]
    public void ParseRecognizesVersionedPackageRoutes()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages/Newtonsoft.Json/13.0.3"));

        var package = Assert.IsType<WasmPackageIdentityRoute>(result);
        Assert.Equal("Newtonsoft.Json", package.Id);
        Assert.Equal(NuGetVersion.Parse("13.0.3"), package.Version);
    }

    [Fact]
    public void ParseRecognizesVersionlessPackageRoutes()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages/Newtonsoft.Json"));

        var package = Assert.IsType<WasmPackageIdentityRoute>(result);
        Assert.Equal("Newtonsoft.Json", package.Id);
        Assert.Null(package.Version);
    }

    [Fact]
    public void ParseRecognizesSearchRoutes()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages?q=uno"));

        var search = Assert.IsType<WasmPackageSearchRoute>(result);
        Assert.Equal("uno", search.Query);
    }

    [Fact]
    public void ParseDecodesPlusDelimitedSearchQueries()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages?q=uno+platform"));

        var search = Assert.IsType<WasmPackageSearchRoute>(result);
        Assert.Equal("uno platform", search.Query);
    }

    [Fact]
    public void ParseRejectsExtraSegmentsAfterVersion()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages/Newtonsoft.Json/13.0.3/invalid"));

        Assert.IsType<WasmPackageInvalidRoute>(result);
    }

    [Fact]
    public void ParseDecodesPackageIds()
    {
        var result = WasmPackageRouteParser.Parse(new Uri("https://nuget.info/packages/Newtonsoft%2EJson/13.0.3"));

        var package = Assert.IsType<WasmPackageIdentityRoute>(result);
        Assert.Equal("Newtonsoft.Json", package.Id);
        Assert.Equal(NuGetVersion.Parse("13.0.3"), package.Version);
    }

    [Fact]
    public void ParseSupportsPrefixedBasePaths()
    {
        var result = WasmPackageRouteParser.Parse(
            new Uri("https://nuget.info/npe/packages/Newtonsoft.Json/13.0.3"),
            applicationBasePath: "/npe/");

        var package = Assert.IsType<WasmPackageIdentityRoute>(result);
        Assert.Equal("Newtonsoft.Json", package.Id);
        Assert.Equal(NuGetVersion.Parse("13.0.3"), package.Version);
    }
}
