namespace Zoom_Server.Net;

internal class FileBuilder
{
    public int Id { get; }
    public FrameBuilder FrameBuilder { get; }
    public string FileName { get; }
    public Client Sender { get; }
    public Client? Receiver { get; }
    public bool IsToEveryone => Receiver == null;


    public FileBuilder(FrameBuilder frameBuilder, string fileName, Client sender, Client? receiver = null)
    {
        Id = IdGenerator.NewId();
        FrameBuilder = frameBuilder;
        Sender = sender;
        Receiver = receiver;
        FileName = fileName;
    }
}
