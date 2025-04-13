namespace Zoom_UI.MVVM.Models;

public class FileModel
{
    public string Id {  get; set; }
    public string FileName {  get; set; }
    public long FileSize {  get; set; }

    public FileModel(string id, string fileName, long fileSize)
    {
        Id = id;
        FileName = fileName;
        FileSize = fileSize;
    }
}
