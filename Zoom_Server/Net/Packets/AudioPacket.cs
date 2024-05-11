namespace Zoom_Server.Net.Packets;

public class AudioPacket : IPacket
{
    public int UserId { get; }
    public byte[] Data { get; }


    public AudioPacket(int userId, byte[] data)
    {
        UserId = userId;
        Data = data;
    }

    public AudioPacket(BinaryReader reader)
    {
        UserId = reader.ReadInt32();
        var dataLength = reader.ReadInt32();
        Data = reader.ReadBytes(dataLength);
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(UserId);
        writer.Write(Data.Length);
        writer.Write(Data);
    }
}
