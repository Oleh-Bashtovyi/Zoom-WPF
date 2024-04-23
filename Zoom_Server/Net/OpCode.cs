namespace Zoom_Server.Net;

public enum OpCode
{
    //General
    //=============================
    None,
    Success,
    Error,
    //Specific
    //=============================
    GetId,
    ChangeName,
    //meeting joining
    //=============================
    ParticipantJoinUsingCode,
    ParticipantCreatesMeeting,
    //meeting process
    //=============================
    ParticipantJoininMeeting,
    ParticipantLeftMeeting,
    PatricipantCameraFrameSent,
    ParticipantSpeek,
    //messages
    //=============================
    ParticipantMessageSentToEveryone,
    ParticipantMessageSentPrivate,
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