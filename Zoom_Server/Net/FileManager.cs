namespace Zoom_Server.Net;

internal static class FileManager
{
    private static string _tempFolder = @".\.temp";

    static FileManager()
    {
        if (!Directory.Exists(_tempFolder))
        {
            Directory.CreateDirectory(_tempFolder);
        }
    }

    public static void ClearTempFolder()
    {
        var dirs = Directory.GetDirectories(_tempFolder);

        foreach (var dir in dirs)
        {
            var files = Directory.GetFiles(dir);

            foreach (var file in files)
            {
                File.Delete(file);
            }

            Directory.Delete(dir);
        }
    }

    public static void CreateMeetingCatalog(long meetingId)
    {
        Directory.CreateDirectory(_tempFolder + "\\" + meetingId);
    }

    public static void DeleteMeetingCatalog(long meetingId)
    {
        var files = Directory.GetFiles(_tempFolder + "\\" + meetingId);

        foreach (var file in files)
        {
            File.Delete(file);
        }

        Directory.Delete(_tempFolder + "\\" + meetingId);
    }

    public static void WriteDataToFile(byte[] data, long cursorPosition, long meetingId, string fileId)
    {
        var file = File.OpenWrite(_tempFolder + "\\" + meetingId + "\\" + fileId);
        file.Seek(cursorPosition, SeekOrigin.Begin);
        file.Write(data, 0, data.Length);
        file.Flush();
        file.Close();
    }

    public static void DeleteFile(long meetingId, string fileId)
    {
        var path = _tempFolder + "\\" + meetingId + "\\" + fileId;

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static long GetFileSize(long meetingId, string fileId)
    {
        using (var file = File.OpenRead(_tempFolder + "\\" + meetingId + "\\" + fileId))
        {
            return file.Length;
        };
    }

    public static (byte[], int) GetFileData(long meetingId, string fileId, long cursor)
    {
        using (var file = File.OpenRead(_tempFolder + "\\" + meetingId + "\\" + fileId))
        {
            file.Seek(cursor, SeekOrigin.Begin);

            var data = new byte[32768];
            var count = file.Read(data, 0, data.Length);

            return (data, count);
        }
    }
}