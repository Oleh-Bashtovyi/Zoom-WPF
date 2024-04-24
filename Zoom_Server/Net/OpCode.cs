namespace Zoom_Server.Net;

public enum OpCode
{
    //General
    //=============================
    None,
    Success,
    Error,
    //Creation
    //=============================
    GetId,
    ChangeName,
    CreateUser,
    CreateMeeting,
    //meeting joining
    //=============================
    Participant_JoinMeetingUsingCode,
    //meeting process
    //=============================
    Participant_JoinedMeeting,
    Participant_LeftMeeting,
    //camera frame
    //=============================
    Participant_CameraFrame_Create,
    Participant_CameraFrame_Update,
    //speek
    //=============================
    Participant_SpeekFrame_Create,
    Participant_SpeekFrame_Update,
    //messages
    //=============================
    Participant_MessageSent_ToEveryone,
    Participant_MessageSent_Private,
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