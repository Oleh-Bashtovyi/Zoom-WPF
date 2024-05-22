namespace Zoom_Server.Net;

public class FrameBuilder
{
    private byte[][] _frames;
    public bool IsFull => _frames.Length == NumberOfFrames;
    public int NumberOfFrames { get; private set; }

    public FrameBuilder(int numberOfFrames)
    {
        _frames = new byte[numberOfFrames][];
        NumberOfFrames = 0;
    }

    public void AddFrame(int position, byte[]? data)
    {
        if(position < 0 || position >= _frames.Length)
        {
            throw new ArgumentOutOfRangeException("position");
        }
        if (_frames[position] == null && data != null)
        {
            NumberOfFrames++;
        }
        else if (_frames[position] != null && data == null)
        {
            NumberOfFrames--;   
        }
             
        _frames[position] = data;
    }

    public byte[] AsByteArray() => _frames.Where(x => x != null)
                                          .Aggregate((dt1, dt2) => dt1.Concat(dt2)
                                          .ToArray());
}
