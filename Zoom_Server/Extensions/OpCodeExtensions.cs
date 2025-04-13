﻿using Zoom_Server.Net.Codes;

namespace Zoom_Server.Extensions;

public static class OpCodeExtensions
{
    public static byte[] AsArray(this OpCode code)
    {
        return [(byte)code];
    }
}
