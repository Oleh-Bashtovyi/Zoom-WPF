namespace Zoom_Server.Net.Codes;

public enum ErrorCode : byte
{
    GENERAL,
    MEETING_DOES_NOT_EXISTS,
    USER_DOES_NOT_EXISTS,
    MEETING_IS_FULL,

    SCREEN_CAPTURE_DOES_NOT_ALLOWED
}
