using ObjCRuntime;

namespace OpenTok.Net.iOS
{
    // Every enum here is transcribed from OpenTok.framework's own headers (OTError.h, OTSession.h,
    // OTVideoKit.h, OTVideoEventReason.h, OTPublisherKit.h, OTPublisher.h, OTStream.h,
    // OTNetworkStatsKit.h) at the SDK version pinned in Directory.Build.props.
    //
    // Two rules hold throughout, and both matter:
    //
    //   * The underlying type is the header's, not a guess. NS_ENUM(NSInteger, ...) is [Native]
    //     (nint, 64-bit on device); NS_ENUM(int32_t, ...) is a plain C# enum over int. Getting this
    //     wrong reads the wrong number of bytes off the stack for every value the SDK hands back.
    //   * Members carry the header's numeric values explicitly, even where they are 0,1,2… in
    //     order. A member missing from a set the SDK actually sends turns a delegate callback into
    //     an undefined enum value, which is silent — see OTPublisherVideoEventReason below, where
    //     the SDK sends 5, 6, 7, 8 and 9.

    /// <summary>Values for <see cref="OTSession.SessionConnectionStatus"/>.</summary>
    // typedef NS_ENUM(int32_t, OTSessionConnectionStatus)
    public enum OTSessionConnectionStatus
    {
        NotConnected = 0,
        Connected = 1,
        Connecting = 2,
        Reconnecting = 3,
        Disconnecting = 4,
        Failed = 5
    }

    /// <summary>Values for <see cref="OTSessionICEConfig.IncludeServers"/>.</summary>
    // typedef NS_ENUM(int32_t, OTSessionICEIncludeServers)
    public enum OTSessionICEIncludeServers
    {
        All = 0,
        Custom = 1
    }

    /// <summary>Values for <see cref="OTSessionICEConfig.TransportPolicy"/>.</summary>
    // typedef NS_ENUM(int32_t, OTSessionICETransportPolicy)
    public enum OTSessionICETransportPolicy
    {
        All = 0,
        Relay = 1
    }

    /// <summary>Values for <see cref="OTPublisherKit.VideoType"/>.</summary>
    // typedef NS_ENUM(int32_t, OTPublisherKitVideoType)
    public enum OTPublisherKitVideoType
    {
        Camera = 1,
        Screen = 2
    }

    /// <summary>Values for <see cref="OTStream.VideoType"/>.</summary>
    // typedef NS_ENUM(int32_t, OTStreamVideoType)
    public enum OTStreamVideoType
    {
        Camera = 1,
        Screen = 2,
        Custom = 3
    }

    /// <summary>
    /// The <c>code</c> of an <see cref="OTError"/> reported through
    /// <c>OTSessionDelegate.DidFailWithError</c>. Cast the error's <c>Code</c> to compare:
    /// <c>(OTSessionErrorCode)error.Code</c>.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTSessionErrorCode)
    public enum OTSessionErrorCode
    {
        SessionSuccess = 0,
        AuthorizationFailure = 1004,
        ErrorInvalidSession = 1005,
        ConnectionFailed = 1006,
        NotConnected = 1010,
        NullOrInvalidParameter = 1011,
        SessionIllegalState = 1015,
        SessionStateFailed = 1020,
        SessionConnectionTimeout = 1021,
        ConnectionDropped = 1022,
        ConnectionRefused = 1023,
        SessionBlockedCountry = 1026,
        SessionConnectionLimitExceeded = 1027,
        SessionSubscriberNotFound = 1112,
        SessionPublisherNotFound = 1113,
        SessionSignalDataTooLong = 1413,
        P2PSessionMaxParticipants = 1403,
        SessionInvalidSignalType = 1461,
        NoMessagingServer = 1503,
        /// <summary>A force-disconnect failed — the token's role does not permit it.</summary>
        SessionUnableToForceDisconnect = 1520,
        SessionUnableToForceMute = 1540,
        /// <summary>A signal could not be sent — the token's role does not permit it.</summary>
        SessionUnableToSendSignal = 1550,
        SessionInternalError = 2000,
        SessionEncryptionSecretMissing = 6000,
        SessionInvalidEncryptionSecret = 6004
    }

    /// <summary>The <c>code</c> of an <see cref="OTError"/> reported to a publisher's delegate.</summary>
    // typedef NS_ENUM(int32_t, OTPublisherErrorCode)
    public enum OTPublisherErrorCode
    {
        PublisherSuccess = 0,
        SessionDisconnected = 1010,
        PublisherWebRTCError = 1610,
        PublisherInternalError = 2000,
        PublisherEncryptionInternalError = 6001,
        /// <summary>End-to-end encryption is on and the negotiated codec does not support it.</summary>
        EncryptionUnsupportedCodec = 6005,
        /// <summary>
        /// The mode passed to <see cref="OTPublisherKit.SetTargetScalabilityMode"/> is null or not
        /// one of L1T1 through L3T3. Added in 2.35.1 as OTPublisherInvalidScalabilityMode.
        /// </summary>
        PublisherInvalidScalabilityMode = 6006
    }

    /// <summary>The <c>code</c> of an <see cref="OTError"/> reported to a subscriber's delegate.</summary>
    // typedef NS_ENUM(int32_t, OTSubscriberErrorCode)
    public enum OTSubscriberErrorCode
    {
        OTSubscriberSuccess = 0,
        OTSubscriberSessionDisconnected = 1010,
        OTConnectionTimedOut = 1542,
        OTSubscriberWebRTCError = 1600,
        OTSubscriberServerCannotFindStream = 1604,
        OTSubscriberStreamLimitExceeded = 1605,
        /// <summary>The subscriber's peer connection never reached the connected state.</summary>
        OTSubscriberPeerConnectionNotConnected = 1606,
        OTSubscriberInternalError = 2000,
        OTSubscriberDecryptionInternalError = 6002,
        EncryptionSecretMismatch = 6003,
        /// <summary>End-to-end encryption is on and the negotiated codec does not support it.</summary>
        DecryptionUnsupportedCodec = 6006
    }

    /// <summary>Values for <see cref="OTVideoFrame.Orientation"/>.</summary>
    // typedef NS_ENUM(int32_t, OTVideoOrientation)
    public enum OTVideoOrientation
    {
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    /// <summary>
    /// Values for <see cref="OTVideoFormat.PixelFormat"/>. The header spells these as FourCC
    /// character literals (<c>'I420'</c>); the numbers here are those literals' integer values.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTPixelFormat)
    public enum OTPixelFormat
    {
        /// <summary><c>'I420'</c></summary>
        I420 = 1228157488,
        /// <summary><c>'ARGB'</c></summary>
        Argb = 1095911234,
        /// <summary><c>'NV12'</c></summary>
        Nv12 = 1314271538
    }

    /// <summary>
    /// How a publisher's or subscriber's render view fits its frame — see
    /// <see cref="OTPublisher.ViewScaleBehavior"/> and <see cref="OTSubscriber.ViewScaleBehavior"/>.
    /// </summary>
    /// <remarks>
    /// The header declares <c>Fit</c> first and <c>Fill</c> second, so <c>Fit</c> is 0 — which is
    /// *not* the default: the SDK documents the default as <c>Fill</c>, and sets it itself. Do not
    /// assume default(OTVideoViewScaleBehavior) is what the view is already doing.
    /// </remarks>
    // typedef NS_ENUM(NSInteger, OTVideoViewScaleBehavior)
    [Native]
    public enum OTVideoViewScaleBehavior : long
    {
        /// <summary>Scale to fit inside the view, letterboxing whatever is left over.</summary>
        Fit = 0,
        /// <summary>Scale to fill the view, cropping the overflow. The SDK's own default.</summary>
        Fill = 1
    }

    /// <summary>Values for <c>OTVideoCapture.VideoContentHint</c>.</summary>
    // typedef NS_ENUM(NSInteger, OTVideoContentHint)
    [Native]
    public enum OTVideoContentHint : long
    {
        None = 0,
        Motion = 1,
        Detail = 2,
        Text = 3
    }

    /// <summary>
    /// Why a publisher's video was disabled, enabled, or changed quality — the <c>reason</c>
    /// argument of <c>OTPublisherKitDelegate</c>'s video callbacks.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTPublisherVideoEventReason)
    public enum OTPublisherVideoEventReason
    {
        None = 0,
        PublisherPropertyChanged = 1,
        QualityChanged = 3,
        /// <summary>Quality was reduced because the available bandwidth dropped.</summary>
        QualityDegradationBandwidth = 5,
        /// <summary>Quality was reduced because the device could not keep up.</summary>
        QualityDegradationCPU = 6,
        QualityDegradationOther = 7,
        /// <summary>The negotiated video codec changed mid-stream.</summary>
        CodecChange = 8,
        /// <summary>The encoded resolution, or the set of simulcast layers, changed.</summary>
        ResolutionChange = 9
    }

    /// <summary>
    /// Why a subscriber's video was disabled, enabled, or changed quality — the <c>reason</c>
    /// argument of <c>OTSubscriberKitDelegate</c>'s video callbacks.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTSubscriberVideoEventReason)
    public enum OTSubscriberVideoEventReason
    {
        None = 0,
        PublisherPropertyChanged = 1,
        SubscriberPropertyChanged = 2,
        QualityChanged = 3,
        CodecNotSupported = 4,
        /// <summary>The negotiated video codec changed mid-stream.</summary>
        CodecChange = 8,
        /// <summary>The decoded resolution changed.</summary>
        ResolutionChange = 9,
        /// <summary>Video stopped arriving without the track being disabled.</summary>
        VideoInterruption = 10
    }

    /// <summary>
    /// Why a network condition changed — the <c>reason</c> argument of the
    /// <c>networkConditionChanged:</c> publisher and subscriber callbacks, and
    /// <see cref="OTTransportStats.NetworkConditionReason"/>.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTNetworkReason)
    public enum OTNetworkReason
    {
        None = 0,
        Unknown = 1,
        Bandwidth = 2,
        PacketLoss = 3,
        NetworkConditionChange = 4
    }

    /// <summary>Overall link health — see <see cref="OTTransportStats.NetworkCondition"/>.</summary>
    // typedef NS_ENUM(int32_t, OTNetworkCondition)
    public enum OTNetworkCondition
    {
        Unknown = 0,
        Critical = 1,
        Warning = 2,
        Fair = 3,
        Good = 4,
        Excellent = 5
    }

    /// <summary>
    /// Which end of a subscriber's link is degrading — see
    /// <see cref="OTSubscriberKitMediaLinkStats.NetworkDegradationSource"/>.
    /// </summary>
    // typedef NS_ENUM(int32_t, OTNetworkDegradationSource)
    public enum OTNetworkDegradationSource
    {
        None = 0,
        BothOrUnclear = 1,
        Local = 2,
        Remote = 3
    }

    /// <summary>
    /// A preset bandwidth strategy for a publisher — see
    /// <see cref="OTPublisherKit.VideoBitratePreset"/>. Mutually exclusive with
    /// <see cref="OTPublisherKit.MaxVideoBitrate"/>: setting that property switches this one to
    /// <see cref="Custom"/>, which cannot be assigned directly.
    /// </summary>
    // typedef NS_ENUM(NSInteger, OTVideoBitratePreset)
    [Native]
    public enum OTVideoBitratePreset : long
    {
        Default = 0,
        BwSaver = 1,
        ExtraBwSaver = 2,
        /// <summary>Reported, never assigned — the result of setting <c>MaxVideoBitrate</c>.</summary>
        Custom = 3
    }

    /// <summary>
    /// A video codec, as carried (boxed in <c>NSNumber</c>) by
    /// <see cref="OTVideoCodecPreference.Codecs"/>.
    /// </summary>
    // typedef NS_ENUM(NSInteger, OTVideoCodecType)
    [Native]
    public enum OTVideoCodecType : long
    {
        VP8 = 1,
        H264 = 2,
        VP9 = 3
    }

    /// <summary>
    /// What the video engine should sacrifice first under limited bandwidth or CPU — see
    /// <see cref="OTPublisherKit.DegradationPreference"/>. Best effort, not a guarantee.
    /// </summary>
    // typedef NS_ENUM(NSInteger, OTDegradationPreference)
    [Native]
    public enum OTDegradationPreference : long
    {
        /// <summary>Let the engine decide. The default.</summary>
        NotSet = -1,
        MaintainFrameRateAndResolution = 0,
        MaintainFrameRate = 1,
        MaintainResolution = 2,
        Balanced = 3
    }

    /// <summary>Values for <see cref="OTPublisherSettings.CameraResolution"/>.</summary>
    // typedef NS_ENUM(NSInteger, OTCameraCaptureResolution)
    [Native]
    public enum OTCameraCaptureResolution : long
    {
        Low = 0,
        /// <summary>AVCaptureSessionPreset640x480.</summary>
        Medium = 1,
        /// <summary>AVCaptureSessionPreset1280x720.</summary>
        High = 2,
        /// <summary>AVCaptureSessionPreset1920x1080.</summary>
        High1080p = 3
    }

    /// <summary>Values for <see cref="OTPublisherSettings.CameraFrameRate"/>.</summary>
    // typedef NS_ENUM(NSInteger, OTCameraCaptureFrameRate)
    [Native]
    public enum OTCameraCaptureFrameRate : long
    {
        OTCameraCaptureFrameRate30FPS = 30,
        OTCameraCaptureFrameRate15FPS = 15,
        OTCameraCaptureFrameRate7FPS = 7,
        OTCameraCaptureFrameRate1FPS = 1
    }
}
