using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Zoom_Server.Net;

namespace ChatServer.Net.IO;

public class PacketBuilder
{
    private MemoryStream _ms;

    public PacketBuilder()
    {
        _ms = new MemoryStream();
    }

    public void WriteOpCode(byte opcode)
    {
        _ms.WriteByte(opcode);
    }

    public void WriteOpCode(OpCode opcode)
    {
        _ms.WriteByte((byte)opcode);
    }


    public void WriteMessage(string msg)
    {
        var msgLength = msg.Length;
        _ms.Write(BitConverter.GetBytes(msgLength));
        _ms.Write(Encoding.UTF8.GetBytes(msg));
    }

    public void WriteArray(byte[] data)
    {
        var arrLength = data.Length;
        _ms.Write(BitConverter.GetBytes(arrLength));
        _ms.Write(data);
    }

    public void WriteBitmap(Bitmap bitmap)
    {
        var curPos = _ms.Position;
        bitmap.Save(_ms, ImageFormat.Jpeg);
        var newPos = _ms.Position;
        var imgLength = newPos - curPos;
        _ms.Seek(newPos, SeekOrigin.Begin);
        _ms.Write(BitConverter.GetBytes(imgLength));
        _ms.Seek(0, SeekOrigin.End);
    }


    public byte[] GetPacketBytes()
    {
        return _ms.ToArray();
    }
}
