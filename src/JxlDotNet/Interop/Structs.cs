using System.Runtime.InteropServices;

namespace JxlDotNet.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct JxlPixelFormat
{
    public uint NumChannels;
    public JxlDataType DataType;
    public JxlEndianness Endianness;
    public nuint Align;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JxlPreviewHeader
{
    public uint Xsize;
    public uint Ysize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JxlAnimationHeader
{
    public uint TpsNumerator;
    public uint TpsDenominator;
    public uint NumLoops;
    public int HaveTimecodes;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JxlBasicInfo
{
    public int HaveContainer;
    public uint Xsize;
    public uint Ysize;
    public uint BitsPerSample;
    public uint ExponentBitsPerSample;
    public float IntensityTarget;
    public float MinNits;
    public int RelativeToMaxDisplay;
    public float LinearBelow;
    public int UsesOriginalProfile;
    public int HavePreview;
    public int HaveAnimation;
    public JxlOrientation Orientation;
    public uint NumColorChannels;
    public uint NumExtraChannels;
    public uint AlphaBits;
    public uint AlphaExponentBits;
    public int AlphaPremultiplied;
    public JxlPreviewHeader Preview;
    public JxlAnimationHeader Animation;
    public uint IntrinsicXsize;
    public uint IntrinsicYsize;
    public fixed byte Padding[100];
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct JxlColorEncoding
{
    public JxlColorSpace ColorSpace;
    public JxlWhitePoint WhitePoint;
    public fixed double WhitePointXy[2];
    public JxlPrimaries Primaries;
    public fixed double PrimariesRedXy[2];
    public fixed double PrimariesGreenXy[2];
    public fixed double PrimariesBlueXy[2];
    public JxlTransferFunction TransferFunction;
    public double Gamma;
    public JxlRenderingIntent RenderingIntent;
}
