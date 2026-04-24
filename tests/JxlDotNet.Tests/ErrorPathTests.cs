using JxlDotNet;

namespace JxlDotNet.Tests;

public class ErrorPathTests
{
    [Fact]
    public void DecodeEmptyThrows()
    {
        Assert.Throws<JxlException>(() => JxlDecoder.Decode([]));
    }

    [Fact]
    public void DecodeGarbageThrows()
    {
        var garbage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03 };
        Assert.Throws<JxlException>(() => JxlDecoder.Decode(garbage));
    }

    [Fact]
    public void EncodeWithWrongBufferSizeThrows()
    {
        var pixels = new byte[10];
        Assert.Throws<JxlException>(() => JxlEncoder.Encode(pixels, 4, 4, default));
    }

    [Fact]
    public void EncodeWithZeroDimensionsThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JxlEncoder.Encode(new byte[0], 0, 0, default));
    }
}
