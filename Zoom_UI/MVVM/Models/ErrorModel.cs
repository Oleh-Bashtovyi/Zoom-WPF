using Zoom_Server.Net.Codes;

namespace Zoom_UI.MVVM.Models;

public class ErrorModel
{
    public ErrorCode ErrorCode { get; set; }
    public string? Message { get; set; }

    public ErrorModel(ErrorCode errorCode, string? message)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}
