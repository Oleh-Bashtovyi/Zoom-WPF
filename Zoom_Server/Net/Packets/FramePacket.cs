namespace Zoom_Server.Net.Packets;

public class FramePacket : IPacket
{
    public int UserId { get; }
    public int FramePosition { get; }
    public byte[] FrameData { get; }

    public FramePacket(int userId, int framePosition, byte[] frameData)
    {
        UserId = userId;
        FramePosition = framePosition;
        FrameData = frameData;
    }

    public FramePacket(BinaryReader reader)
    {
        UserId = reader.ReadInt32();
        FramePosition = reader.ReadInt32();
        var dataLength = reader.ReadInt32();
        FrameData = reader.ReadBytes(dataLength);
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(UserId);
        writer.Write(FramePosition);
        writer.Write(FrameData.Length);
        writer.Write(FrameData);
    }

    public static byte[] CreatePacket(int userId, int framePosition, byte[] frameData)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(userId);
        writer.Write(framePosition);
        writer.Write(frameData.Length);
        writer.Write(frameData);
        return stream.ToArray();
    }

    public static FramePacket ReadPacket(BinaryReader reader)
    {
        return new FramePacket(reader);
    }
}
