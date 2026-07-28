using System.IO.Compression;

namespace OpenTok.Net.iOS.PackageTests;

/// <summary>Locates the packed .nupkg and describes what it is expected to contain.</summary>
public static class Packages
{
    public const string Id = "OpenTok.Net.iOS";

    /// <summary>
    /// The single xcframework the package ships. Unlike the sibling Net.Agora.iOS repository's
    /// several *dynamic* frameworks, OpenTok.xcframework's binary is a static `ar` archive
    /// (confirmed with `file`/`otool`), so it is embedded into the binding assembly rather than
    /// carried as a loose dependency — but the compressed-resources payload check below still
    /// applies, since CompressBindingResourcePackage packs it as a resources.zip either way.
    /// </summary>
    public const string XcFramework = "OpenTok";

    /// <summary>
    /// A floor on the native payload's size. Anything near zero means an empty placeholder was
    /// packed rather than the real SDK — which is the only thing this check is for.
    /// </summary>
    /// <remarks>
    /// The compressed OpenTok.xcframework payload is ~25 MB at SDK 2.34.1 (both iOS slices, ~74 MB
    /// uncompressed). It was ~52 MB at 2.27.1: the SDK genuinely halved, because 2.3x moved the ML
    /// video transformers — and the TensorFlow Lite model that made the framework so large — out
    /// into a separate OpenTokTransformers library that this package does not bind. The floor
    /// therefore has to move *down* on an SDK bump sometimes, and a failure here is a prompt to
    /// check what the framework now contains, not automatically a packaging bug.
    /// </remarks>
    public const long MinPayloadBytes = 15_000_000;

    /// <summary>
    /// Target frameworks the package must carry, one per SDK band pass (see
    /// src/OpenTok.Net.iOS/OpenTok.Net.iOS.csproj and build/BuildNugets.sh). Pinned rather than
    /// discovered: a package that silently lost a target framework because a pack pass failed is
    /// exactly the regression these tests exist to catch.
    /// </summary>
    public static readonly string[] TargetFrameworks =
    [
        "net8.0-ios18.0", "net9.0-ios18.0", "net10.0-ios26.0",
    ];

    public static IEnumerable<object[]> Frameworks =>
        TargetFrameworks.Select(tfm => new object[] { tfm });

    /// <summary>
    /// Identifies the simulator slice of an xcframework from its directory name. Checked by shape
    /// rather than pinned literally: OpenTok ships ios-arm64 and ios-arm64_x86_64-simulator today,
    /// but the exact strings are Vonage's to change.
    /// </summary>
    public static bool IsSimulatorSlice(string slice) =>
        slice.Contains("simulator", StringComparison.Ordinal);

    /// <summary>Whether a slice directory name denotes an iOS slice at all.</summary>
    public static bool IsIosSlice(string slice) =>
        slice.StartsWith("ios-", StringComparison.Ordinal);

    /// <summary>
    /// The binding's native payload entry for a target framework: OpenTok.xcframework, zipped by
    /// the iOS SDK into a single OpenTok.Net.iOS.resources.zip beside the binding assembly.
    /// CompressBindingResourcePackage=true in the csproj means it is always the zip, never a loose
    /// .resources directory — see the comment there for why.
    /// </summary>
    public static string ResourcesEntry(string tfm) => $"lib/{tfm}/{Id}.resources.zip";

    /// <summary>The names of everything in the binding package's native payload for one target framework.</summary>
    public static IReadOnlyList<string> NativePayload(string tfm)
    {
        using var package = OpenPackage();

        var zipped = package.GetEntry(ResourcesEntry(tfm));
        if (zipped is null)
        {
            return [];
        }

        using var archive = new ZipArchive(zipped.Open());
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    /// <summary>
    /// Opens the binding resource package for a target framework. The native payload is a zip
    /// nested inside the .nupkg, so its contents are only reachable through a second archive.
    /// </summary>
    public static ZipArchive OpenNativePayload(ZipArchive package, string tfm)
    {
        var entry = package.GetEntry(ResourcesEntry(tfm));
        Assert.True(entry is not null, $"Package has no entry '{ResourcesEntry(tfm)}'.");

        var buffer = new MemoryStream();
        using (var stream = entry!.Open())
        {
            stream.CopyTo(buffer);
        }

        buffer.Position = 0;
        return new ZipArchive(buffer);
    }

    public static string ArtifactsDirectory { get; } = ResolveArtifactsDirectory();

    public static string FindPackage(string extension = ".nupkg")
    {
        var matches = Directory.Exists(ArtifactsDirectory)
            ? Directory.GetFiles(ArtifactsDirectory, $"{Id}.*{extension}")
                .Where(f => IsVersionOf(Path.GetFileName(f), extension))
                .ToArray()
            : [];

        Assert.True(
            matches.Length > 0,
            $"No {Id}.<version>{extension} found in '{ArtifactsDirectory}'. " +
            "Run build/BuildNugets.sh first.");

        // A rebuilt working copy can leave several versions behind; test the newest.
        return matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    private static bool IsVersionOf(string fileName, string extension)
    {
        var remainder = fileName[(Id.Length + 1)..^extension.Length];
        return remainder.Length > 0 && char.IsDigit(remainder[0]);
    }

    public static ZipArchive OpenPackage(string extension = ".nupkg") =>
        ZipFile.OpenRead(FindPackage(extension));

    private static string ResolveArtifactsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? AppContext.BaseDirectory;

        return Environment.GetEnvironmentVariable("OPENTOK_ARTIFACTS") is { Length: > 0 } configured
            ? Path.GetFullPath(configured, root)
            : Path.Combine(root, "artifacts");
    }
}
