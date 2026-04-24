using System.Runtime.InteropServices;

namespace JxlDotNet.Interop;

internal static partial class NativeMethods
{
    private const string Jxl = "jxl";
    private const string JxlThreads = "jxl_threads";

    // ---------- decoder ----------

    [LibraryImport(Jxl)]
    internal static partial JxlSignature JxlSignatureCheck(
        ref byte buf, nuint len);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderSafeHandle JxlDecoderCreate(nint memoryManager);

    [LibraryImport(Jxl)]
    internal static partial void JxlDecoderDestroy(nint dec);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderSubscribeEvents(
        JxlDecoderSafeHandle dec, int eventsWanted);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderSetParallelRunner(
        JxlDecoderSafeHandle dec, nint parallelRunner, nint runnerOpaque);

    [LibraryImport(Jxl)]
    internal static unsafe partial JxlDecoderStatus JxlDecoderSetInput(
        JxlDecoderSafeHandle dec, byte* data, nuint size);

    [LibraryImport(Jxl)]
    internal static partial void JxlDecoderCloseInput(JxlDecoderSafeHandle dec);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderProcessInput(
        JxlDecoderSafeHandle dec);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderGetBasicInfo(
        JxlDecoderSafeHandle dec, out JxlBasicInfo info);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderImageOutBufferSize(
        JxlDecoderSafeHandle dec, in JxlPixelFormat format, out nuint size);

    [LibraryImport(Jxl)]
    internal static unsafe partial JxlDecoderStatus JxlDecoderSetImageOutBuffer(
        JxlDecoderSafeHandle dec, in JxlPixelFormat format, byte* buffer, nuint size);

    [LibraryImport(Jxl)]
    internal static partial JxlDecoderStatus JxlDecoderGetICCProfileSize(
        JxlDecoderSafeHandle dec, int target, out nuint size);

    [LibraryImport(Jxl)]
    internal static unsafe partial JxlDecoderStatus JxlDecoderGetColorAsICCProfile(
        JxlDecoderSafeHandle dec, int target, byte* iccProfile, nuint size);

    // ---------- encoder ----------

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderSafeHandle JxlEncoderCreate(nint memoryManager);

    [LibraryImport(Jxl)]
    internal static partial void JxlEncoderDestroy(nint enc);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderError JxlEncoderGetError(JxlEncoderSafeHandle enc);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderSetParallelRunner(
        JxlEncoderSafeHandle enc, nint parallelRunner, nint runnerOpaque);

    [LibraryImport(Jxl)]
    internal static partial void JxlEncoderInitBasicInfo(out JxlBasicInfo info);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderSetBasicInfo(
        JxlEncoderSafeHandle enc, in JxlBasicInfo info);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderSetColorEncoding(
        JxlEncoderSafeHandle enc, in JxlColorEncoding colorEncoding);

    [LibraryImport(Jxl)]
    internal static partial void JxlColorEncodingSetToSRGB(
        out JxlColorEncoding colorEncoding,
        [MarshalAs(UnmanagedType.I4)] bool isGray);

    [LibraryImport(Jxl)]
    internal static partial nint JxlEncoderFrameSettingsCreate(
        JxlEncoderSafeHandle enc, nint source);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderSetFrameDistance(
        nint frameSettings, float distance);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderSetFrameLossless(
        nint frameSettings, [MarshalAs(UnmanagedType.I4)] bool lossless);

    [LibraryImport(Jxl)]
    internal static partial JxlEncoderStatus JxlEncoderFrameSettingsSetOption(
        nint frameSettings, JxlEncoderFrameSettingId option, long value);

    [LibraryImport(Jxl)]
    internal static unsafe partial JxlEncoderStatus JxlEncoderAddImageFrame(
        nint frameSettings, in JxlPixelFormat pixelFormat, byte* buffer, nuint size);

    [LibraryImport(Jxl)]
    internal static partial void JxlEncoderCloseInput(JxlEncoderSafeHandle enc);

    [LibraryImport(Jxl)]
    internal static unsafe partial JxlEncoderStatus JxlEncoderProcessOutput(
        JxlEncoderSafeHandle enc, byte** nextOut, nuint* availOut);

    // ---------- threads ----------

    [LibraryImport(JxlThreads)]
    internal static partial JxlRunnerSafeHandle JxlResizableParallelRunnerCreate(nint memoryManager);

    [LibraryImport(JxlThreads)]
    internal static partial void JxlResizableParallelRunnerDestroy(nint runnerOpaque);

    [LibraryImport(JxlThreads)]
    internal static partial void JxlResizableParallelRunnerSetThreads(
        JxlRunnerSafeHandle runnerOpaque, nuint numThreads);

    [LibraryImport(JxlThreads)]
    internal static partial uint JxlResizableParallelRunnerSuggestThreads(
        ulong xsize, ulong ysize);

    private static nint _resizableRunnerPtr;

    internal static nint GetResizableRunnerFunctionPointer()
    {
        var cached = _resizableRunnerPtr;
        if (cached != 0) return cached;
        var lib = NativeLibrary.Load(JxlThreads, typeof(NativeMethods).Assembly, searchPath: null);
        var ptr = NativeLibrary.GetExport(lib, "JxlResizableParallelRunner");
        Interlocked.CompareExchange(ref _resizableRunnerPtr, ptr, 0);
        return _resizableRunnerPtr;
    }
}
