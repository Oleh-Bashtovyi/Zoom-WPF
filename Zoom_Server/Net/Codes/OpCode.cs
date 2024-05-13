namespace Zoom_Server.Net.Codes;

public enum OpCode : byte
{
    //General
    //=============================
    NONE,
    ERROR,
    SUCCESS,
    QUESTION_CHECKOUT,
    ACTIVITY_TIMEOUT_CHECKOUT,
    //Creation
    //=============================
    GET_USER_ID,
    CHANGE_USER_NAME,
    CREATE_USER,
    CREATE_MEETING,
    //meeting joining
    //=============================
    PARTICIPANT_USES_CODE_TO_JOIN_MEETING,
    //meeting process
    //=============================
    PARTICIPANT_JOINED_MEETING,
    PARTICIPANT_LEFT_MEETING,
    //camera frame
    //=============================
    PARTICIPANT_TURNED_CAMERA_ON,
    PARTICIPANT_TURNED_CAMERA_OFF,
    PARTICIPANT_CAMERA_FRAME_CREATE,
    PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE,
    //screen capture frame
    //=============================
    PARTICIPANT_TURNED_SCREEN_CAPTURE_ON,
    PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF,
    PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME,
    PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME,
    //audio
    //=============================
    PARTICIPANT_SENT_AUDIO,
    PARTICIPANT_TURNED_MICROPHONE_ON,
    PARTICIPANT_TURNED_MICROPHONE_OFF,
    //messages
    //=============================
    PARTICIPANT_MESSAGE_SEND_EVERYONE,
    PARTICIPANT_MESSAGE_SEND,
    //files
    //=============================
    PARTICIPANT_FILE_SEND_REQUEST_EVERYONE,
    PARTICIPANT_FILE_SEND_REQUEST,
    PARTICIPANT_FILE_SEND,

    PARTICIPANT_FILE_SEND_FRAME_UPDATE,
    PARTICIPANT_FILE_DOWNLOAD_START,
    PARTICIPANT_FILE_DOWNLOAD_FRAME,
}