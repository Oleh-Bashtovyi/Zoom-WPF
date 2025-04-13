using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zoom_UI.MVVM.Models.Frames;

public class FileFrame
{
    public string FileId { get; set; }
    public byte[] Data { get; set; }


    public FileFrame(string fileId, byte[] date)
    {
        FileId = fileId;
        Data = date;
    }
}
