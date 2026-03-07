using System.Net;
using System.Reflection;

using PackageExplorerViewModel.Types;

namespace PackageExplorer.Security.Tests;

public sealed class CredentialManagerTests
{
    [Fact]
    public void GetForUriReturnsNullWhenNoCredentialIsStored()
    {
        var manager = CreateCredentialManager();

        var credentials = manager.GetForUri(new Uri("https://example.invalid/feed/index.json"));

        Assert.Null(credentials);
    }

    [Fact]
    public void GetForUriRequiresPathPrefixBoundaryMatch()
    {
        var manager = CreateCredentialManager();
        manager.Add(new NetworkCredential("user", "pass"), new Uri("https://example.invalid/feed"));

        var credentials = manager.GetForUri(new Uri("https://example.invalid/feed-extra/index.json"));

        Assert.Null(credentials);
    }

    [Fact]
    public void GetForUriReturnsStoredCredentialForMatchingFeedPath()
    {
        var manager = CreateCredentialManager();
        var expected = new NetworkCredential("user", "pass");
        manager.Add(expected, new Uri("https://example.invalid/feed"));

        var credentials = manager.GetForUri(new Uri("https://example.invalid/feed/index.json"));

        Assert.Same(expected, credentials);
    }

    private static ICredentialManager CreateCredentialManager()
    {
        var type = typeof(PackageExplorer.App).Assembly.GetType("PackageExplorer.MefServices.CredentialManager", throwOnError: true)!;
        return (ICredentialManager)Activator.CreateInstance(type, nonPublic: true)!;
    }
}
