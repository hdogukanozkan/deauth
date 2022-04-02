namespace DeAuth.Models;

public enum Status
{

  [ChoiceName("Verified")] Verified, [ChoiceName("Not Verified")] UnVerified, [ChoiceName("Quarantined")] Kicked // kicked, banned etc.

}