using System.IO.Compression;

namespace OpenTok.Net.iOS.PackageTests;

/// <summary>Locates the packed .nupkg files and describes what they are expected to contain.</summary>
public static class Packages
{
    /// <summary>The binding package — a real ApiDefinition.cs and a managed API.</summary>
    public const string Id = "OpenTok.Net.iOS";

    /// <summary>
    /// The transformers package — native payload only, no managed API. See
    /// src/OpenTok.Transformers.md.
    /// </summary>
    public const string TransformersId = "OpenTok.Net.Transformers.iOS";

    /// <summary>Both package ids, matching build/packages.tsv.</summary>
    public static readonly string[] Ids = [Id, TransformersId];

    /// <summary>
    /// The xcframework the binding package ships. Unlike the sibling Net.Agora.iOS repository's
    /// several *dynamic* frameworks, OpenTok.xcframework's binary is a static `ar` archive
    /// (confirmed with `file`/`otool`), so it is embedded into the binding assembly rather than
    /// carried as a loose dependency — but the compressed-resources payload check below still
    /// applies, since CompressBindingResourcePackage packs it as a resources.zip either way.
    /// </summary>
    public const string XcFramework = "OpenTok";

    /// <summary>
    /// The xcframework the transformers package ships. This one <em>is</em> dynamic, which is why
    /// that project sets NoBindingEmbedding and this one does not.
    /// </summary>
    public const string TransformersXcFramework = "OpenTokTransformers";

    /// <summary>
    /// A floor on the binding's native payload size. Anything near zero means an empty placeholder
    /// was packed rather than the real SDK — which is the only thing this check is for.
    /// </summary>
    /// <remarks>
    /// The compressed OpenTok.xcframework payload is ~25 MB at SDK 2.34.1 (both iOS slices, ~74 MB
    /// uncompressed). It was ~52 MB at 2.27.1: the SDK genuinely halved, because 2.3x moved the ML
    /// video transformers — and the TensorFlow Lite models that made the framework so large — out
    /// into a separate OpenTokTransformers library, which this repository now ships as its own
    /// package. The floor therefore has to move *down* on an SDK bump sometimes, and a failure here
    /// is a prompt to check what the framework now contains, not automatically a packaging bug.
    /// </remarks>
    public const long MinPayloadBytes = 15_000_000;

    /// <summary>
    /// A floor on the transformers payload, which is the larger of the two: ~68 MB compressed,
    /// ~122 MB on disk, most of it the 36 MB binary and the three TensorFlow Lite models.
    /// </summary>
    public const long MinTransformersPayloadBytes = 40_000_000;

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
    public static string ResourcesEntry(string tfm, string id = Id) => $"lib/{tfm}/{id}.resources.zip";

    /// <summary>The names of everything in a package's native payload for one target framework.</summary>
    public static IReadOnlyList<string> NativePayload(string tfm, string id = Id)
    {
        using var package = OpenPackage(id);

        var zipped = package.GetEntry(ResourcesEntry(tfm, id));
        if (zipped is null)
        {
            return [];
        }

        using var archive = new ZipArchive(zipped.Open());
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    /// <summary>
    /// Opens a package's binding resource package for a target framework. The native payload is a
    /// zip nested inside the .nupkg, so its contents are only reachable through a second archive.
    /// </summary>
    public static ZipArchive OpenNativePayload(ZipArchive package, string tfm, string id = Id)
    {
        var entry = package.GetEntry(ResourcesEntry(tfm, id));
        Assert.True(entry is not null, $"Package has no entry '{ResourcesEntry(tfm, id)}'.");

        var buffer = new MemoryStream();
        using (var stream = entry!.Open())
        {
            stream.CopyTo(buffer);
        }

        buffer.Position = 0;
        return new ZipArchive(buffer);
    }

    public static string ArtifactsDirectory { get; } = ResolveArtifactsDirectory();

    public static string FindPackage(string id = Id, string extension = ".nupkg")
    {
        var matches = Directory.Exists(ArtifactsDirectory)
            ? Directory.GetFiles(ArtifactsDirectory, $"{id}.*{extension}")
                .Where(f => IsVersionOf(Path.GetFileName(f), id, extension))
                .ToArray()
            : [];

        Assert.True(
            matches.Length > 0,
            $"No {id}.<version>{extension} found in '{ArtifactsDirectory}'. " +
            "Run build/BuildNugets.sh first.");

        // A rebuilt working copy can leave several versions behind; test the newest.
        return matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    /// <summary>
    /// Whether a file name is a version of exactly <paramref name="id"/>.
    /// </summary>
    /// <remarks>
    /// The prefix test alone is not enough now that there are two packages: the glob for
    /// "OpenTok.Net.iOS.*" does not match OpenTok.Net.Transformers.iOS, but the reverse question —
    /// asking for a package whose id is a prefix of another's — is exactly the trap this guards
    /// against, and requiring the next character to be a digit settles it either way.
    /// </remarks>
    private static bool IsVersionOf(string fileName, string id, string extension)
    {
        if (!fileName.StartsWith($"{id}.", StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = fileName[(id.Length + 1)..^extension.Length];
        return remainder.Length > 0 && char.IsDigit(remainder[0]);
    }

    public static ZipArchive OpenPackage(string id = Id, string extension = ".nupkg") =>
        ZipFile.OpenRead(FindPackage(id, extension));

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
