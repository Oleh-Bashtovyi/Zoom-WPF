namespace Zoom_Server.Net.Packets;

public interface IPacket
{
    public void WriteToStream(BinaryWriter writer);
}
