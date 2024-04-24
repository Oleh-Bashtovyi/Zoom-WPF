namespace Zoom_Server.Net;

internal static class IdGenerator
{
    private static int currentId = 1000;

    internal static int NewId()
    {
        return ++currentId;
    }
}
