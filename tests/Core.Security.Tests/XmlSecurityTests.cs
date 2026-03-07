using System.IO;
using System.Text;

using NuGetPe;

namespace Core.Security.Tests;

public sealed class XmlSecurityTests
{
    [Fact]
    public void ReadManifestRejectsDocumentTypeDeclarations()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            <!DOCTYPE package [
              <!ENTITY injected "owned">
            ]>
            <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
              <metadata>
                <id>Test.Package</id>
                <version>1.0.0</version>
                <authors>test</authors>
                <description>&injected;</description>
              </metadata>
            </package>
            """));

        var exception = Record.Exception(() => ManifestUtility.ReadManifest(stream));

        Assert.NotNull(exception);
    }

    [Fact]
    public void SaveToStreamRejectsDocumentTypeDeclarations()
    {
        using var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("""
            <!DOCTYPE package [
              <!ENTITY injected "owned">
            ]>
            <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
              <metadata>
                <id>Test.Package</id>
                <version>1.0.0</version>
                <authors>test</authors>
                <description>&injected;</description>
              </metadata>
            </package>
            """));
        using var destinationStream = new MemoryStream();

        var exception = Record.Exception(() => ManifestUtility.SaveToStream(sourceStream, destinationStream));

        Assert.NotNull(exception);
    }
}
