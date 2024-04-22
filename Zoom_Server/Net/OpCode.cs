namespace Zoom_Server.Net;

public enum OpCode
{
    None = 0,
    ParticipantEnterUsingCode = 1,
    ParticipantEnteredMeeting = 2,
    ParticipantLeftMeeting = 3,
    PatricipantFrameSent = 4,
    ParticipantMessageSentToEveryone = 5,
    ParticipantMessageSentPrivate = 6,
    ParticipantSpeek = 7,
}
