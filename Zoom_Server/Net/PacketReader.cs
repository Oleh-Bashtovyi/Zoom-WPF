using System.Net.Sockets;
using System.Text;

namespace ChatServer.Net.IO;

public class PacketReader : BinaryReader
{
    private NetworkStream _ns;
    public PacketReader(NetworkStream ns) : base(ns)
    {
        _ns = ns;
    }

    public byte[] ReadArray()
    {
        var length = ReadInt32();
        var msgBuffer = new byte[length];
        _ns.Read(msgBuffer, 0, length);
        return msgBuffer;
    }

    public string ReadMessage() => Encoding.UTF8.GetString(ReadArray());
}
