# The transformers package

`OpenTok.Net.Transformers.iOS` is not a binding. It carries native payload and nothing else.

## Why it exists at all

Vonage ships the media transformers — background blur, background replacement, audio noise
suppression — as a separate library from the Video SDK, on both platforms. On iOS that is the
`VonageClientSDKVideoTransformers` pod, which vendors `OpenTokTransformers.xcframework` from its own
`vonage-ios-sdk-transformers-<version>.zip`, at the same version as the SDK it belongs to.

The classes that use it are already in `OpenTok.Net.iOS`: `OTVideoTransformer`, `OTAudioTransformer`,
and `OTPublisherKit.VideoTransformers` / `.AudioTransformers`. The core SDK `dlopen`s the transformers
framework when one of those is constructed.

Without this package that construction **compiles, links and then fails at runtime**:
`initWithName:properties:` returns null and the SDK raises

```
0x0A000006 — OTC_MEDIA_TRANSFORMER_OPENTOK_TRANSFORMERS_LIBRARY_NOT_LOADED
```

which is the exact failure `OpenTok.Net.iOS` shipped for as long as it bound those classes without
anyone shipping the library. It is not a compile error and not a link error, which is why it went
unnoticed.

## What the package looks like

One `.xcframework`, no Objective-C header of any kind — the framework exposes no class a consumer
would call, because the engine loads it itself. So `ApiDefinition.cs` is deliberately empty and the
packed assembly is a small stub beside the real payload in `<Assembly>.resources.zip`. That is why
`tests/OpenTok.Net.iOS.PackageTests` holds it to different expectations than the binding.

It is still a *binding* project, because that is what packs a `NativeReference` as
`<Assembly>.resources.zip` — and the iOS SDK refuses to build one without an API definition file
("No API definition file specified"), which is the only reason that empty file exists.

**`NoBindingEmbedding` is set here and not on the binding**, and the difference is real.
`OpenTok.xcframework`'s binary is a static `ar` archive, so it links into the binding assembly's own
native companion; `OpenTokTransformers.xcframework`'s is a dynamically linked Mach-O, which cannot be
embedded that way — it has to travel beside the assembly and be copied into the app bundle.

## What it costs

About 68 MB compressed, 122 MB on disk: a 36 MB binary plus three TensorFlow Lite models
(`selfie_segmentation.tflite` for the background transformers, `ns_model_1`/`ns_model_2.tflite` for
noise suppression) and two privacy bundles.

That is the whole reason it is a separate package rather than part of the binding. An app that does
not blur backgrounds should not carry the segmentation model, and before SDK 2.3x it did not have a
choice — the model was inside `OpenTok.framework` itself, which is why the core payload roughly
halved when Vonage moved it out.

## Dependencies, deliberately none

The package does not depend on `OpenTok.Net.iOS`, and the binding does not depend on it. A consumer
adds it alongside whichever it already has. Depending on the binding would be defensible here — the
payload is useless alone — but it would also mean the transformers package's version line had to
track the binding's revision rather than the SDK's, and the two are versioned independently for the
same reason every other package in this family is.

## The Android side

`OpenTok.Net.Transformers.Android`, in the sibling repository, is the same idea over
`com.vonage:client-sdk-video-transformers`, which in turn pulls `mltransformers-ps16k` and
`mltransformersaudionoisesuppression-ps16k`. The `ps16k` suffix is the 16 KB page-size variant,
which Android 15 requires on 64-bit devices.
