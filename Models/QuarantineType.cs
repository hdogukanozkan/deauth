namespace DeAuth.Models;

public enum QuarantineType
{

  [ChoiceName("None | Disable Quarantine")]
  None = 0,
  [ChoiceName("Only Bots")]           OnlyBots  = 1,
  [ChoiceName("Only Users")]          OnlyUsers = 2,
  [ChoiceName("Both | Bots & Users")] Both      = 3

}