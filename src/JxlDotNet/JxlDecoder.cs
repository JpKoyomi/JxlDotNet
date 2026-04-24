using JxlDotNet.Interop;

namespace JxlDotNet;

public sealed class JxlDecoder : IDisposable
{
    private const int JxlColorProfileTargetData = 1;

    private readonly JxlDecoderSafeHandle _decoder;
    private readonly JxlRunnerSafeHandle _runner;

    public JxlDecoder()
    {
        _decoder = NativeMethods.JxlDecoderCreate(0);
        if (_decoder.IsInvalid)
            throw new JxlException("JxlDecoderCreate returned null.");

        _runner = NativeMethods.JxlResizableParallelRunnerCreate(0);
        if (_runner.IsInvalid)
        {
            _decoder.Dispose();
            throw new JxlException("JxlResizableParallelRunnerCreate returned null.");
        }
    }

    public static DecodedImage Decode(ReadOnlySpan<byte> input)
    {
        using var decoder = new JxlDecoder();
        return decoder.DecodeOneShot(input);
    }

    public unsafe DecodedImage DecodeOneShot(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty)
            throw new JxlException("Input data is empty.");

        const int eventsWanted =
            (int)JxlDecoderStatus.BasicInfo |
            (int)JxlDecoderStatus.ColorEncoding |
            (int)JxlDecoderStatus.FullImage;

        if (NativeMethods.JxlDecoderSubscribeEvents(_decoder, eventsWanted) != JxlDecoderStatus.Success)
            throw new JxlException("JxlDecoderSubscribeEvents failed.");

        var runnerFunc = NativeMethods.GetResizableRunnerFunctionPointer();
        if (NativeMethods.JxlDecoderSetParallelRunner(_decoder, runnerFunc, _runner.DangerousGetHandle())
            != JxlDecoderStatus.Success)
        {
            throw new JxlException("JxlDecoderSetParallelRunner failed.");
        }

        JxlBasicInfo info = default;
        byte[]? pixels = null;
        byte[]? icc = null;
        var width = 0;
        var height = 0;

        var pixelFormat = new JxlPixelFormat
        {
            NumChannels = 4,
            DataType = JxlDataType.Uint8,
            Endianness = JxlEndianness.Native,
            Align = 0,
        };

        fixed (byte* inputPtr = input)
        {
            if (NativeMethods.JxlDecoderSetInput(_decoder, inputPtr, (nuint)input.Length)
                != JxlDecoderStatus.Success)
            {
                throw new JxlException("JxlDecoderSetInput failed.");
            }
            NativeMethods.JxlDecoderCloseInput(_decoder);

            while (true)
            {
                var status = NativeMethods.JxlDecoderProcessInput(_decoder);
                switch (status)
                {
                    case JxlDecoderStatus.Error:
                        throw new JxlException("Decoder reported an error while processing input.");
                    case JxlDecoderStatus.NeedMoreInput:
                        throw new JxlException("Decoder unexpectedly requested more input (truncated data?).");
                    case JxlDecoderStatus.BasicInfo:
                        if (NativeMethods.JxlDecoderGetBasicInfo(_decoder, out info) != JxlDecoderStatus.Success)
                            throw new JxlException("JxlDecoderGetBasicInfo failed.");
                        width = checked((int)info.Xsize);
                        height = checked((int)info.Ysize);
                        if (width <= 0 || height <= 0)
                            throw new JxlException($"Invalid image dimensions: {width}x{height}.");
                        NativeMethods.JxlResizableParallelRunnerSetThreads(
                            _runner,
                            NativeMethods.JxlResizableParallelRunnerSuggestThreads(info.Xsize, info.Ysize));
                        break;
                    case JxlDecoderStatus.ColorEncoding:
                        if (NativeMethods.JxlDecoderGetICCProfileSize(
                                _decoder, JxlColorProfileTargetData, out var iccSize)
                            == JxlDecoderStatus.Success && iccSize > 0)
                        {
                            icc = new byte[iccSize];
                            fixed (byte* iccPtr = icc)
                            {
                                if (NativeMethods.JxlDecoderGetColorAsICCProfile(
                                        _decoder, JxlColorProfileTargetData, iccPtr, iccSize)
                                    != JxlDecoderStatus.Success)
                                {
                                    icc = null;
                                }
                            }
                        }
                        break;
                    case JxlDecoderStatus.NeedImageOutBuffer:
                        if (NativeMethods.JxlDecoderImageOutBufferSize(_decoder, pixelFormat, out var outSize)
                            != JxlDecoderStatus.Success)
                        {
                            throw new JxlException("JxlDecoderImageOutBufferSize failed.");
                        }
                        var expected = (nuint)width * (nuint)height * 4;
                        if (outSize != expected)
                            throw new JxlException($"Unexpected output buffer size: got {outSize}, expected {expected}.");
                        pixels = new byte[outSize];
                        fixed (byte* pixelsPtr = pixels)
                        {
                            if (NativeMethods.JxlDecoderSetImageOutBuffer(
                                    _decoder, pixelFormat, pixelsPtr, outSize)
                                != JxlDecoderStatus.Success)
                            {
                                throw new JxlException("JxlDecoderSetImageOutBuffer failed.");
                            }
                        }
                        break;
                    case JxlDecoderStatus.FullImage:
                        // keep processing — next call should return SUCCESS
                        break;
                    case JxlDecoderStatus.Success:
                        if (pixels is null)
                            throw new JxlException("Decoder finished without producing pixel data.");
                        return new DecodedImage(width, height, pixels, icc ?? []);
                    default:
                        throw new JxlException($"Unexpected decoder status: 0x{(int)status:X}.");
                }
            }
        }
    }

    public void Dispose()
    {
        _runner.Dispose();
        _decoder.Dispose();
    }
}
