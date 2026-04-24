using Microsoft.Win32.SafeHandles;

namespace JxlDotNet.Interop;

internal sealed class JxlDecoderSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public JxlDecoderSafeHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        NativeMethods.JxlDecoderDestroy(handle);
        return true;
    }
}
