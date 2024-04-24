using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zoom_Server.Net;

namespace Zoom_Server.Extensions;

public static class OpCodeExtensions
{
    public static byte AsByte(this OpCode code)
    {
        return (byte)code;
    }
}
