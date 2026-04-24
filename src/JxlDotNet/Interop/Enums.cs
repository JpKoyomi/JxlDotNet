namespace JxlDotNet.Interop;

internal enum JxlDataType
{
    Float = 0,
    Uint8 = 2,
    Uint16 = 3,
    Float16 = 5,
}

internal enum JxlEndianness
{
    Native = 0,
    Little = 1,
    Big = 2,
}

internal enum JxlSignature
{
    NotEnoughBytes = 0,
    Invalid = 1,
    Codestream = 2,
    Container = 3,
}

internal enum JxlDecoderStatus
{
    Success = 0,
    Error = 1,
    NeedMoreInput = 2,
    NeedPreviewOutBuffer = 3,
    NeedImageOutBuffer = 5,
    JpegNeedMoreOutput = 6,
    BoxNeedMoreOutput = 7,
    BasicInfo = 0x40,
    ColorEncoding = 0x100,
    PreviewImage = 0x200,
    Frame = 0x400,
    FullImage = 0x1000,
    JpegReconstruction = 0x2000,
    Box = 0x4000,
    FrameProgression = 0x8000,
    BoxComplete = 0x10000,
}

internal enum JxlEncoderStatus
{
    Success = 0,
    Error = 1,
    NeedMoreOutput = 2,
}

internal enum JxlEncoderError
{
    Ok = 0,
    Generic = 1,
    OutOfMemory = 2,
    Jbrd = 3,
    BadInput = 4,
    NotSupported = 0x80,
    ApiUsage = 0x81,
}

internal enum JxlEncoderFrameSettingId
{
    Effort = 0,
    DecodingSpeed = 1,
    Resampling = 2,
    ExtraChannelResampling = 3,
    AlreadyDownsampled = 4,
    PhotonNoise = 5,
    Noise = 6,
    Dots = 7,
    Patches = 8,
    Epf = 9,
    Gaborish = 10,
    Modular = 11,
    KeepInvisible = 12,
}

internal enum JxlOrientation
{
    Identity = 1,
    FlipHorizontal = 2,
    Rotate180 = 3,
    FlipVertical = 4,
    Transpose = 5,
    Rotate90Cw = 6,
    AntiTranspose = 7,
    Rotate90Ccw = 8,
}

internal enum JxlColorSpace
{
    Rgb = 0,
    Gray = 1,
    Xyb = 2,
    Unknown = 3,
}

internal enum JxlWhitePoint
{
    D65 = 1,
    Custom = 2,
    E = 10,
    Dci = 11,
}

internal enum JxlPrimaries
{
    Srgb = 1,
    Custom = 2,
    Bt2100 = 9,
    P3 = 11,
}

internal enum JxlTransferFunction
{
    Bt709 = 1,
    Unknown = 2,
    Linear = 8,
    Srgb = 13,
    Pq = 16,
    Dci = 17,
    Hlg = 18,
    Gamma = 65535,
}

internal enum JxlRenderingIntent
{
    Perceptual = 0,
    Relative = 1,
    Saturation = 2,
    Absolute = 3,
}
