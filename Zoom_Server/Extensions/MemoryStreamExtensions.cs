namespace Zoom_Server.Extensions;

public static class MemoryStreamExtensions
{
    public static void Clear(this MemoryStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        stream.SetLength(0);
    }
}
