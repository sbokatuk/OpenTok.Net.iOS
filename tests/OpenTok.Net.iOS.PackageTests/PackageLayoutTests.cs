using System.IO.Compression;

namespace OpenTok.Net.iOS.PackageTests;

/// <summary>
/// Asserts the shape of the packed NuGet package — target frameworks present, native payload
/// carried alongside the binding assembly, iOS-only slices inside it. Runs against the packed
/// .nupkg, not the build output, so it catches packaging regressions the compiler cannot see.
/// </summary>
public class PackageLayoutTests
{
    /// <summary>
    /// The heavyweight payload checks decompress tens of megabytes, and the payload is identical
    /// across target frameworks, so they run against one rather than all three. That the others
    /// carry the same content is asserted separately and cheaply by
    /// <see cref="Native_payload_is_the_same_across_target_frameworks"/>.
    /// </summary>
    private const string PayloadTargetFramework = "net8.0-ios18.0";

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Package_carries_a_binding_assembly_for_every_target_framework(string tfm)
    {
        using var package = Packages.OpenPackage();

        var expected = $"lib/{tfm}/{Packages.Id}.dll";
        Assert.True(package.GetEntry(expected) is not null, $"missing '{expected}'.");
    }

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Package_carries_the_native_payload_for_every_target_framework(string tfm)
    {
        using var package = Packages.OpenPackage();

        var entry = package.GetEntry(Packages.ResourcesEntry(tfm));

        // The payload must be a single .resources.zip rather than a .resources directory. The iOS
        // SDK emits the directory form unless CompressBindingResourcePackage is set, which puts
        // every framework file at paths long enough to break restore on Windows, and — observed
        // while building this package — can differ between the net9 and net10 pack passes for the
        // identical framework if left unset. See the comment in the csproj.
        Assert.True(
            entry is not null,
            $"missing '{Packages.ResourcesEntry(tfm)}'. Has CompressBindingResourcePackage been unset?");

        Assert.True(
            entry!.Length > Packages.MinPayloadBytes,
            $"'{entry.FullName}' is only {entry.Length} bytes.");
    }

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Package_ships_opentok_xcframework_beside_the_binding_assembly(string tfm)
    {
        var names = Packages.NativePayload(tfm);

        Assert.True(names.Count > 0, $"carries no native payload for {tfm}.");
        Assert.Contains(names, n => n.Contains($"{Packages.XcFramework}.xcframework/", StringComparison.Ordinal));
    }

    [Fact]
    public void Package_carries_the_resources_bundled_inside_the_framework()
    {
        // OpenTok.framework carries more than a binary and headers, and none of it is declared
        // separately in the podspec — embedding the whole framework directory as a NativeReference
        // resource is what carries it along. This is the check that a future switch to a leaner
        // staging step in fetch-opentok.sh does not quietly drop the extras.
        //
        // PrivacyInfo.xcprivacy is the one with teeth. Apple requires a privacy manifest from every
        // SDK that declares required-reason API use, and an app submitted without the one its
        // dependencies ship is rejected at App Store review — a failure that surfaces to a
        // *consumer*, at the worst possible moment, and never here.
        //
        // (At 2.27.1 this test asserted selfie_segmentation.tflite instead. That model is gone as
        // of 2.3x: the ML video transformers moved to a separate OpenTokTransformers library this
        // package does not bind, which is also why the payload roughly halved — see
        // Packages.MinPayloadBytes.)
        //
        // (Through 2.34.1 the framework also carried a VonageWebRTC_Privacy.bundle next to the
        // manifest. 2.35.1 ships without it — the framework's own _CodeSignature/CodeResources
        // lists no bundle, so it was gone before fetch-opentok.sh ever staged it — and the
        // manifest alone is what Apple actually checks.)
        var names = Packages.NativePayload(PayloadTargetFramework);

        Assert.Contains(names, n => n.EndsWith("PrivacyInfo.xcprivacy", StringComparison.Ordinal));
    }

    [Fact]
    public void Native_payload_is_the_same_across_target_frameworks()
    {
        using var package = Packages.OpenPackage();

        // net8/net9 are packed by the .NET 9 SDK and net10 by the .NET 10 one, and
        // merge-packages.py then grafts the net10 lib/ tree into the other package. Nothing in
        // that flow guarantees the two passes zipped the same framework, so this is where a
        // mismatched graft would be caught.
        //
        // Compared by logical content, not bytes: each pass re-zips the payload and the archives
        // embed their own timestamps, so the same framework legitimately produces different CRCs.
        var manifests = new List<(string Tfm, List<(string Name, long Length)> Entries)>();

        foreach (var tfm in Packages.TargetFrameworks)
        {
            // Opened and released one at a time — three payloads at once would be hundreds of MB.
            using var payload = Packages.OpenNativePayload(package, tfm);
            manifests.Add((tfm, payload.Entries
                .Select(e => (e.FullName, e.Length))
                .OrderBy(e => e.FullName, StringComparer.Ordinal)
                .ToList()));
        }

        var reference = manifests[0];
        foreach (var (tfm, entries) in manifests.Skip(1))
        {
            Assert.True(
                reference.Entries.SequenceEqual(entries),
                $"The native payload for {tfm} differs from {reference.Tfm}.");
        }
    }

    [Fact]
    public void Native_payload_carries_ios_slices_only()
    {
        using var package = Packages.OpenPackage();
        using var payload = Packages.OpenNativePayload(package, PayloadTargetFramework);

        var slices = SlicesOf(payload);

        // Device and simulator, and nothing else. OpenTok ships no macOS or Mac Catalyst slice for
        // this SDK (see OpenTok.xcframework's own Info.plist), so one appearing here would mean the
        // fetch staged something new and the package silently grew. Checked by shape rather than by
        // name so an upstream slice rename does not break the suite.
        Assert.All(slices, slice => Assert.True(
            Packages.IsIosSlice(slice),
            $"OpenTok.xcframework carries a non-iOS slice '{slice}'."));

        Assert.Single(slices, Packages.IsSimulatorSlice);
        Assert.Single(slices, slice => !Packages.IsSimulatorSlice(slice));
    }

    [Fact]
    public void Xcframework_manifest_matches_the_slices_actually_shipped()
    {
        using var package = Packages.OpenPackage();
        using var payload = Packages.OpenNativePayload(package, PayloadTargetFramework);

        var manifest = payload.GetEntry($"{Packages.XcFramework}.xcframework/Info.plist");
        Assert.True(manifest is not null, $"{Packages.XcFramework}.xcframework has no Info.plist.");

        using var reader = new StreamReader(manifest!.Open());
        var text = reader.ReadToEnd();

        // An AvailableLibraries entry whose directory does not exist makes the iOS SDK reject the
        // whole xcframework — a failure that would only surface in a consuming app's build. The
        // reverse (a directory the manifest does not advertise) is invisible dead weight. Both
        // directions are covered: no macOS advertised, and every slice on disk advertised.
        Assert.DoesNotContain("macos", text, StringComparison.OrdinalIgnoreCase);

        foreach (var slice in SlicesOf(payload))
        {
            Assert.Contains(slice, text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Transformers_package_carries_its_framework_for_every_target_framework(string tfm)
    {
        using var package = Packages.OpenPackage(Packages.TransformersId);

        var assembly = $"lib/{tfm}/{Packages.TransformersId}.dll";
        Assert.True(package.GetEntry(assembly) is not null, $"missing '{assembly}'.");

        var entry = package.GetEntry(Packages.ResourcesEntry(tfm, Packages.TransformersId));
        Assert.True(
            entry is not null,
            $"missing '{Packages.ResourcesEntry(tfm, Packages.TransformersId)}'.");

        Assert.True(
            entry!.Length > Packages.MinTransformersPayloadBytes,
            $"'{entry.FullName}' is only {entry.Length} bytes.");
    }

    [Fact]
    public void Transformers_package_carries_the_models_the_transformers_need()
    {
        // The whole point of the package. Each model backs one feature, and a payload missing one
        // of them fails at runtime in the feature's own way rather than as a load error:
        //
        //   selfie_segmentation.tflite  background blur and background replacement
        //   ns_model_1 / ns_model_2     audio noise suppression
        //
        // These were inside OpenTok.framework itself before SDK 2.3x, which is why the core
        // payload roughly halved when Vonage moved them out — see Packages.MinPayloadBytes.
        var names = Packages.NativePayload(PayloadTargetFramework, Packages.TransformersId);

        Assert.Contains(names, n => n.EndsWith("selfie_segmentation.tflite", StringComparison.Ordinal));
        Assert.Contains(names, n => n.EndsWith("ns_model_1.tflite", StringComparison.Ordinal));
        Assert.Contains(names, n => n.EndsWith("ns_model_2.tflite", StringComparison.Ordinal));

        // Through 2.34.1 this also asserted a *_Privacy.bundle. As of 2.35.1 Vonage ships
        // OpenTokTransformers.framework with no privacy manifest at all — neither a bundle nor a
        // PrivacyInfo.xcprivacy — so there is nothing for the package to carry; the core
        // OpenTok.framework's manifest (checked above) is the one Apple sees.
    }

    [Fact]
    public void Transformers_package_has_no_managed_api()
    {
        // The assertion is the absence. This package exists to carry native payload; if it ever
        // grows a real ApiDefinition.cs, that is a design change and not something to discover
        // from a diff. The binding assembly is a stub — a few KB — beside a payload of tens of MB.
        using var package = Packages.OpenPackage(Packages.TransformersId);

        var assembly = package.GetEntry($"lib/{PayloadTargetFramework}/{Packages.TransformersId}.dll");
        Assert.True(assembly is not null, "the transformers package has no binding assembly.");

        Assert.True(
            assembly!.Length < 100_000,
            $"the transformers stub assembly is {assembly.Length} bytes — it appears to carry a real API.");
    }

    [Fact]
    public void Transformers_payload_carries_ios_slices_only()
    {
        using var package = Packages.OpenPackage(Packages.TransformersId);
        using var payload = Packages.OpenNativePayload(package, PayloadTargetFramework, Packages.TransformersId);

        var slices = payload.Entries
            .Where(e => e.FullName.StartsWith($"{Packages.TransformersXcFramework}.xcframework/", StringComparison.Ordinal))
            .Select(e => e.FullName.Split('/'))
            .Where(parts => parts.Length > 2 && parts[2].EndsWith(".framework", StringComparison.Ordinal))
            .Select(parts => parts[1])
            .Distinct()
            .ToList();

        Assert.All(slices, slice => Assert.True(
            Packages.IsIosSlice(slice),
            $"{Packages.TransformersXcFramework}.xcframework carries a non-iOS slice '{slice}'."));

        Assert.Single(slices, Packages.IsSimulatorSlice);
        Assert.Single(slices, slice => !Packages.IsSimulatorSlice(slice));
    }

    /// <summary>
    /// The slice directory names OpenTok.xcframework actually carries inside the payload. The
    /// xcframework root also carries non-slice content alongside the per-platform directories —
    /// LICENSE, README.md, release-notes.md and a doc/ folder of generated HTML — so a slice is
    /// identified by actually containing a .framework bundle (OpenTok.framework), not merely by
    /// being a top-level directory.
    /// </summary>
    private static List<string> SlicesOf(ZipArchive payload) =>
        payload.Entries
            .Where(e => e.FullName.StartsWith($"{Packages.XcFramework}.xcframework/", StringComparison.Ordinal))
            .Select(e => e.FullName.Split('/'))
            .Where(parts => parts.Length > 2 && parts[2].EndsWith(".framework", StringComparison.Ordinal))
            .Select(parts => parts[1])
            .Distinct()
            .OrderBy(slice => slice, StringComparer.Ordinal)
            .ToList();
}
