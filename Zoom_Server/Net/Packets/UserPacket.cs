namespace Zoom_Server.Net.Packets;

internal class UserPacket : IPacket
{
    public int Id { get; }
    public string Username { get; }

    public UserPacket(int userId, string username)
    {
        Id = userId;
        Username = username;
    }

    public UserPacket(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Username = reader.ReadString();
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Username);
    }

    public static UserPacket ReadPacket(BinaryReader reader)
    {
        var userId = reader.ReadInt32();
        var username = reader.ReadString();
        return new UserPacket(userId, username);
    }
}
