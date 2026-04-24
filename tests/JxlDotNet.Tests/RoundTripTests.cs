using JxlDotNet;

namespace JxlDotNet.Tests;

public class RoundTripTests
{
    [Fact]
    public void LosslessRoundTripPreservesPixels()
    {
        const int w = 4, h = 4;
        var pixels = new byte[w * h * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = 255;
            pixels[i + 1] = 0;
            pixels[i + 2] = 0;
            pixels[i + 3] = 255;
        }

        var encoded = JxlEncoder.Encode(pixels, w, h, new EncoderOptions(Lossless: true));
        Assert.NotEmpty(encoded);

        var decoded = JxlDecoder.Decode(encoded);
        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        Assert.Equal(pixels, decoded.PixelsRgba8.ToArray());
    }

    [Fact]
    public void LossyEncodeProducesSmallerOutputThanLossless()
    {
        const int w = 64, h = 64;
        var pixels = new byte[w * h * 4];
        var rng = new Random(42);
        rng.NextBytes(pixels);
        for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;

        var lossless = JxlEncoder.Encode(pixels, w, h, new EncoderOptions(Lossless: true));
        var lossy    = JxlEncoder.Encode(pixels, w, h, new EncoderOptions(Distance: 3.0f));

        Assert.NotEmpty(lossless);
        Assert.NotEmpty(lossy);
        Assert.True(lossy.Length < lossless.Length,
            $"lossy={lossy.Length}, lossless={lossless.Length}");
    }
}
