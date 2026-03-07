using System.IO;
using System.Text;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using PackageExplorerViewModel;
using NuGetPe;

namespace Core.Security.Tests;

public sealed class PathTraversalTests
{
    [Fact]
    public void ExportRejectsPackageFilePathsThatEscapeTheSelectedRoot()
    {
        using var root = new TempDirectory();
        var escapedName = "escape-" + System.Guid.NewGuid().ToString("n") + ".txt";
        using var packageRoot = new PackageFolder(string.Empty, viewModel: null);
        using var packageFile = new PackageFile(new TestPackageFile("..\\" + escapedName), "..\\" + escapedName, packageRoot);
        var escapedPath = Path.Combine(root.Path, "..", escapedName);

        var exception = Record.Exception(() => packageFile.Export(root.Path));

        Assert.NotNull(exception);
        Assert.False(File.Exists(escapedPath), "export should not write outside the selected root");
    }

    [Fact]
    public void UnpackPackageRejectsPluginPathsThatEscapeTheTargetRoot()
    {
        using var root = new TempDirectory();
        using var package = new TestPackage(
            new TestPackageFile("lib\\net9.0\\..\\..\\plugin.dll"),
            new TestPackageFile("lib\\net9.0\\plugin.deps.json"));
        var escapedPath = Path.Combine(root.Path, "..", "plugin.dll");

        var exception = Record.Exception(() => package.UnpackPackage("lib\\net9.0", root.Path));

        Assert.NotNull(exception);
        Assert.False(File.Exists(escapedPath), "plugin unpack should not write outside the plugin root");
    }

    [Fact]
    public void TreeConversionRejectsTraversalSegmentsInPackagePaths()
    {
        var files = new List<IPackageFile>
        {
            new TestPackageFile("content\\..\\escape.txt")
        };

        var exception = Record.Exception(() => PackageExplorerViewModel.PathToTreeConverter.Convert(files, viewModel: null));

        Assert.NotNull(exception);
    }

    [Fact]
    public void TreeConversionRejectsEscapedTraversalSegmentsInPackagePaths()
    {
        var files = new List<IPackageFile>
        {
            new TestPackageFile("content\\%2e%2e\\escape.txt")
        };

        var exception = Record.Exception(() => PackageExplorerViewModel.PathToTreeConverter.Convert(files, viewModel: null));

        Assert.NotNull(exception);
    }

    [Fact]
    public void AddFolderRejectsTraversalSegments()
    {
        using var root = new PackageFolder(string.Empty, viewModel: null);

        var addedFolder = root.AddFolder("..");

        Assert.Null(addedFolder);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void RenameRejectsTraversalSegments()
    {
        using var root = new PackageFolder(string.Empty, viewModel: null);
        using var childFolder = root.AddFolder("content")!;

        childFolder.Rename("..");

        Assert.Equal("content", childFolder.Name);
        Assert.Equal("content", childFolder.Path);
    }

    [Fact]
    public void CreateTempFileRejectsTraversalNamesForStringContent()
    {
        var exception = Record.Exception(() => FileHelper.CreateTempFile("..\\escape.txt", "content"));

        Assert.NotNull(exception);
    }

    [Fact]
    public void CreateTempFileRejectsTraversalNamesForStreamContent()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        var exception = Record.Exception(() => FileHelper.CreateTempFile("..\\escape.txt", content));

        Assert.NotNull(exception);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "npe-security-" + System.Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class TestPackage : IPackage
    {
        private readonly IReadOnlyList<IPackageFile> _files;

        public TestPackage(params IPackageFile[] files)
        {
            _files = files;
        }

        public string Id => "Test.Package";

        public NuGetVersion Version => new(1, 0, 0);

        public string Title => string.Empty;

        public IEnumerable<string> Authors => [];

        public IEnumerable<string> Owners => [];

        public string Icon => string.Empty;

        public Uri IconUrl => new("https://example.invalid/icon");

        public string Readme => string.Empty;

        public Uri LicenseUrl => new("https://example.invalid/license");

        public Uri ProjectUrl => new("https://example.invalid/project");

        public bool RequireLicenseAcceptance => false;

        public bool DevelopmentDependency => false;

        public string Description => "test";

        public string Summary => string.Empty;

        public string ReleaseNotes => string.Empty;

        public string Language => "en-US";

        public string Tags => string.Empty;

        public bool Serviceable => false;

        public string Copyright => string.Empty;

        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences => [];

        public IEnumerable<PackageDependencyGroup> DependencyGroups => [];

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences => [];

        public bool IsAbsoluteLatestVersion => false;

        public bool IsLatestVersion => false;

        public bool IsPrerelease => false;

        public DateTimeOffset LastUpdated => DateTimeOffset.UtcNow;

        public DateTimeOffset? Published => DateTimeOffset.UtcNow;

        public Version MinClientVersion => new(1, 0);

        public IEnumerable<ManifestContentFiles> ContentFiles => [];

        public IEnumerable<PackageType> PackageTypes => [];

        public RepositoryMetadata Repository => null!;

        public LicenseMetadata LicenseMetadata => null!;

        public IEnumerable<FrameworkReferenceGroup> FrameworkReferenceGroups => [];

        public Uri? ReportAbuseUrl => null;

        public long DownloadCount => 0;

        public IEnumerable<IPackageFile> GetFiles() => _files;

        public Stream GetStream() => new MemoryStream();

        public void Dispose()
        {
        }
    }

    private sealed class TestPackageFile : IPackageFile
    {
        private readonly byte[] _content;
        private readonly string? _originalPath;

        public TestPackageFile(string path)
        {
            Path = path;
            _content = Encoding.UTF8.GetBytes("test");
            _originalPath = null;
        }

        public string Path { get; }

        public string? OriginalPath => _originalPath;

        public Stream GetStream() => new MemoryStream(_content, writable: false);

        public string EffectivePath => Path;

        public FrameworkName TargetFramework => new(".NETCoreApp,Version=v9.0");

        public NuGetFramework NuGetFramework => NuGetFramework.Parse("net9.0");

        public IEnumerable<NuGetFramework> SupportedFrameworks => [NuGetFramework];

        public DateTimeOffset LastWriteTime => DateTimeOffset.UtcNow;
    }
}
