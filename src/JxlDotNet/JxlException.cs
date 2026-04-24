namespace JxlDotNet;

public sealed class JxlException : Exception
{
    public JxlException(string message) : base(message) { }
    public JxlException(string message, Exception inner) : base(message, inner) { }
}
