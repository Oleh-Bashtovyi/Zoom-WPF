public class AudioFrame
{
    public int UserId { get; set; }
    public byte[] Data { get; set; }

    public AudioFrame(int userId, byte[] data)
    {
        UserId = userId;
        Data = data;
    }
}