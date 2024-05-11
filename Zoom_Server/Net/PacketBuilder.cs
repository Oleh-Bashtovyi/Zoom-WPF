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

    public void Write_UserFrame(int fromUser_Id, int position, byte[] data)
    {
        Write(fromUser_Id);
        Write(position);
        Write(data.Length);
        Write(data);
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




public class PacketReader : BinaryReader
{
    private MemoryStream _stream;

    public PacketReader(MemoryStream stream) : base(stream)
    {
        _stream = stream;
    }


    public OpCode ReadOpCode()
    {
        return (OpCode)ReadByte();
    }

    public ErrorCode ReadErrorCode()
    {
        return (ErrorCode)ReadByte();
    }




    public record UserInfo(int Id, string Username);
    /// <summary>
    /// Read bytes as: Id-(int) and usernam-(string). 
    /// </summary>
    /// <returns></returns>
    public UserInfo ReadUserInfo()
    {
        var userId = ReadInt32();
        var username = ReadString();
        return new UserInfo(userId, username);  
    } 

    public record UserFrame(int UserId, int Position, byte[] Data);
    /// <summary>
    /// Read bytes as: User_Id-(int), Cluster_Position-(int), Cluster_Size-(int) and Cluster-(bytes)
    /// </summary>
    /// <returns></returns>
    public UserFrame ReadUserFrame()
    {
        var userId = ReadInt32();
        var position = ReadInt32();
        var clusterSize = ReadInt32();
        var cluster = ReadBytes(clusterSize);
        return new UserFrame(userId, position, cluster);
    }
}
