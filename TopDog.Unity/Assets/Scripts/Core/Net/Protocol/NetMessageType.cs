namespace TopDog.Net.Protocol;

public enum NetMessageType
{
    HELLO,
    HELLO_ACK,
    ROOM_LIST,
    JOIN_REQUEST,
    JOIN_ACCEPT,
    COMMAND_SUBMIT,
    STATE_DELTA,
    PHASE_SYNC,
    TACTICAL_INPUT,
    MATCH_PAUSE,
    MATCH_RESUME,
    DISCONNECT,
}
