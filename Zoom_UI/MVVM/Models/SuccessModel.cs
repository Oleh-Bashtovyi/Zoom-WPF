using Zoom_Server.Net;

namespace Zoom_UI.MVVM.Models
{
    public class SuccessModel
    {
        public ScsCode SuccessCode { get; set; }
        public string? Message { get; set; }

        public SuccessModel(ScsCode errorCode, string? message)
        {
            SuccessCode = errorCode;
            Message = message;
        }
    }
}
