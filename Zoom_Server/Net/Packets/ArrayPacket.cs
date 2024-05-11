
namespace Zoom_Server.Net.Packets;

public class ArrayPacket : IPacket
{
    private byte[] _data;

    public ArrayPacket(byte[] data)
    {
        if(data == null)
        {
            data = [];
        }

        _data = data;
    }

    public byte[] ToArray()
    {
        return _data;
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(_data.Length);
        writer.Write(_data);
    }
}
