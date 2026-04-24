namespace JxlDotNet;

public sealed class DecodedImage
{
    public int Width { get; }
    public int Height { get; }
    public ReadOnlyMemory<byte> PixelsRgba8 { get; }
    public ReadOnlyMemory<byte> IccProfile { get; }

    internal DecodedImage(int width, int height, byte[] pixels, byte[] icc)
    {
        Width = width;
        Height = height;
        PixelsRgba8 = pixels;
        IccProfile = icc;
    }
}
