using Microsoft.Win32.SafeHandles;

namespace JxlDotNet.Interop;

internal sealed class JxlEncoderSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public JxlEncoderSafeHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        NativeMethods.JxlEncoderDestroy(handle);
        return true;
    }
}
