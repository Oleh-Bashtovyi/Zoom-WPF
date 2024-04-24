using Zoom_Server.Net;

namespace Zoom_Server.Extensions;

public static class OpCodeExtensions
{
    public static byte AsByte(this OpCode code)
    {
        return (byte)code;
    }
}
