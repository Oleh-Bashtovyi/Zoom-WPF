using Zoom_Server.Extensions;
using Zoom_Server.Net.Codes;
namespace Zoom_Server.Net;

public class PacketBuilder : BinaryWriter
{
    private MemoryStream _stream;

    public PacketBuilder() : base(new MemoryStream())
    {
        _stream = (MemoryStream)BaseStream;
    }

    public void Write(OpCode opCode)
    {
        _stream.WriteByte(opCode.AsByte());
    }


    public void Write_UserInfo(int userId, string username)
    {
        Write(userId);
        Write(username);
    }

    public void Clear()
    {
        _stream.Clear();
    }


    public byte[] ToArray()
    {
        return _stream.ToArray();
    }
}
