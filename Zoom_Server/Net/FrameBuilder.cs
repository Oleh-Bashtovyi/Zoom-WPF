namespace Zoom_Server.Net;

public class FrameBuilder
{
    private byte[][] frames;
    public bool IsFull => frames.Length == NumberOfFrames;
    public int NumberOfFrames { get; private set; }



    public FrameBuilder(int numberOfFrames)
    {
        frames = new byte[numberOfFrames][];
        NumberOfFrames = 0;
    }

    public void AddFrame(int position, byte[]? data)
    {
        if(position < 0 || position >= frames.Length)
        {
            throw new ArgumentOutOfRangeException("position");
        }
        if (frames[position] == null && data != null)
        {
            NumberOfFrames++;
        }
        else if (frames[position] != null && data == null)
        {
            NumberOfFrames--;   
        }
             
        frames[position] = data;
    }

    public byte[][] GetFrames() => frames.Where(x => x != null).ToArray();
    public byte[] AsByteArray() => GetFrames().Where(x => x != null).Aggregate((dt1, dt2) => dt1.Concat(dt2).ToArray());
}
