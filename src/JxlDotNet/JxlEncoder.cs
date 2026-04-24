using JxlDotNet.Interop;

namespace JxlDotNet;

public sealed class JxlEncoder : IDisposable
{
    private readonly JxlEncoderSafeHandle _encoder;
    private readonly JxlRunnerSafeHandle _runner;

    public JxlEncoder()
    {
        _encoder = NativeMethods.JxlEncoderCreate(0);
        if (_encoder.IsInvalid)
            throw new JxlException("JxlEncoderCreate returned null.");

        _runner = NativeMethods.JxlResizableParallelRunnerCreate(0);
        if (_runner.IsInvalid)
        {
            _encoder.Dispose();
            throw new JxlException("JxlResizableParallelRunnerCreate returned null.");
        }
    }

    public static byte[] Encode(ReadOnlySpan<byte> rgba8, int width, int height, EncoderOptions options = default)
    {
        using var encoder = new JxlEncoder();
        return encoder.EncodeOneShot(rgba8, width, height, options);
    }

    public unsafe byte[] EncodeOneShot(ReadOnlySpan<byte> rgba8, int width, int height, EncoderOptions options = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var expected = checked((long)width * height * 4);
        if (rgba8.Length != expected)
            throw new JxlException($"RGBA8 buffer size mismatch: expected {expected} bytes, got {rgba8.Length}.");

        var runnerFunc = NativeMethods.GetResizableRunnerFunctionPointer();
        if (NativeMethods.JxlEncoderSetParallelRunner(_encoder, runnerFunc, _runner.DangerousGetHandle())
            != JxlEncoderStatus.Success)
        {
            throw new JxlException("JxlEncoderSetParallelRunner failed.");
        }
        NativeMethods.JxlResizableParallelRunnerSetThreads(
            _runner,
            NativeMethods.JxlResizableParallelRunnerSuggestThreads((ulong)width, (ulong)height));

        NativeMethods.JxlEncoderInitBasicInfo(out var info);
        info.Xsize = (uint)width;
        info.Ysize = (uint)height;
        info.BitsPerSample = 8;
        info.ExponentBitsPerSample = 0;
        info.NumColorChannels = 3;
        info.NumExtraChannels = 1;
        info.AlphaBits = 8;
        info.AlphaExponentBits = 0;
        info.AlphaPremultiplied = 0;
        info.UsesOriginalProfile = options.Lossless ? 1 : 0;

        if (NativeMethods.JxlEncoderSetBasicInfo(_encoder, info) != JxlEncoderStatus.Success)
            throw EncoderError("JxlEncoderSetBasicInfo");

        NativeMethods.JxlColorEncodingSetToSRGB(out var colorEncoding, isGray: false);
        if (NativeMethods.JxlEncoderSetColorEncoding(_encoder, colorEncoding) != JxlEncoderStatus.Success)
            throw EncoderError("JxlEncoderSetColorEncoding");

        var frameSettings = NativeMethods.JxlEncoderFrameSettingsCreate(_encoder, 0);
        if (frameSettings == 0)
            throw new JxlException("JxlEncoderFrameSettingsCreate returned null.");

        if (options.Lossless)
        {
            if (NativeMethods.JxlEncoderSetFrameLossless(frameSettings, true) != JxlEncoderStatus.Success)
                throw EncoderError("JxlEncoderSetFrameLossless");
        }
        else
        {
            if (NativeMethods.JxlEncoderSetFrameDistance(frameSettings, options.Distance) != JxlEncoderStatus.Success)
                throw EncoderError("JxlEncoderSetFrameDistance");
        }

        if (NativeMethods.JxlEncoderFrameSettingsSetOption(frameSettings, JxlEncoderFrameSettingId.Effort, options.Effort)
            != JxlEncoderStatus.Success)
        {
            throw EncoderError("JxlEncoderFrameSettingsSetOption(Effort)");
        }

        var pixelFormat = new JxlPixelFormat
        {
            NumChannels = 4,
            DataType = JxlDataType.Uint8,
            Endianness = JxlEndianness.Native,
            Align = 0,
        };

        fixed (byte* pixelsPtr = rgba8)
        {
            if (NativeMethods.JxlEncoderAddImageFrame(
                    frameSettings, pixelFormat, pixelsPtr, (nuint)rgba8.Length)
                != JxlEncoderStatus.Success)
            {
                throw EncoderError("JxlEncoderAddImageFrame");
            }
        }

        NativeMethods.JxlEncoderCloseInput(_encoder);

        var output = new byte[Math.Max(64, (int)Math.Min(expected / 2, int.MaxValue))];
        nuint written = 0;
        while (true)
        {
            fixed (byte* outPtr = output)
            {
                var next = outPtr + written;
                var avail = (nuint)output.Length - written;
                var status = NativeMethods.JxlEncoderProcessOutput(_encoder, &next, &avail);
                written = (nuint)(next - outPtr);
                if (status == JxlEncoderStatus.Success)
                    break;
                if (status == JxlEncoderStatus.Error)
                    throw EncoderError("JxlEncoderProcessOutput");
                if (status == JxlEncoderStatus.NeedMoreOutput)
                {
                    Array.Resize(ref output, output.Length * 2);
                    continue;
                }
                throw new JxlException($"Unexpected encoder status: 0x{(int)status:X}.");
            }
        }

        Array.Resize(ref output, (int)written);
        return output;
    }

    private JxlException EncoderError(string op)
    {
        var err = NativeMethods.JxlEncoderGetError(_encoder);
        return new JxlException($"{op} failed: {err}.");
    }

    public void Dispose()
    {
        _runner.Dispose();
        _encoder.Dispose();
    }
}
