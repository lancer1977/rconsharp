namespace RconSharp;

/// <summary>
/// RCON Packet type as defined in <see cref="https://developer.valvesoftware.com/wiki/Source_RCON_Protocol#Requests_and_Responses"/>
/// </summary>
public enum PacketType
{
  // SERVERDATA_RESPONSE_VALUE
  Response = 0,

  // SERVERDATA_AUTH_RESPONSE
  AuthResponse = 2,

  // SERVERDATA_EXECCOMMAND
  ExecCommand = 2,

  // SERVERDATA_AUTH
  Auth = 3
}
