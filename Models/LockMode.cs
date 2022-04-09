namespace DeAuth.Models;

public enum LockMode
{

  [ChoiceName("Kick the new members instantly.")]
  Kick,

  [ChoiceName("Ban the new members instantly.")]
  Ban,

  [ChoiceName("Just display a locked message.")]
  ShowMessage,

}