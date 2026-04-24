using Microsoft.Win32.SafeHandles;

namespace JxlDotNet.Interop;

internal sealed class JxlRunnerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public JxlRunnerSafeHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        NativeMethods.JxlResizableParallelRunnerDestroy(handle);
        return true;
    }
}
