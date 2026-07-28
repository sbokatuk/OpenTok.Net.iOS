using System;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace OpenTok.Net.iOS
{
    // Hand-written against OpenTok.framework's own headers at the SDK version pinned in
    // Directory.Build.props — see README.md for why Objective Sharpie is not run against this
    // xcframework. Each member carries the header declaration it was transcribed from, so a future
    // SDK bump can be diffed against the headers rather than re-derived.
    //
    // Three conventions run through the file, and each exists because getting it wrong is silent:
    //
    //   * [DisableDefaultCtor] on every type whose ObjC init is NS_UNAVAILABLE or whose only real
    //     initialiser takes arguments. Without it the generator emits a parameterless constructor
    //     that compiles, then traps or hands back a half-built object at runtime.
    //   * Delegate callbacks are typed with the *header's* parameter type — OTPublisherKit, not
    //     OTPublisher. They differ: a screen-share publisher is constructed as a bare
    //     OTPublisherKit, and a callback declared to take OTPublisher would fail to marshal it.
    //   * `out OTError` mirrors the SDK's own error convention: null means success. The error's
    //     `Code` is one of the OT*ErrorCode enums in StructsAndEnums.cs — cast to compare.

    // @interface OTSessionICEConfig : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSessionICEConfig
    {
        // @property (assign, nonatomic) enum OTSessionICEIncludeServers includeServers;
        [Export("includeServers", ArgumentSemantic.Assign)]
        OTSessionICEIncludeServers IncludeServers { get; set; }

        // @property (assign, nonatomic) enum OTSessionICETransportPolicy transportPolicy;
        [Export("transportPolicy", ArgumentSemantic.Assign)]
        OTSessionICETransportPolicy TransportPolicy { get; set; }

        // @property (readonly) NSArray * _Nullable customIceServers;
        [NullAllowed, Export("customIceServers")]
        NSObject[] CustomIceServers { get; }

        // @property (assign, nonatomic) BOOL filterOutLanCandidates;
        [Export("filterOutLanCandidates")]
        bool FilterOutLanCandidates { get; set; }

        // +(NSInteger)maxTURNServersLimit;
        [Static]
        [Export("maxTURNServersLimit")]
        nint MaxTURNServersLimit { get; }

        // -(void)addICEServerWithURL:(NSString * _Nonnull)turn_url userName:(NSString * _Nonnull)username credential:(NSString * _Nonnull)credential error:(NSError * _Nonnull * _Nullable)errorPtr;
        [Export("addICEServerWithURL:userName:credential:error:")]
        void AddICEServerWithURL(string turn_url, string username, string credential, out NSError errorPtr);
    }

    // @interface OTSessionSettings : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSessionSettings
    {
        // @property (assign, nonatomic) BOOL connectionEventsSuppressed;
        [Export("connectionEventsSuppressed")]
        bool ConnectionEventsSuppressed { get; set; }

        // @property (retain, nonatomic) OTSessionICEConfig * _Nullable iceConfig;
        [NullAllowed, Export("iceConfig", ArgumentSemantic.Strong)]
        OTSessionICEConfig IceConfig { get; set; }

        // @property (nonatomic, strong) NSURL * _Nullable apiURL DEPRECATED_ATTRIBUTE;
        [Obsolete("Deprecated by the SDK, with no replacement named in the header.")]
        [NullAllowed, Export("apiURL", ArgumentSemantic.Strong)]
        NSUrl ApiURL { get; set; }

        // @property (assign, nonatomic) BOOL ipWhitelist;
        [Export("ipWhitelist")]
        bool IpWhitelist { get; set; }

        // @property (nonatomic, strong) NSString * _Nullable proxyURL;
        [NullAllowed, Export("proxyURL", ArgumentSemantic.Strong)]
        string ProxyURL { get; set; }

        // @property (nonatomic) BOOL singlePeerConnection;
        [Export("singlePeerConnection")]
        bool SinglePeerConnection { get; set; }

        // @property (nonatomic) BOOL sessionMigration;
        [Export("sessionMigration")]
        bool SessionMigration { get; set; }
    }

    // @interface OTSession : NSObject
    // -(instancetype)init NS_UNAVAILABLE;
    [BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate" }, Events = new Type[] { typeof(OTSessionDelegate) })]
    [DisableDefaultCtor]
    interface OTSession
    {
        // @property (readonly) OTSessionConnectionStatus sessionConnectionStatus;
        [Export("sessionConnectionStatus")]
        OTSessionConnectionStatus SessionConnectionStatus { get; }

        // @property (readonly) NSString * _Nonnull sessionId;
        [Export("sessionId")]
        string SessionId { get; }

        // @property (readonly) NSDictionary<NSString *,OTStream *> * _Nonnull streams;
        //
        // Left as a plain NSDictionary rather than NSDictionary<NSString, OTStream>: inside an
        // ApiDefinition, OTStream is still the *definition interface*, which does not implement
        // INativeObject, so the generic form does not satisfy the constraint (CS0311). Keys are
        // stream IDs; index it and cast:
        //     var stream = session.Streams.ObjectForKey(new NSString(id)) as OTStream;
        [Export("streams")]
        NSDictionary Streams { get; }

        // @property (readonly) OTConnection * _Nullable connection;
        [NullAllowed, Export("connection")]
        OTConnection Connection { get; }

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTSessionDelegate Delegate { get; set; }

        // @property (assign, nonatomic) id<OTSessionDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Assign)]
        NSObject WeakDelegate { get; set; }

        // @property (assign, nonatomic) dispatch_queue_t _Nonnull apiQueue;
        [Export("apiQueue", ArgumentSemantic.Assign)]
        DispatchQueue ApiQueue { get; set; }

        // @property (readonly) OTSessionCapabilities * _Nullable capabilities;
        [NullAllowed, Export("capabilities")]
        OTSessionCapabilities Capabilities { get; }

        // -(id _Nullable)initWithApiKey:(NSString * _Nonnull)apiKey sessionId:(NSString * _Nonnull)sessionId delegate:(id<OTSessionDelegate> _Nullable)delegate;
        [Export("initWithApiKey:sessionId:delegate:")]
        NativeHandle Constructor(string apiKey, string sessionId, [NullAllowed] IOTSessionDelegate @delegate);

        // -(id _Nullable)initWithApiKey:(NSString * _Nonnull)apiKey sessionId:(NSString * _Nonnull)sessionId delegate:(id<OTSessionDelegate> _Nullable)delegate settings:(OTSessionSettings * _Nullable)settings;
        [Export("initWithApiKey:sessionId:delegate:settings:")]
        NativeHandle Constructor(string apiKey, string sessionId, [NullAllowed] IOTSessionDelegate @delegate, [NullAllowed] OTSessionSettings settings);

        // -(void)connectWithToken:(NSString * _Nonnull)token error:(OTError * _Nullable * _Nullable)error;
        [Export("connectWithToken:error:")]
        void ConnectWithToken(string token, [NullAllowed] out OTError error);

        // -(void)disconnect:(OTError * _Nullable * _Nullable)error;
        [Export("disconnect:")]
        void Disconnect([NullAllowed] out OTError error);

        // -(void)disconnect __attribute__((deprecated("use disconnect: instead")));
        [Obsolete("Use the Disconnect(out OTError) overload instead.")]
        [Export("disconnect")]
        void Disconnect();

        // -(void)publish:(OTPublisherKit * _Nonnull)publisher error:(OTError * _Nullable * _Nullable)error;
        [Export("publish:error:")]
        void Publish(OTPublisherKit publisher, [NullAllowed] out OTError error);

        // -(void)publish:(OTPublisherKit * _Nonnull)publisher __attribute__((deprecated("use publish:error: instead")));
        [Obsolete("Use the Publish(OTPublisherKit, out OTError) overload instead.")]
        [Export("publish:")]
        void Publish(OTPublisherKit publisher);

        // -(void)unpublish:(OTPublisherKit * _Nonnull)publisher error:(OTError * _Nullable * _Nullable)error;
        [Export("unpublish:error:")]
        void Unpublish(OTPublisherKit publisher, [NullAllowed] out OTError error);

        // -(void)unpublish:(OTPublisherKit * _Nonnull)publisher __attribute__((deprecated("use unpublish:error: instead")));
        [Obsolete("Use the Unpublish(OTPublisherKit, out OTError) overload instead.")]
        [Export("unpublish:")]
        void Unpublish(OTPublisherKit publisher);

        // -(void)subscribe:(OTSubscriberKit * _Nonnull)subscriber error:(OTError * _Nullable * _Nullable)error;
        //
        // OTSubscriberKit, matching the header — an OTSubscriber (the one with a UIView) is one,
        // but a custom-renderer subscriber built as a bare OTSubscriberKit is equally valid here.
        [Export("subscribe:error:")]
        void Subscribe(OTSubscriberKit subscriber, [NullAllowed] out OTError error);

        // -(void)subscribe:(OTSubscriberKit * _Nonnull)subscriber __attribute__((deprecated("use subscribe:error: instead")));
        [Obsolete("Use the Subscribe(OTSubscriberKit, out OTError) overload instead.")]
        [Export("subscribe:")]
        void Subscribe(OTSubscriberKit subscriber);

        // -(void)unsubscribe:(OTSubscriberKit * _Nonnull)subscriber error:(OTError * _Nullable * _Nullable)error;
        [Export("unsubscribe:error:")]
        void Unsubscribe(OTSubscriberKit subscriber, [NullAllowed] out OTError error);

        // -(void)unsubscribe:(OTSubscriberKit * _Nonnull)subscriber __attribute__((deprecated("use unsubscribe:error: instead")));
        [Obsolete("Use the Unsubscribe(OTSubscriberKit, out OTError) overload instead.")]
        [Export("unsubscribe:")]
        void Unsubscribe(OTSubscriberKit subscriber);

        // -(void)signalWithType:(NSString * _Nullable)type string:(NSString * _Nullable)string connection:(OTConnection * _Nullable)connection error:(OTError * _Nullable * _Nullable)error;
        [Export("signalWithType:string:connection:error:")]
        void SignalWithType([NullAllowed] string type, [NullAllowed] string @string, [NullAllowed] OTConnection connection, [NullAllowed] out OTError error);

        // -(void)signalWithType:(NSString * _Nullable)type string:(NSString * _Nullable)string connection:(OTConnection * _Nullable)connection retryAfterReconnect:(BOOL)retryAfterReconnect error:(OTError * _Nullable * _Nullable)error;
        [Export("signalWithType:string:connection:retryAfterReconnect:error:")]
        void SignalWithType([NullAllowed] string type, [NullAllowed] string @string, [NullAllowed] OTConnection connection, bool retryAfterReconnect, [NullAllowed] out OTError error);

        // -(void)forceMuteAll:(NSArray<OTStream *> * _Nullable)excludedStreams error:(OTError * _Nullable * _Nullable)error;
        [Export("forceMuteAll:error:")]
        void ForceMuteAll([NullAllowed] OTStream[] excludedStreams, [NullAllowed] out OTError error);

        // -(void)disableForceMute:(OTError * _Nullable * _Nullable)error;
        [Export("disableForceMute:")]
        void DisableForceMute([NullAllowed] out OTError error);

        // -(void)forceMuteStream:(OTStream * _Nonnull)stream error:(OTError * _Nullable * _Nullable)error;
        [Export("forceMuteStream:error:")]
        void ForceMuteStream(OTStream stream, [NullAllowed] out OTError error);

        // -(void)forceDisconnect:(OTConnection * _Nullable)connection error:(OTError * _Nullable * _Nullable)error;
        //
        // Requires a moderator token — see OTSessionCapabilities.CanForceDisconnect. Answers
        // OTSessionErrorCode.SessionUnableToForceDisconnect (1520) when the role does not permit it.
        [Export("forceDisconnect:error:")]
        void ForceDisconnect([NullAllowed] OTConnection connection, [NullAllowed] out OTError error);

        // -(void)reportIssue:(NSString * _Nullable * _Nullable)issueId;
        [Export("reportIssue:")]
        void ReportIssue([NullAllowed] out string issueId);

        // -(void)setEncryptionSecret:(NSString * _Nonnull)secret error:(OTError * _Nullable * _Nullable)error;
        [Export("setEncryptionSecret:error:")]
        void SetEncryptionSecret(string secret, [NullAllowed] out OTError error);
    }

    interface IOTSessionDelegate
    {
    }

    [Protocol, Model, BaseType(typeof(NSObject))]
    partial interface OTSessionDelegate
    {
        // @required -(void)sessionDidConnect:(OTSession * _Nonnull)session;
        [Export("sessionDidConnect:")]
        void DidConnect(OTSession session);

        // @required -(void)sessionDidDisconnect:(OTSession * _Nonnull)session;
        [Export("sessionDidDisconnect:")]
        void DidDisconnect(OTSession session);

        // @required -(void)session:(OTSession * _Nonnull)session didFailWithError:(OTError * _Nonnull)error;
        [Export("session:didFailWithError:"), EventArgs("OTSessionDelegateError")]
        void DidFailWithError(OTSession session, OTError error);

        // @required -(void)session:(OTSession * _Nonnull)session streamCreated:(OTStream * _Nonnull)stream;
        [Export("session:streamCreated:"), EventArgs("OTSessionDelegateStream")]
        void StreamCreated(OTSession session, OTStream stream);

        // @required -(void)session:(OTSession * _Nonnull)session streamDestroyed:(OTStream * _Nonnull)stream;
        [Export("session:streamDestroyed:"), EventArgs("OTSessionDelegateStream")]
        void StreamDestroyed(OTSession session, OTStream stream);

        // @optional -(void)session:(OTSession * _Nonnull)session connectionCreated:(OTConnection * _Nonnull)connection;
        [Export("session:connectionCreated:"), EventArgs("OTSessionDelegateConnection")]
        void ConnectionCreated(OTSession session, OTConnection connection);

        // @optional -(void)session:(OTSession * _Nonnull)session connectionDestroyed:(OTConnection * _Nonnull)connection;
        [Export("session:connectionDestroyed:"), EventArgs("OTSessionDelegateConnection")]
        void ConnectionDestroyed(OTSession session, OTConnection connection);

        // @optional -(void)session:(OTSession * _Nonnull)session receivedSignalType:(NSString * _Nullable)type fromConnection:(OTConnection * _Nullable)connection withString:(NSString * _Nullable)string;
        [Export("session:receivedSignalType:fromConnection:withString:"), EventArgs("OTSessionDelegateSignal")]
        void ReceivedSignalType(OTSession session, [NullAllowed] string type, [NullAllowed] OTConnection connection, [NullAllowed] string stringData);

        // @optional -(void)session:(OTSession * _Nonnull)session archiveStartedWithId:(NSString * _Nonnull)archiveId name:(NSString * _Nullable)name;
        [Export("session:archiveStartedWithId:name:"), EventArgs("OTSessionDelegateArchiveStarted")]
        void ArchiveStartedWithId(OTSession session, string archiveId, [NullAllowed] string name);

        // @optional -(void)session:(OTSession * _Nonnull)session archiveStoppedWithId:(NSString * _Nonnull)archiveId;
        [Export("session:archiveStoppedWithId:"), EventArgs("OTSessionDelegateArchiveStopped")]
        void ArchiveStoppedWithId(OTSession session, string archiveId);

        // @optional -(void)sessionDidBeginReconnecting:(OTSession * _Nonnull)session;
        [Export("sessionDidBeginReconnecting:"), EventArgs("OTSessionDelegateDidBeginReconnecting")]
        void DidBeginReconnecting(OTSession session);

        // @optional -(void)sessionDidReconnect:(OTSession * _Nonnull)session;
        [Export("sessionDidReconnect:"), EventArgs("OTSessionDelegateDidReconnect")]
        void DidReconnect(OTSession session);

        // @optional -(void)session:(OTSession * _Nonnull)session muteForced:(OTMuteForcedInfo * _Nonnull)muteForcedInfo;
        [Export("session:muteForced:"), EventArgs("OTSessionDelegateMuteForced")]
        void MuteForced(OTSession session, OTMuteForcedInfo muteForcedInfo);
    }

    // @interface OTMuteForcedInfo : NSObject
    [BaseType(typeof(NSObject))]
    interface OTMuteForcedInfo
    {
        // @property (assign, nonatomic) BOOL active;
        //
        // YES when a moderator has muted the session's streams, NO when the mute state was lifted.
        [Export("active")]
        bool Active { get; set; }
    }

    // @interface OTSessionCapabilities : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTSessionCapabilities
    {
        // @property (readonly) BOOL canPublish;
        [Export("canPublish")]
        bool CanPublish { get; }

        // @property (readonly) BOOL canSubscribe;
        [Export("canSubscribe")]
        bool CanSubscribe { get; }

        // @property (readonly) BOOL canForceMute;
        [Export("canForceMute")]
        bool CanForceMute { get; }

        // @property (readonly) BOOL canForceDisconnect;
        [Export("canForceDisconnect")]
        bool CanForceDisconnect { get; }

        // -(instancetype _Nonnull)initWithCanPublish:(BOOL)publish canSubscribe:(BOOL)subscribe;
        [Export("initWithCanPublish:canSubscribe:")]
        NativeHandle Constructor(bool publish, bool subscribe);
    }

    // @interface OTVideoCodecPreference : NSObject
    //
    // -(instancetype)init __attribute__((unavailable("Use +automatic or +manualWithCodecs:")));
    // +(instancetype)new  __attribute__((unavailable("Use +automatic or +manualWithCodecs:")));
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTVideoCodecPreference
    {
        // @property (nonatomic, readonly) BOOL automatic;
        //
        // Named IsAutomatic so it does not collide with the +automatic factory below; both export
        // the same selector, which is fine because one is an instance member and one is a class
        // member.
        [Export("automatic")]
        bool IsAutomatic { get; }

        // @property (nonatomic, readonly, nonnull) NSArray<NSNumber *> *codecs;
        //
        // Boxed OTVideoCodecType values. Only meaningful when IsAutomatic is false.
        [Export("codecs")]
        NSNumber[] Codecs { get; }

        // +(nonnull instancetype)automatic;
        [Static]
        [Export("automatic")]
        OTVideoCodecPreference Automatic { get; }

        // +(nonnull instancetype)manualWithCodecs:(nonnull NSArray<NSNumber *> *)codecs;
        //
        // An empty or unrecognised list makes publisher construction fail, per the header.
        [Static]
        [Export("manualWithCodecs:")]
        OTVideoCodecPreference ManualWithCodecs(NSNumber[] codecs);
    }

    // @interface OTPublisherKitSettings : NSObject
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitSettings
    {
        // @property (copy, nonatomic) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; set; }

        // @property (nonatomic) BOOL audioTrack;
        [Export("audioTrack")]
        bool AudioTrack { get; set; }

        // @property (nonatomic) BOOL videoTrack;
        [Export("videoTrack")]
        bool VideoTrack { get; set; }

        // @property (nonatomic) BOOL senderStatsTrack;
        [Export("senderStatsTrack")]
        bool SenderStatsTrack { get; set; }

        // @property (nonatomic) int audioBitrate;
        [Export("audioBitrate")]
        int AudioBitrate { get; set; }

        // @property (nonatomic) BOOL enableOpusDtx;
        [Export("enableOpusDtx")]
        bool EnableOpusDtx { get; set; }

        // @property (nonatomic) BOOL scalableScreenshare;
        [Export("scalableScreenshare")]
        bool ScalableScreenshare { get; set; }

        // @property (assign, nonatomic) BOOL subscriberAudioFallbackEnabled;
        [Export("subscriberAudioFallbackEnabled")]
        bool SubscriberAudioFallbackEnabled { get; set; }

        // @property (assign, nonatomic) BOOL publisherAudioFallbackEnabled;
        [Export("publisherAudioFallbackEnabled")]
        bool PublisherAudioFallbackEnabled { get; set; }

        // @property (nonatomic) BOOL allowAudioCaptureWhileMuted;
        //
        // Keeps the microphone open while PublishAudio is off, so unmuting does not have to
        // reacquire the audio device.
        [Export("allowAudioCaptureWhileMuted")]
        bool AllowAudioCaptureWhileMuted { get; set; }

        // @property (nonatomic, nullable) OTVideoCodecPreference *videoCodecPreference;
        [NullAllowed, Export("videoCodecPreference", ArgumentSemantic.Assign)]
        OTVideoCodecPreference VideoCodecPreference { get; set; }
    }

    // @interface OTPublisherKit : NSObject
    // -(instancetype)init NS_UNAVAILABLE;
    [BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate", "WeakAudioLevelDelegate", "WeakNetworkStatsDelegate", "WeakRtcStatsReportDelegate" }, Events = new Type[] { typeof(OTPublisherKitDelegate), typeof(OTPublisherKitAudioLevelDelegate), typeof(OTPublisherKitNetworkStatsDelegate), typeof(OTPublisherKitRtcStatsReportDelegate) })]
    [DisableDefaultCtor]
    interface OTPublisherKit
    {
        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate;
        [Export("initWithDelegate:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate settings:(OTPublisherKitSettings * _Nonnull)settings;
        [Export("initWithDelegate:settings:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate, OTPublisherKitSettings settings);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Obsolete("Use the constructor taking OTPublisherKitSettings instead.")]
        [Export("initWithDelegate:name:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate, [NullAllowed] string name);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name audioTrack:(BOOL)audioTrack videoTrack:(BOOL)videoTrack __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Obsolete("Use the constructor taking OTPublisherKitSettings instead.")]
        [Export("initWithDelegate:name:audioTrack:videoTrack:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate, [NullAllowed] string name, bool audioTrack, bool videoTrack);

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTPublisherKitDelegate Delegate { get; set; }

        // @property (nonatomic, weak) id<OTPublisherKitDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Weak)]
        NSObject WeakDelegate { get; set; }

        [Wrap("WeakAudioLevelDelegate")]
        [NullAllowed]
        IOTPublisherKitAudioLevelDelegate AudioLevelDelegate { get; set; }

        // @property (nonatomic, weak) id<OTPublisherKitAudioLevelDelegate> _Nullable audioLevelDelegate;
        [NullAllowed, Export("audioLevelDelegate", ArgumentSemantic.Weak)]
        NSObject WeakAudioLevelDelegate { get; set; }

        [Wrap("WeakNetworkStatsDelegate")]
        [NullAllowed]
        IOTPublisherKitNetworkStatsDelegate NetworkStatsDelegate { get; set; }

        // @property (nonatomic, weak) id<OTPublisherKitNetworkStatsDelegate> _Nullable networkStatsDelegate;
        [NullAllowed, Export("networkStatsDelegate", ArgumentSemantic.Weak)]
        NSObject WeakNetworkStatsDelegate { get; set; }

        [Wrap("WeakRtcStatsReportDelegate")]
        [NullAllowed]
        IOTPublisherKitRtcStatsReportDelegate RtcStatsReportDelegate { get; set; }

        // @property (nonatomic, weak) id<OTPublisherKitRtcStatsReportDelegate> _Nullable rtcStatsReportDelegate;
        [NullAllowed, Export("rtcStatsReportDelegate", ArgumentSemantic.Weak)]
        NSObject WeakRtcStatsReportDelegate { get; set; }

        // @property (readonly) OTSession * _Nullable session;
        [NullAllowed, Export("session")]
        OTSession Session { get; }

        // @property (readonly) OTStream * _Nullable stream;
        [NullAllowed, Export("stream")]
        OTStream Stream { get; }

        // @property (readonly) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; }

        // -(void)getRtcStatsReport;
        [Export("getRtcStatsReport")]
        void GetRtcStatsReport();

        // @property (nonatomic) BOOL publishAudio;
        [Export("publishAudio")]
        bool PublishAudio { get; set; }

        // @property (nonatomic) BOOL publishVideo;
        [Export("publishVideo")]
        bool PublishVideo { get; set; }

        // @property (nonatomic) BOOL publishCaptions;
        [Export("publishCaptions")]
        bool PublishCaptions { get; set; }

        // @property (retain, nonatomic) id<OTVideoCapture> _Nullable videoCapture;
        [NullAllowed, Export("videoCapture", ArgumentSemantic.Strong)]
        IOTVideoCapture VideoCapture { get; set; }

        // @property (assign, nonatomic) OTPublisherKitVideoType videoType;
        [Export("videoType", ArgumentSemantic.Assign)]
        OTPublisherKitVideoType VideoType { get; set; }

        // @property (retain, nonatomic) id<OTVideoRender> _Nullable videoRender;
        [NullAllowed, Export("videoRender", ArgumentSemantic.Strong)]
        IOTVideoRender VideoRender { get; set; }

        // @property (nonatomic) int maxVideoBitrate;
        //
        // Setting this overrides VideoBitratePreset and switches it to
        // OTVideoBitratePreset.Custom — the two are mutually exclusive.
        [Export("maxVideoBitrate")]
        int MaxVideoBitrate { get; set; }

        // @property (nonatomic) OTVideoBitratePreset videoBitratePreset;
        [Export("videoBitratePreset", ArgumentSemantic.Assign)]
        OTVideoBitratePreset VideoBitratePreset { get; set; }

        // @property (nonatomic) OTDegradationPreference degradationPreference;
        [Export("degradationPreference", ArgumentSemantic.Assign)]
        OTDegradationPreference DegradationPreference { get; set; }

        // @property (nonatomic, strong) NSArray * _Nonnull videoTransformers;
        [Export("videoTransformers", ArgumentSemantic.Strong)]
        OTVideoTransformer[] VideoTransformers { get; set; }

        // @property (nonatomic, strong) NSArray * _Nonnull audioTransformers;
        [Export("audioTransformers", ArgumentSemantic.Strong)]
        OTAudioTransformer[] AudioTransformers { get; set; }
    }

    interface IOTPublisherKitDelegate
    {
    }

    // @protocol OTPublisherKitDelegate <NSObject>
    //
    // Every callback is typed OTPublisherKit, as the header declares. A screen-share publisher is
    // routinely a bare OTPublisherKit rather than an OTPublisher, so typing these with the concrete
    // subclass would fail to marshal exactly that case.
    [Protocol(Name = "OTPublisherKitDelegate"), Model]
    [BaseType(typeof(NSObject))]
    partial interface OTPublisherKitDelegate
    {
        // @required -(void)publisher:(OTPublisherKit * _Nonnull)publisher didFailWithError:(OTError * _Nonnull)error;
        [Abstract]
        [Export("publisher:didFailWithError:"), EventArgs("OTPublisherDelegateError")]
        void DidFailWithError(OTPublisherKit publisher, OTError error);

        // @optional -(void)muteForced:(OTPublisherKit * _Nonnull)publisher;
        [Export("muteForced:")]
        void MuteForced(OTPublisherKit publisher);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher streamCreated:(OTStream * _Nonnull)stream;
        [Export("publisher:streamCreated:"), EventArgs("OTPublisherDelegateStream")]
        void StreamCreated(OTPublisherKit publisher, OTStream stream);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher streamDestroyed:(OTStream * _Nonnull)stream;
        [Export("publisher:streamDestroyed:"), EventArgs("OTPublisherDelegateStream")]
        void StreamDestroyed(OTPublisherKit publisher, OTStream stream);

        // @optional -(void)publisherVideoDisabled:(OTPublisherKit * _Nonnull)publisher reason:(OTPublisherVideoEventReason)reason;
        [Export("publisherVideoDisabled:reason:"), EventArgs("OTPublisherDelegateEventReason")]
        void PublisherVideoDisabled(OTPublisherKit publisher, OTPublisherVideoEventReason reason);

        // @optional -(void)publisherVideoEnabled:(OTPublisherKit * _Nonnull)publisher reason:(OTPublisherVideoEventReason)reason;
        [Export("publisherVideoEnabled:reason:"), EventArgs("OTPublisherDelegateEventReason")]
        void PublisherVideoEnabled(OTPublisherKit publisher, OTPublisherVideoEventReason reason);

        // @optional -(void)publisherVideoDisableWarning:(OTPublisherKit * _Nonnull)publisher;
        [Export("publisherVideoDisableWarning:")]
        void PublisherVideoDisableWarning(OTPublisherKit publisher);

        // @optional -(void)publisherVideoDisableWarningLifted:(OTPublisherKit * _Nonnull)publisher;
        [Export("publisherVideoDisableWarningLifted:")]
        void PublisherVideoDisableWarningLifted(OTPublisherKit publisher);
    }

    interface IOTPublisherDelegate
    {
    }

    // @protocol OTPublisherDelegate <OTPublisherKitDelegate>
    //
    // The one callback that genuinely is OTPublisher-specific: only the camera-backed publisher has
    // a camera to switch. Declared in OTPublisher.h as its own protocol refining
    // OTPublisherKitDelegate, and bound that way rather than folded into the base — assigning an
    // OTPublisherKitDelegate to an OTPublisher stays legal, which is what the header allows.
    [Protocol(Name = "OTPublisherDelegate"), Model]
    [BaseType(typeof(OTPublisherKitDelegate))]
    partial interface OTPublisherDelegate
    {
        // @optional -(void)publisher:(OTPublisher * _Nonnull)publisher didChangeCameraPosition:(AVCaptureDevicePosition)position;
        [Export("publisher:didChangeCameraPosition:"), EventArgs("OTPublisherDelegateEventCameraPosition")]
        void DidChangeCameraPosition(OTPublisher publisher, AVCaptureDevicePosition position);
    }

    interface IOTPublisherKitAudioLevelDelegate
    {
    }

    // @protocol OTPublisherKitAudioLevelDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    partial interface OTPublisherKitAudioLevelDelegate
    {
        // @required -(void)publisher:(OTPublisherKit * _Nonnull)publisher audioLevelUpdated:(float)audioLevel;
        [Abstract]
        [Export("publisher:audioLevelUpdated:"), EventArgs("OTPublisherKitAudioLevelDelegateAudioLevelUpdated")]
        void AudioLevelUpdated(OTPublisherKit publisher, float audioLevel);
    }

    interface IOTPublisherKitNetworkStatsDelegate
    {
    }

    // @protocol OTPublisherKitNetworkStatsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitNetworkStatsDelegate
    {
        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher videoNetworkStatsUpdated:(NSArray<OTPublisherKitVideoNetworkStats *> * _Nonnull)stats;
        [Export("publisher:videoNetworkStatsUpdated:"), EventArgs("OTPublisherKitNetworkStatsDelegateVideoNetworkStatsUpdated")]
        void VideoNetworkStatsUpdated(OTPublisherKit publisher, OTPublisherKitVideoNetworkStats[] stats);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher audioNetworkStatsUpdated:(NSArray<OTPublisherKitAudioNetworkStats *> * _Nonnull)stats;
        [Export("publisher:audioNetworkStatsUpdated:"), EventArgs("OTPublisherKitNetworkStatsDelegateAudioNetworkStatsUpdated")]
        void AudioNetworkStatsUpdated(OTPublisherKit publisher, OTPublisherKitAudioNetworkStats[] stats);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher videoQualityChanged:(OTPublisherKitVideoNetworkStats * _Nonnull)stats reason:(OTPublisherVideoEventReason)reason;
        //
        // Quality moved while the video track stayed on — distinct from
        // publisherVideoDisabled:/publisherVideoEnabled:, which report the track itself turning off
        // and on.
        [Export("publisher:videoQualityChanged:reason:"), EventArgs("OTPublisherKitNetworkStatsDelegateVideoQualityChanged")]
        void VideoQualityChanged(OTPublisherKit publisher, OTPublisherKitVideoNetworkStats stats, OTPublisherVideoEventReason reason);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher networkConditionChanged:(OTPublisherKitMediaLinkStats * _Nonnull)mediaLinkStats reason:(OTNetworkReason)reason;
        [Export("publisher:networkConditionChanged:reason:"), EventArgs("OTPublisherKitNetworkStatsDelegateNetworkConditionChanged")]
        void NetworkConditionChanged(OTPublisherKit publisher, OTPublisherKitMediaLinkStats mediaLinkStats, OTNetworkReason reason);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher mediaLinkStatsUpdated:(NSArray<OTPublisherKitMediaLinkStats *> * _Nonnull)mediaLinkStats;
        [Export("publisher:mediaLinkStatsUpdated:"), EventArgs("OTPublisherKitNetworkStatsDelegateMediaLinkStatsUpdated")]
        void MediaLinkStatsUpdated(OTPublisherKit publisher, OTPublisherKitMediaLinkStats[] mediaLinkStats);
    }

    // @interface OTPublisherRtcStats : NSObject
    [BaseType(typeof(NSObject))]
    interface OTPublisherRtcStats
    {
        // @property (strong) NSString * _Nonnull connectionId;
        [Export("connectionId", ArgumentSemantic.Strong)]
        string ConnectionId { get; set; }

        // @property (strong) NSString * _Nonnull jsonArrayOfReports;
        //
        // A JSON array shaped like the browser RTCStatsReport object; the SDK does not parse it.
        [Export("jsonArrayOfReports", ArgumentSemantic.Strong)]
        string JsonArrayOfReports { get; set; }
    }

    interface IOTPublisherKitRtcStatsReportDelegate
    {
    }

    // @protocol OTPublisherKitRtcStatsReportDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitRtcStatsReportDelegate
    {
        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher rtcStatsReport:(NSArray<OTPublisherRtcStats *> * _Nonnull)stats;
        [Export("publisher:rtcStatsReport:"), EventArgs("OTPublisherKitRtcStatsReportDelegateRtcStats")]
        void RtcStatsReport(OTPublisherKit publisher, OTPublisherRtcStats[] stats);
    }

    interface IOTCustomVideoTransformer
    {
    }

    // @protocol OTCustomVideoTransformer <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTCustomVideoTransformer
    {
        // @required -(void)transform:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("transform:")]
        void Transform(OTVideoFrame frame);
    }

    interface IOTCustomAudioTransformer
    {
    }

    // @protocol OTCustomAudioTransformer <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTCustomAudioTransformer
    {
        // @required -(void)transform:(OTAudioData * _Nonnull)audioData;
        [Abstract]
        [Export("transform:")]
        void Transform(OTAudioData audioData);
    }

    // @interface OTVideoTransformer : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTVideoTransformer
    {
        // -(id _Nullable)initWithName:(NSString * _Nonnull)name properties:(NSString * _Nonnull)properties;
        //
        // Names one of Vonage's own built-in transformers (background blur and the like); the
        // properties string is that transformer's JSON configuration.
        [Export("initWithName:properties:")]
        NativeHandle Constructor(string name, string properties);

        // -(id _Nullable)initWithName:(NSString * _Nonnull)name transformer:(id<OTCustomVideoTransformer> _Nonnull)transformer;
        [Export("initWithName:transformer:")]
        NativeHandle Constructor(string name, IOTCustomVideoTransformer transformer);
    }

    // @interface OTAudioTransformer : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTAudioTransformer
    {
        // -(id _Nullable)initWithName:(NSString * _Nonnull)name properties:(NSString * _Nonnull)properties;
        [Export("initWithName:properties:")]
        NativeHandle Constructor(string name, string properties);

        // -(id _Nullable)initWithName:(NSString * _Nonnull)name transformer:(id<OTCustomAudioTransformer> _Nonnull)transformer;
        [Export("initWithName:transformer:")]
        NativeHandle Constructor(string name, IOTCustomAudioTransformer transformer);
    }

    // @interface OTSubscriberKit : NSObject
    // -(instancetype)init NS_UNAVAILABLE;
    //
    // Bound as its own type rather than folded into OTSubscriber. The two are distinct in the
    // header — OTSubscriberKit has no view at all, and a subscriber driving a custom
    // OTVideoRender is constructed as one — and OTSession.Subscribe takes the kit.
    [BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate", "WeakAudioLevelDelegate", "WeakCaptionsDelegate", "WeakNetworkStatsDelegate", "WeakRtcStatsReportDelegate" }, Events = new Type[] { typeof(OTSubscriberKitDelegate), typeof(OTSubscriberKitAudioLevelDelegate), typeof(OTSubscriberKitCaptionsDelegate), typeof(OTSubscriberKitNetworkStatsDelegate), typeof(OTSubscriberKitRtcStatsReportDelegate) })]
    [DisableDefaultCtor]
    interface OTSubscriberKit
    {
        // -(id _Nullable)initWithStream:(OTStream * _Nonnull)stream delegate:(id<OTSubscriberKitDelegate> _Nullable)delegate;
        [Export("initWithStream:delegate:")]
        NativeHandle Constructor(OTStream stream, [NullAllowed] IOTSubscriberKitDelegate @delegate);

        // @property (readonly) OTSession * _Nonnull session;
        [Export("session")]
        OTSession Session { get; }

        // @property (readonly) OTStream * _Nullable stream;
        [NullAllowed, Export("stream")]
        OTStream Stream { get; }

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTSubscriberKitDelegate Delegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Weak)]
        NSObject WeakDelegate { get; set; }

        [Wrap("WeakAudioLevelDelegate")]
        [NullAllowed]
        IOTSubscriberKitAudioLevelDelegate AudioLevelDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitAudioLevelDelegate> _Nullable audioLevelDelegate;
        [NullAllowed, Export("audioLevelDelegate", ArgumentSemantic.Weak)]
        NSObject WeakAudioLevelDelegate { get; set; }

        [Wrap("WeakCaptionsDelegate")]
        [NullAllowed]
        IOTSubscriberKitCaptionsDelegate CaptionsDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitCaptionsDelegate> _Nullable captionsDelegate;
        [NullAllowed, Export("captionsDelegate", ArgumentSemantic.Weak)]
        NSObject WeakCaptionsDelegate { get; set; }

        [Wrap("WeakNetworkStatsDelegate")]
        [NullAllowed]
        IOTSubscriberKitNetworkStatsDelegate NetworkStatsDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitNetworkStatsDelegate> _Nullable networkStatsDelegate;
        [NullAllowed, Export("networkStatsDelegate", ArgumentSemantic.Weak)]
        NSObject WeakNetworkStatsDelegate { get; set; }

        [Wrap("WeakRtcStatsReportDelegate")]
        [NullAllowed]
        IOTSubscriberKitRtcStatsReportDelegate RtcStatsReportDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitRtcStatsReportDelegate> _Nullable rtcStatsReportDelegate;
        [NullAllowed, Export("rtcStatsReportDelegate", ArgumentSemantic.Weak)]
        NSObject WeakRtcStatsReportDelegate { get; set; }

        // @property (nonatomic) BOOL subscribeToAudio;
        [Export("subscribeToAudio")]
        bool SubscribeToAudio { get; set; }

        // @property (nonatomic) BOOL subscribeToVideo;
        [Export("subscribeToVideo")]
        bool SubscribeToVideo { get; set; }

        // @property (nonatomic) BOOL subscribeToCaptions;
        [Export("subscribeToCaptions")]
        bool SubscribeToCaptions { get; set; }

        // @property (nonatomic, copy, nullable) NSString *captionsTranslationLanguage;
        //
        // A BCP-47 code, or null for no translation. An unsupported code is ignored rather than
        // rejected, so the header says to read the property back to see whether it took.
        [NullAllowed, Export("captionsTranslationLanguage")]
        string CaptionsTranslationLanguage { get; set; }

        // @property (retain, nonatomic) id<OTVideoRender> _Nullable videoRender;
        [NullAllowed, Export("videoRender", ArgumentSemantic.Strong)]
        IOTVideoRender VideoRender { get; set; }

        // @property (nonatomic) CGSize preferredResolution;
        [Export("preferredResolution", ArgumentSemantic.Assign)]
        CGSize PreferredResolution { get; set; }

        // @property (nonatomic) float preferredFrameRate;
        [Export("preferredFrameRate")]
        float PreferredFrameRate { get; set; }

        // @property (nonatomic) double audioVolume;
        [Export("audioVolume")]
        double AudioVolume { get; set; }

        // -(void)getRtcStatsReport:(OTError * _Nullable * _Nullable)error;
        [Export("getRtcStatsReport:")]
        void GetRtcStatsReport([NullAllowed] out OTError error);

        // -(void)getRtcStatsReport __attribute__((deprecated("use getRtcStatsReport error: instead")));
        [Obsolete("Use the GetRtcStatsReport(out OTError) overload instead.")]
        [Export("getRtcStatsReport")]
        void GetRtcStatsReport();
    }

    // @interface OTSubscriber : OTSubscriberKit
    [BaseType(typeof(OTSubscriberKit))]
    [DisableDefaultCtor]
    interface OTSubscriber
    {
        // Inherited from OTSubscriberKit; redeclared because a subclass does not inherit its base's
        // bound constructors.
        [Export("initWithStream:delegate:")]
        NativeHandle Constructor(OTStream stream, [NullAllowed] IOTSubscriberKitDelegate @delegate);

        // @property (readonly) UIView * _Nullable view;
        //
        // Read-only and SDK-created: unlike Agora, the app attaches this view into its own layout
        // rather than handing a view in. Only valid once subscriberDidConnectToStream: has fired.
        [NullAllowed, Export("view")]
        UIView View { get; }

        // @property (nonatomic) OTVideoViewScaleBehavior viewScaleBehavior;
        [Export("viewScaleBehavior", ArgumentSemantic.Assign)]
        OTVideoViewScaleBehavior ViewScaleBehavior { get; set; }
    }

    interface IOTSubscriberKitDelegate
    {
    }

    // @protocol OTSubscriberKitDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    partial interface OTSubscriberKitDelegate
    {
        // @required -(void)subscriberDidConnectToStream:(OTSubscriberKit * _Nonnull)subscriber;
        [Abstract]
        [Export("subscriberDidConnectToStream:")]
        void DidConnectToStream(OTSubscriberKit subscriber);

        // @required -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber didFailWithError:(OTError * _Nonnull)error;
        [Abstract]
        [Export("subscriber:didFailWithError:"), EventArgs("OTSubscriberKitDelegateError")]
        void DidFailWithError(OTSubscriberKit subscriber, OTError error);

        // @optional -(void)subscriberVideoDisabled:(OTSubscriberKit * _Nonnull)subscriber reason:(OTSubscriberVideoEventReason)reason;
        [Export("subscriberVideoDisabled:reason:"), EventArgs("OTSubscriberKitDelegateVideoEventReason")]
        void VideoDisabled(OTSubscriberKit subscriber, OTSubscriberVideoEventReason reason);

        // @optional -(void)subscriberVideoEnabled:(OTSubscriberKit * _Nonnull)subscriber reason:(OTSubscriberVideoEventReason)reason;
        [Export("subscriberVideoEnabled:reason:"), EventArgs("OTSubscriberKitDelegateVideoEventReason")]
        void VideoEnabled(OTSubscriberKit subscriber, OTSubscriberVideoEventReason reason);

        // @optional -(void)subscriberVideoDisableWarning:(OTSubscriberKit * _Nonnull)subscriber;
        [Export("subscriberVideoDisableWarning:")]
        void VideoDisableWarning(OTSubscriberKit subscriber);

        // @optional -(void)subscriberVideoDisableWarningLifted:(OTSubscriberKit * _Nonnull)subscriber;
        [Export("subscriberVideoDisableWarningLifted:")]
        void VideoDisableWarningLifted(OTSubscriberKit subscriber);

        // @optional -(void)subscriberDidDisconnectFromStream:(OTSubscriberKit * _Nonnull)subscriber;
        [Export("subscriberDidDisconnectFromStream:")]
        void DidDisconnectFromStream(OTSubscriberKit subscriber);

        // @optional -(void)subscriberDidReconnectToStream:(OTSubscriberKit * _Nonnull)subscriber;
        [Export("subscriberDidReconnectToStream:")]
        void DidReconnectToStream(OTSubscriberKit subscriber);
    }

    interface IOTSubscriberDelegate
    {
    }

    // @protocol OTSubscriberDelegate <OTSubscriberKitDelegate>
    //
    // Declared in OTSubscriber.h: the first-decoded-frame callback only exists for the view-backed
    // subscriber, since a custom-renderer one receives frames through OTVideoRender instead.
    [Protocol(Name = "OTSubscriberDelegate"), Model]
    [BaseType(typeof(OTSubscriberKitDelegate))]
    partial interface OTSubscriberDelegate
    {
        // @optional -(void)subscriberVideoDataReceived:(OTSubscriber * _Nonnull)subscriber;
        [Export("subscriberVideoDataReceived:")]
        void VideoDataReceived(OTSubscriber subscriber);
    }

    interface IOTSubscriberKitAudioLevelDelegate
    {
    }

    // @protocol OTSubscriberKitAudioLevelDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitAudioLevelDelegate
    {
        // @required -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber audioLevelUpdated:(float)audioLevel;
        [Abstract]
        [Export("subscriber:audioLevelUpdated:"), EventArgs("OTSubscriberKitAudioLevelDelegateAudioLevelUpdated")]
        void AudioLevelUpdated(OTSubscriberKit subscriber, float audioLevel);
    }

    interface IOTSubscriberKitCaptionsDelegate
    {
    }

    // @protocol OTSubscriberKitCaptionsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitCaptionsDelegate
    {
        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber caption:(NSString * _Nonnull)text isFinal:(BOOL)isFinal;
        [Export("subscriber:caption:isFinal:"), EventArgs("OTSubscriberKitCaptionsDelegateCaption")]
        void Caption(OTSubscriberKit subscriber, string text, bool isFinal);
    }

    interface IOTSubscriberKitNetworkStatsDelegate
    {
    }

    // @protocol OTSubscriberKitNetworkStatsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitNetworkStatsDelegate
    {
        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber videoNetworkStatsUpdated:(OTSubscriberKitVideoNetworkStats * _Nonnull)stats;
        [Export("subscriber:videoNetworkStatsUpdated:"), EventArgs("OTSubscriberKitNetworkStatsDelegateVideoNetworkStatsUpdated")]
        void VideoNetworkStatsUpdated(OTSubscriberKit subscriber, OTSubscriberKitVideoNetworkStats stats);

        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber audioNetworkStatsUpdated:(OTSubscriberKitAudioNetworkStats * _Nonnull)stats;
        [Export("subscriber:audioNetworkStatsUpdated:"), EventArgs("OTSubscriberKitNetworkStatsDelegateAudioNetworkStatsUpdated")]
        void AudioNetworkStatsUpdated(OTSubscriberKit subscriber, OTSubscriberKitAudioNetworkStats stats);

        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber videoQualityChanged:(OTSubscriberKitVideoNetworkStats * _Nonnull)stats reason:(OTSubscriberVideoEventReason)reason;
        [Export("subscriber:videoQualityChanged:reason:"), EventArgs("OTSubscriberKitNetworkStatsDelegateVideoQualityChanged")]
        void VideoQualityChanged(OTSubscriberKit subscriber, OTSubscriberKitVideoNetworkStats stats, OTSubscriberVideoEventReason reason);

        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber networkConditionChanged:(OTSubscriberKitMediaLinkStats * _Nonnull)mediaLinkStats reason:(OTNetworkReason)reason;
        [Export("subscriber:networkConditionChanged:reason:"), EventArgs("OTSubscriberKitNetworkStatsDelegateNetworkConditionChanged")]
        void NetworkConditionChanged(OTSubscriberKit subscriber, OTSubscriberKitMediaLinkStats mediaLinkStats, OTNetworkReason reason);

        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber mediaLinkStatsUpdated:(OTSubscriberKitMediaLinkStats * _Nonnull)mediaLinkStats;
        [Export("subscriber:mediaLinkStatsUpdated:"), EventArgs("OTSubscriberKitNetworkStatsDelegateMediaLinkStatsUpdated")]
        void MediaLinkStatsUpdated(OTSubscriberKit subscriber, OTSubscriberKitMediaLinkStats mediaLinkStats);
    }

    interface IOTSubscriberKitRtcStatsReportDelegate
    {
    }

    // @protocol OTSubscriberKitRtcStatsReportDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitRtcStatsReportDelegate
    {
        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber rtcStatsReport:(NSString * _Nonnull)jsonArrayOfReports;
        [Export("subscriber:rtcStatsReport:"), EventArgs("OTSubscriberKitRtcStatsReportDelegateJson")]
        void RtcStatsReport(OTSubscriberKit subscriber, string jsonArrayOfReports);
    }

    // @interface OTStream : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTStream
    {
        // @property (readonly) OTConnection * _Nonnull connection;
        [Export("connection")]
        OTConnection Connection { get; }

        // @property (readonly) OTSession * _Nonnull session;
        [Export("session")]
        OTSession Session { get; }

        // @property (readonly) NSString * _Nonnull streamId;
        [Export("streamId")]
        string StreamId { get; }

        // @property (readonly) NSDate * _Nonnull creationTime;
        [Export("creationTime")]
        NSDate CreationTime { get; }

        // @property (readonly) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; }

        // @property (readonly) BOOL hasAudio;
        [Export("hasAudio")]
        bool HasAudio { get; }

        // @property (readonly) BOOL hasVideo;
        [Export("hasVideo")]
        bool HasVideo { get; }

        // @property (readonly) BOOL hasCaptions;
        [Export("hasCaptions")]
        bool HasCaptions { get; }

        // @property (readonly) CGSize videoDimensions;
        [Export("videoDimensions")]
        CGSize VideoDimensions { get; }

        // @property (readonly) OTStreamVideoType videoType;
        [Export("videoType")]
        OTStreamVideoType VideoType { get; }
    }

    // @interface OTConnection : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTConnection
    {
        // @property (readonly) NSString * _Nonnull connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (readonly) NSDate * _Nonnull creationTime;
        [Export("creationTime")]
        NSDate CreationTime { get; }

        // @property (readonly) NSString * _Nullable data;
        //
        // Whatever connection metadata the token was minted with.
        [NullAllowed, Export("data")]
        string Data { get; }
    }

    // @interface OTError : NSError
    //
    // No members of its own — the SDK subclasses NSError purely as a marker. Code and
    // LocalizedDescription come from NSError; Code carries one of the OT*ErrorCode values in
    // StructsAndEnums.cs, which enum depending on which delegate reported it.
    [BaseType(typeof(NSError))]
    [DisableDefaultCtor]
    interface OTError
    {
    }

    // @interface OTVideoFormat : NSObject
    [BaseType(typeof(NSObject))]
    interface OTVideoFormat
    {
        // @property (copy, nonatomic) NSString * _Nonnull name;
        [Export("name")]
        string Name { get; set; }

        // @property (assign, nonatomic) OTPixelFormat pixelFormat;
        [Export("pixelFormat", ArgumentSemantic.Assign)]
        OTPixelFormat PixelFormat { get; set; }

        // @property (retain, nonatomic) NSMutableArray * _Nonnull bytesPerRow;
        [Export("bytesPerRow", ArgumentSemantic.Retain)]
        NSMutableArray BytesPerRow { get; set; }

        // @property (assign, nonatomic) uint32_t imageWidth;
        [Export("imageWidth")]
        uint ImageWidth { get; set; }

        // @property (assign, nonatomic) uint32_t imageHeight;
        [Export("imageHeight")]
        uint ImageHeight { get; set; }

        // @property (assign, nonatomic) double estimatedFramesPerSecond;
        [Export("estimatedFramesPerSecond")]
        double EstimatedFramesPerSecond { get; set; }

        // @property (assign, nonatomic) double estimatedCaptureDelay;
        [Export("estimatedCaptureDelay")]
        double EstimatedCaptureDelay { get; set; }

        // +(OTVideoFormat * _Nonnull)videoFormatI420WithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatI420WithWidth:height:")]
        OTVideoFormat VideoFormatI420WithWidth(uint width, uint height);

        // +(OTVideoFormat * _Nonnull)videoFormatNV12WithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatNV12WithWidth:height:")]
        OTVideoFormat VideoFormatNV12WithWidth(uint width, uint height);

        // +(OTVideoFormat * _Nonnull)videoFormatARGBWithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatARGBWithWidth:height:")]
        OTVideoFormat VideoFormatARGBWithWidth(uint width, uint height);
    }

    // @interface OTVideoFrame : NSObject
    [BaseType(typeof(NSObject))]
    interface OTVideoFrame
    {
        // @property (assign, nonatomic) CMTime timestamp;
        [Export("timestamp", ArgumentSemantic.Assign)]
        CMTime Timestamp { get; set; }

        // @property (assign, nonatomic) OTVideoOrientation orientation;
        [Export("orientation", ArgumentSemantic.Assign)]
        OTVideoOrientation Orientation { get; set; }

        // @property (retain, nonatomic) OTVideoFormat * _Nullable format;
        [NullAllowed, Export("format", ArgumentSemantic.Strong)]
        OTVideoFormat Format { get; set; }

        // @property (readonly, nonatomic) NSData * _Nullable metadata;
        [NullAllowed, Export("metadata")]
        NSData Metadata { get; }

        // -(id _Nonnull)initWithFormat:(OTVideoFormat * _Nonnull)videoFormat;
        [Export("initWithFormat:")]
        NativeHandle Constructor(OTVideoFormat videoFormat);

        // -(void)setPlanesWithPointers:(uint8_t * _Nonnull *)planes numPlanes:(int)numPlanes;
        //
        // An array of raw plane pointers. Left as IntPtr: the SDK takes the buffers as-is, and the
        // caller owns them for the frame's lifetime.
        [Export("setPlanesWithPointers:numPlanes:")]
        void SetPlanesWithPointers(IntPtr planes, int numPlanes);

        // -(void)clearPlanes;
        [Export("clearPlanes")]
        void ClearPlanes();

        // -(int)getPlaneStride:(int)plane;
        [Export("getPlaneStride:")]
        int GetPlaneStride(int plane);

        // -(void)setMetadata:(NSData * _Nonnull)data error:(OTError * _Nullable *)error;
        [Export("setMetadata:error:")]
        void SetMetadata(NSData data, [NullAllowed] out OTError error);
    }

    interface IOTVideoCaptureConsumer
    {
    }

    // @protocol OTVideoCaptureConsumer <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoCaptureConsumer
    {
        // @required -(void)consumeFrame:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("consumeFrame:")]
        void ConsumeFrame(OTVideoFrame frame);

        // @required -(BOOL)consumeImageBuffer:(CVImageBufferRef _Nonnull)frame orientation:(OTVideoOrientation)orientation timestamp:(CMTime)ts metadata:(NSData * _Nullable)metadata;
        [Abstract]
        [Export("consumeImageBuffer:orientation:timestamp:metadata:")]
        bool ConsumeImageBuffer(CVImageBuffer frame, OTVideoOrientation orientation, CMTime ts, [NullAllowed] NSData metadata);
    }

    interface IOTVideoCapture
    {
    }

    // @protocol OTVideoCapture <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoCapture
    {
        // @required @property (assign, atomic) id<OTVideoCaptureConsumer> _Nullable videoCaptureConsumer;
        //
        // Set by the SDK, read by the implementation: frames are pushed into this consumer. Typed
        // NSObject rather than IOTVideoCaptureConsumer because the generator cannot emit a
        // protocol-typed abstract property on a [Model] type; wrap it yourself if you need the
        // protocol interface.
        [Abstract]
        [NullAllowed, Export("videoCaptureConsumer", ArgumentSemantic.Assign)]
        NSObject VideoCaptureConsumer { get; set; }

        // @required @property (readwrite, nonatomic) OTVideoContentHint videoContentHint;
        [Abstract]
        [Export("videoContentHint", ArgumentSemantic.Assign)]
        OTVideoContentHint VideoContentHint { get; set; }

        // @required -(void)initCapture;
        [Abstract]
        [Export("initCapture")]
        void InitCapture();

        // @required -(void)releaseCapture;
        [Abstract]
        [Export("releaseCapture")]
        void ReleaseCapture();

        // @required -(int32_t)startCapture;
        [Abstract]
        [Export("startCapture")]
        int StartCapture();

        // @required -(int32_t)stopCapture;
        [Abstract]
        [Export("stopCapture")]
        int StopCapture();

        // @required -(BOOL)isCaptureStarted;
        [Abstract]
        [Export("isCaptureStarted")]
        bool IsCaptureStarted { get; }

        // @required -(int32_t)captureSettings:(OTVideoFormat * _Nonnull)videoFormat;
        [Abstract]
        [Export("captureSettings:")]
        int CaptureSettings(OTVideoFormat videoFormat);
    }

    interface IOTVideoRender
    {
    }

    // @protocol OTVideoRender <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoRender
    {
        // @required -(void)renderVideoFrame:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("renderVideoFrame:")]
        void RenderVideoFrame(OTVideoFrame frame);
    }

    // @interface OTAudioDeviceManager : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioDeviceManager
    {
        // +(void)setAudioDevice:(id<OTAudioDevice> _Nullable)device;
        //
        // Process-wide and permanent: there is one capture source and one render target for the
        // whole app, and this must be called before connecting a session.
        [Static]
        [Export("setAudioDevice:")]
        void SetAudioDevice([NullAllowed] IOTAudioDevice device);

        // +(id<OTAudioDevice> _Nullable)currentAudioDevice;
        [Static]
        [NullAllowed, Export("currentAudioDevice")]
        IOTAudioDevice CurrentAudioDevice { get; }

        // +(id<OTAudioSessionManager> _Nullable)currentAudioSessionManager;
        //
        // The same object as CurrentAudioDevice when that device implements
        // OTAudioSessionManager, and null when it does not. The SDK's own default audio device
        // does implement it, so this is the entry point to CallKit integration without writing a
        // custom audio device — see IOTAudioSessionManager.
        [Static]
        [NullAllowed, Export("currentAudioSessionManager")]
        IOTAudioSessionManager CurrentAudioSessionManager { get; }
    }

    interface IOTAudioSessionManager
    {
    }

    // @protocol OTAudioSessionManager <NSObject>
    //
    // AVAudioSession control for calling services — in practice, CallKit.
    //
    // CallKit activates and deactivates the audio session itself, on its own schedule, and an app
    // that lets the SDK also manage the session ends up with the two fighting: audio that does not
    // start on answer, or that stops when a second call arrives. This protocol is how an app hands
    // that control over — the SDK still *configures* the session, but waits to be told when the
    // system has activated it.
    //
    // Optional for a custom audio device to implement, but the SDK's own default device already
    // does, which is what makes CallKit integration reachable without writing an OTAudioDevice at
    // all. Reach it through OTAudioDeviceManager.CurrentAudioSessionManager rather than by
    // constructing anything.
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTAudioSessionManager
    {
        // @required -(void)enableCallingServicesMode;
        //
        // Call once, early — at launch or before the first call. Until this is called the SDK
        // manages the session itself and the three methods below are ignored.
        [Abstract]
        [Export("enableCallingServicesMode")]
        void EnableCallingServicesMode();

        // @required -(void)preconfigureAudioSessionForCallWithMode:(AVAudioSessionMode _Nullable)mode;
        //
        // Configures the session but deliberately does *not* activate it. Call it in response to
        // CXAnswerCallAction or CXStartCallAction, so the session is already configured when the
        // system activates it. Pass null for the default (AVAudioSessionModeVoiceChat).
        [Abstract]
        [Export("preconfigureAudioSessionForCallWithMode:")]
        void PreconfigureAudioSessionForCall([NullAllowed] string mode);

        // @required -(void)audioSessionDidActivate:(AVAudioSession * _Nonnull)session;
        //
        // Forward CXProviderDelegate's provider:didActivateAudioSession: here.
        [Abstract]
        [Export("audioSessionDidActivate:")]
        void AudioSessionDidActivate(AVAudioSession session);

        // @required -(void)audioSessionDidDeactivate:(AVAudioSession * _Nonnull)session;
        //
        // Forward CXProviderDelegate's provider:didDeactivateAudioSession: here.
        [Abstract]
        [Export("audioSessionDidDeactivate:")]
        void AudioSessionDidDeactivate(AVAudioSession session);
    }

    // @interface OTAudioFormat : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioFormat
    {
        // @property (assign, nonatomic) uint16_t sampleRate;
        [Export("sampleRate")]
        ushort SampleRate { get; set; }

        // @property (assign, nonatomic) uint8_t numChannels;
        [Export("numChannels")]
        byte NumChannels { get; set; }
    }

    interface IOTAudioBus
    {
    }

    // @protocol OTAudioBus <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTAudioBus
    {
        // @required -(void)writeCaptureData:(void * _Nonnull)data numberOfSamples:(uint32_t)count;
        [Abstract]
        [Export("writeCaptureData:numberOfSamples:")]
        void WriteCaptureData(IntPtr data, uint count);

        // @required -(uint32_t)readRenderData:(void * _Nonnull)data numberOfSamples:(uint32_t)count;
        [Abstract]
        [Export("readRenderData:numberOfSamples:")]
        uint ReadRenderData(IntPtr data, uint count);
    }

    interface IOTAudioDevice
    {
    }

    // @protocol OTAudioDevice <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTAudioDevice
    {
        // @required -(BOOL)setAudioBus:(id<OTAudioBus> _Nullable)audioBus;
        [Abstract]
        [Export("setAudioBus:")]
        bool SetAudioBus([NullAllowed] IOTAudioBus audioBus);

        // @required -(OTAudioFormat * _Nonnull)captureFormat;
        [Abstract]
        [Export("captureFormat")]
        OTAudioFormat CaptureFormat { get; }

        // @required -(OTAudioFormat * _Nonnull)renderFormat;
        [Abstract]
        [Export("renderFormat")]
        OTAudioFormat RenderFormat { get; }

        // @required -(BOOL)renderingIsAvailable;
        [Abstract]
        [Export("renderingIsAvailable")]
        bool RenderingIsAvailable { get; }

        // @required -(BOOL)initializeRendering;
        [Abstract]
        [Export("initializeRendering")]
        bool InitializeRendering();

        // @required -(BOOL)renderingIsInitialized;
        [Abstract]
        [Export("renderingIsInitialized")]
        bool RenderingIsInitialized { get; }

        // @required -(BOOL)startRendering;
        [Abstract]
        [Export("startRendering")]
        bool StartRendering();

        // @required -(BOOL)stopRendering;
        [Abstract]
        [Export("stopRendering")]
        bool StopRendering();

        // @required -(BOOL)isRendering;
        [Abstract]
        [Export("isRendering")]
        bool IsRendering { get; }

        // @required -(uint16_t)estimatedRenderDelay;
        [Abstract]
        [Export("estimatedRenderDelay")]
        ushort EstimatedRenderDelay { get; }

        // @required -(BOOL)captureIsAvailable;
        [Abstract]
        [Export("captureIsAvailable")]
        bool CaptureIsAvailable { get; }

        // @required -(BOOL)initializeCapture;
        [Abstract]
        [Export("initializeCapture")]
        bool InitializeCapture();

        // @required -(BOOL)captureIsInitialized;
        [Abstract]
        [Export("captureIsInitialized")]
        bool CaptureIsInitialized { get; }

        // @required -(BOOL)startCapture;
        [Abstract]
        [Export("startCapture")]
        bool StartCapture();

        // @required -(BOOL)stopCapture;
        [Abstract]
        [Export("stopCapture")]
        bool StopCapture();

        // @required -(BOOL)isCapturing;
        [Abstract]
        [Export("isCapturing")]
        bool IsCapturing { get; }

        // @required -(uint16_t)estimatedCaptureDelay;
        [Abstract]
        [Export("estimatedCaptureDelay")]
        ushort EstimatedCaptureDelay { get; }
    }

    // @interface OTAudioData : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioData
    {
        // @property (assign, nonatomic) int16_t * _Nullable sampleBuffer;
        //
        // Raw interleaved 16-bit samples. Exposed as IntPtr: a custom audio transformer reads and
        // writes this buffer in place, so there is nothing sensible to marshal it into.
        [Export("sampleBuffer", ArgumentSemantic.Assign)]
        IntPtr SampleBuffer { get; set; }

        // @property (assign, nonatomic) uint32_t bitsPerSample;
        [Export("bitsPerSample")]
        uint BitsPerSample { get; set; }

        // @property (assign, nonatomic) uint32_t sampleRate;
        [Export("sampleRate")]
        uint SampleRate { get; set; }

        // @property (assign, nonatomic) uint32_t numberOfChannels;
        [Export("numberOfChannels")]
        uint NumberOfChannels { get; set; }

        // @property (assign, nonatomic) uint32_t numberOfSamples;
        [Export("numberOfSamples")]
        uint NumberOfSamples { get; set; }
    }

    // @interface OTTransportStats : NSObject
    //
    // The transport half of a media link — how the connection itself is doing, as opposed to how
    // one track is doing. Shared by audio and video, which ride the same transport.
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTTransportStats
    {
        // @property (readonly) int64_t connectionEstimatedBandwidth;
        [Export("connectionEstimatedBandwidth")]
        long ConnectionEstimatedBandwidth { get; }

        // @property (readonly) OTNetworkCondition networkCondition;
        [Export("networkCondition")]
        OTNetworkCondition NetworkCondition { get; }

        // @property (readonly) OTNetworkReason networkConditionReason;
        [Export("networkConditionReason")]
        OTNetworkReason NetworkConditionReason { get; }

        // -(instancetype _Nonnull)initWithConnectionEstimatedBandwidth:(int64_t)connectionEstimatedBandwidth networkCondition:(OTNetworkCondition)networkCondition networkConditionReason:(OTNetworkReason)networkConditionReason;
        [Export("initWithConnectionEstimatedBandwidth:networkCondition:networkConditionReason:")]
        NativeHandle Constructor(long connectionEstimatedBandwidth, OTNetworkCondition networkCondition, OTNetworkReason networkConditionReason);
    }

    // @interface OTSenderStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTSenderStats
    {
        // @property (readonly) int64_t connectionMaxAllocatedBitrate;
        [Export("connectionMaxAllocatedBitrate")]
        long ConnectionMaxAllocatedBitrate { get; }

        // @property (readonly) int64_t connectionEstimatedBandwidth;
        [Export("connectionEstimatedBandwidth")]
        long ConnectionEstimatedBandwidth { get; }

        // -(instancetype _Nonnull)initWithconnectionMaxAllocatedBitrate:(int64_t)connectionMaxAllocatedBitrate connectionEstimatedBandwidth:(int64_t)connectionEstimatedBandwidth;
        //
        // Lower-case "connection" in the selector is the SDK's own typo; the export has to match it.
        [Export("initWithconnectionMaxAllocatedBitrate:connectionEstimatedBandwidth:")]
        NativeHandle Constructor(long connectionMaxAllocatedBitrate, long connectionEstimatedBandwidth);
    }

    // @interface OTSubscriberKitVideoNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTSubscriberKitVideoNetworkStats
    {
        // @property (readonly) uint64_t videoPacketsLost;
        [Export("videoPacketsLost")]
        ulong VideoPacketsLost { get; }

        // @property (readonly) uint64_t videoPacketsReceived;
        [Export("videoPacketsReceived")]
        ulong VideoPacketsReceived { get; }

        // @property (readonly) uint64_t videoBytesReceived;
        [Export("videoBytesReceived")]
        ulong VideoBytesReceived { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (readonly, nullable) OTSenderStats *senderStats __attribute__((deprecated(...)));
        [Obsolete("Use the subscriber's media link stats (OTSubscriberKitMediaLinkStats) instead.")]
        [NullAllowed, Export("senderStats")]
        OTSenderStats SenderStats { get; }

        // @property (readonly) int32_t width;
        [Export("width")]
        int Width { get; }

        // @property (readonly) int32_t height;
        [Export("height")]
        int Height { get; }

        // @property (readonly) double decodedFrameRate;
        [Export("decodedFrameRate")]
        double DecodedFrameRate { get; }

        // @property (readonly) int64_t bitrate;
        [Export("bitrate")]
        long Bitrate { get; }

        // @property (readonly) int64_t totalBitrate;
        [Export("totalBitrate")]
        long TotalBitrate { get; }

        // @property (readonly) int64_t pauseCount;
        [Export("pauseCount")]
        long PauseCount { get; }

        // @property (readonly) int64_t totalPausesDuration;
        [Export("totalPausesDuration")]
        long TotalPausesDuration { get; }

        // @property (readonly) int64_t freezeCount;
        [Export("freezeCount")]
        long FreezeCount { get; }

        // @property (readonly) int64_t totalFreezesDuration;
        [Export("totalFreezesDuration")]
        long TotalFreezesDuration { get; }

        // @property (readonly, nullable) NSString *codec;
        [NullAllowed, Export("codec")]
        string Codec { get; }

        // -(instancetype _Nonnull)initWithPacketsLost:packetsReceived:bytesReceived:timestamp:
        [Export("initWithPacketsLost:packetsReceived:bytesReceived:timestamp:")]
        NativeHandle Constructor(ulong packetsLost, ulong packetsReceived, ulong bytesReceived, double timestamp);
    }

    // @interface OTSubscriberKitMediaLinkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTSubscriberKitMediaLinkStats
    {
        // @property (readonly, nonnull) OTTransportStats *transport;
        //
        // This device's own leg of the link.
        [Export("transport")]
        OTTransportStats Transport { get; }

        // @property (readonly, nonnull) OTTransportStats *remotePublisherTransport;
        //
        // The publishing device's leg — which is what makes NetworkDegradationSource answerable.
        [Export("remotePublisherTransport")]
        OTTransportStats RemotePublisherTransport { get; }

        // @property (readonly) OTNetworkDegradationSource networkDegradationSource;
        [Export("networkDegradationSource")]
        OTNetworkDegradationSource NetworkDegradationSource { get; }

        // -(instancetype _Nonnull)initWithTransport:remotePublisherTransport:networkDegradationSource:
        [Export("initWithTransport:remotePublisherTransport:networkDegradationSource:")]
        NativeHandle Constructor(OTTransportStats transport, OTTransportStats remotePublisherTransport, OTNetworkDegradationSource networkDegradationSource);
    }

    // @interface OTSubscriberKitAudioNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTSubscriberKitAudioNetworkStats
    {
        // @property (readonly) uint64_t audioPacketsLost;
        [Export("audioPacketsLost")]
        ulong AudioPacketsLost { get; }

        // @property (readonly) uint64_t audioPacketsReceived;
        [Export("audioPacketsReceived")]
        ulong AudioPacketsReceived { get; }

        // @property (readonly) uint64_t audioBytesReceived;
        [Export("audioBytesReceived")]
        ulong AudioBytesReceived { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (readonly, nullable) OTSenderStats *senderStats __attribute__((deprecated(...)));
        [Obsolete("Use the subscriber's media link stats (OTSubscriberKitMediaLinkStats) instead.")]
        [NullAllowed, Export("senderStats")]
        OTSenderStats SenderStats { get; }

        // -(instancetype _Nonnull)initWithPacketsLost:packetsReceived:bytesReceived:timestamp:
        [Export("initWithPacketsLost:packetsReceived:bytesReceived:timestamp:")]
        NativeHandle Constructor(ulong packetsLost, ulong packetsReceived, ulong bytesReceived, double timestamp);
    }

    // @interface OTPublisherKitMediaLinkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTPublisherKitMediaLinkStats
    {
        // @property (nonnull, readonly) NSString* connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (nonnull, readonly) NSString* subscriberId;
        [Export("subscriberId")]
        string SubscriberId { get; }

        // @property (readonly, nonnull) OTTransportStats *transport;
        [Export("transport")]
        OTTransportStats Transport { get; }

        // -(instancetype _Nonnull)initWithConnectionId:subscriberId:transport:
        [Export("initWithConnectionId:subscriberId:transport:")]
        NativeHandle Constructor(string connectionId, string subscriberId, OTTransportStats transport);
    }

    // @interface OTPublisherKitVideoLayerStats : NSObject
    //
    // One simulcast layer. A publisher sending three resolutions reports three of these per update.
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTPublisherKitVideoLayerStats
    {
        // @property (readonly) int32_t width;
        [Export("width")]
        int Width { get; }

        // @property (readonly) int32_t height;
        [Export("height")]
        int Height { get; }

        // @property (readonly) double encodedFrameRate;
        [Export("encodedFrameRate")]
        double EncodedFrameRate { get; }

        // @property (readonly) int64_t bitrate;
        [Export("bitrate")]
        long Bitrate { get; }

        // @property (readonly) int64_t totalBitrate;
        [Export("totalBitrate")]
        long TotalBitrate { get; }

        // @property (nullable, readonly) NSString* scalabilityMode;
        [NullAllowed, Export("scalabilityMode")]
        string ScalabilityMode { get; }

        // @property (readonly) OTPublisherVideoEventReason qualityLimitationReason;
        [Export("qualityLimitationReason")]
        OTPublisherVideoEventReason QualityLimitationReason { get; }

        // @property (nullable, readonly) NSString* codec;
        [NullAllowed, Export("codec")]
        string Codec { get; }

        // -(instancetype _Nonnull)initWithWidth:height:encodedFrameRate:bitrate:totalBitrate:scalabilityMode:qualityLimitationReason:codec:
        [Export("initWithWidth:height:encodedFrameRate:bitrate:totalBitrate:scalabilityMode:qualityLimitationReason:codec:")]
        NativeHandle Constructor(int width, int height, double encodedFrameRate, long bitrate, long totalBitrate, [NullAllowed] string scalabilityMode, OTPublisherVideoEventReason qualityLimitationReason, [NullAllowed] string codec);
    }

    // @interface OTPublisherKitVideoNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTPublisherKitVideoNetworkStats
    {
        // @property (nonnull, readonly) NSString* connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (nonnull, readonly) NSString* subscriberId;
        [Export("subscriberId")]
        string SubscriberId { get; }

        // @property (readonly) int64_t videoPacketsLost;
        [Export("videoPacketsLost")]
        long VideoPacketsLost { get; }

        // @property (readonly) int64_t videoPacketsSent;
        [Export("videoPacketsSent")]
        long VideoPacketsSent { get; }

        // @property (readonly) int64_t videoBytesSent;
        [Export("videoBytesSent")]
        long VideoBytesSent { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (nonnull, readonly) NSArray<OTPublisherKitVideoLayerStats*>* videoLayers;
        [Export("videoLayers")]
        OTPublisherKitVideoLayerStats[] VideoLayers { get; }

        // @property (readonly, nullable) OTTransportStats *transport;
        [NullAllowed, Export("transport")]
        OTTransportStats Transport { get; }

        // @property (readonly) double startTime;
        [Export("startTime")]
        double StartTime { get; }

        // -(instancetype _Nonnull)initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:videoLayers:transport:
        [Export("initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:videoLayers:transport:")]
        NativeHandle Constructor(string connectionId, string subscriberId, long packetsLost, long packetsSent, long bytesSent, double timestamp, double startTime, OTPublisherKitVideoLayerStats[] videoLayers, [NullAllowed] OTTransportStats transport);
    }

    // @interface OTPublisherKitAudioNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface OTPublisherKitAudioNetworkStats
    {
        // @property (nonnull, readonly) NSString* connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (nonnull, readonly) NSString* subscriberId;
        [Export("subscriberId")]
        string SubscriberId { get; }

        // @property (readonly) int64_t audioPacketsLost;
        [Export("audioPacketsLost")]
        long AudioPacketsLost { get; }

        // @property (readonly) int64_t audioPacketsSent;
        [Export("audioPacketsSent")]
        long AudioPacketsSent { get; }

        // @property (readonly) int64_t audioBytesSent;
        [Export("audioBytesSent")]
        long AudioBytesSent { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (readonly) double startTime;
        [Export("startTime")]
        double StartTime { get; }

        // @property (readonly, nullable) OTTransportStats *transport;
        [NullAllowed, Export("transport")]
        OTTransportStats Transport { get; }

        // -(instancetype _Nonnull)initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:transport:
        [Export("initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:transport:")]
        NativeHandle Constructor(string connectionId, string subscriberId, long packetsLost, long packetsSent, long bytesSent, double timestamp, double startTime, [NullAllowed] OTTransportStats transport);
    }

    // @interface OTPublisherSettings : OTPublisherKitSettings
    [BaseType(typeof(OTPublisherKitSettings))]
    interface OTPublisherSettings
    {
        // @property (nonatomic, strong) id<OTVideoCapture> _Nullable videoCapture;
        [NullAllowed, Export("videoCapture", ArgumentSemantic.Strong)]
        IOTVideoCapture VideoCapture { get; set; }

        // @property (nonatomic) OTCameraCaptureResolution cameraResolution;
        [Export("cameraResolution", ArgumentSemantic.Assign)]
        OTCameraCaptureResolution CameraResolution { get; set; }

        // @property (nonatomic) OTCameraCaptureFrameRate cameraFrameRate;
        [Export("cameraFrameRate", ArgumentSemantic.Assign)]
        OTCameraCaptureFrameRate CameraFrameRate { get; set; }
    }

    // @interface OTPublisher : OTPublisherKit
    [BaseType(typeof(OTPublisherKit))]
    [DisableDefaultCtor]
    interface OTPublisher
    {
        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate settings:(OTPublisherSettings * _Nonnull)settings;
        [Export("initWithDelegate:settings:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate, OTPublisherSettings settings);

        // Inherited from OTPublisherKit; redeclared because bound constructors are not inherited.
        [Export("initWithDelegate:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name cameraResolution:(OTCameraCaptureResolution)cameraResolution cameraFrameRate:(OTCameraCaptureFrameRate)cameraFrameRate __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Obsolete("Use the constructor taking OTPublisherSettings instead.")]
        [Export("initWithDelegate:name:cameraResolution:cameraFrameRate:")]
        NativeHandle Constructor([NullAllowed] IOTPublisherKitDelegate @delegate, [NullAllowed] string name, OTCameraCaptureResolution cameraResolution, OTCameraCaptureFrameRate cameraFrameRate);

        // @property (readonly) UIView * _Nullable view;
        //
        // Valid as soon as the publisher exists — the camera preview renders into it before the
        // stream is created, which is why samples attach it immediately rather than on a callback.
        [NullAllowed, Export("view")]
        UIView View { get; }

        // @property (nonatomic) OTVideoViewScaleBehavior viewScaleBehavior;
        [Export("viewScaleBehavior", ArgumentSemantic.Assign)]
        OTVideoViewScaleBehavior ViewScaleBehavior { get; set; }

        // @property (nonatomic) AVCaptureDevicePosition cameraPosition;
        [Export("cameraPosition", ArgumentSemantic.Assign)]
        AVCaptureDevicePosition CameraPosition { get; set; }

        // @property (nonatomic) BOOL cameraTorch;
        //
        // A preference: the front camera generally has no torch, and setting this there does
        // nothing. Reading it back reports the active camera's real state once publishing.
        [Export("cameraTorch")]
        bool CameraTorch { get; set; }

        // @property (nonatomic) float cameraZoomFactor;
        //
        // 1.0 is no zoom; below 1.0 asks for ultra-wide, which not every camera has. Values outside
        // the active camera's range are clamped rather than rejected.
        [Export("cameraZoomFactor")]
        float CameraZoomFactor { get; set; }
    }
}
