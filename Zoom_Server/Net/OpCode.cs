namespace Zoom_Server.Net;

public enum OpCode : byte
{
    //General
    //=============================
    NONE,
    ERROR,
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
    PARTICIPANT_CAMERA_FRAME_CREATE,
    PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE,
    //messages
    //=============================
    PARTICIPANT_MESSAGE_SENT_EVERYONE,
    PARTICIPANT_MESSAGE_SENT_PRIVATE,
}
//=====================================================
//CONNECTION TO SERVER:
//client:
//--connect to server on button click.
//server:
//--create uid
//--add user to collection

//=====================================================
//GET UID:
//client:
//--op_code "get_id"
//server:
//--op_code "get_id"
//--uid

//=====================================================
//CHANGE NAME
//client:
//--op_code "change_name"
//--new_name
//server:
//--op_code "change_name"
//--new_name


//=====================================================
//CREATE MEETING:
//clinet:  
//--op_code "new_meeting"
//server:
//--create meeting
//--add user to meeting
//--send op_code "new_meeting"

//=====================================================
//JOIN MEETING VIA CODE:
//clinet:  
//--op_code "join_meeting"
//--meeting_code