namespace DeAuth.Models;

public enum LogType
{

  Join,         // a new member has joined
  Verify,       // new verify action performed
  VFail,        // verify failed
  BotJoined,    // bot joined
  RaidDetected, // raid detected
  CDIS,         // country-disallowing module triggered
  AgeLimit      // age limit 

}