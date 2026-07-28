// Deliberately empty — see src/OpenTok.Transformers.md.
//
// This package carries native payload and no managed API: OpenTokTransformers.xcframework exposes
// no Objective-C class a consumer would call, because the core SDK dlopens it itself when an
// OTVideoTransformer or OTAudioTransformer is constructed. Those classes live in OpenTok.Net.iOS.
//
// The file exists at all because the .NET for iOS SDK refuses to build a binding project without
// one ("No API definition file specified"), and this has to be a binding project — that is what
// packs a NativeReference as <Assembly>.resources.zip.
