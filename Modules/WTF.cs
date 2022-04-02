namespace DeAuth.Modules;

public static class WTF
{

  /// <summary>
  ///   Calculates the percentage of suspiciously of member.
  /// </summary>
  /// <param name="Member"></param>
  /// <returns></returns>
  public static bool IsSuspicious(this DiscordMember Member)
  {
    var SusCount = 0;
    DiscordUser AsUser = Member;

    if (AsUser.IsBot || AsUser?.IsSystem == true)
    {
      return false;
    }

    if (Member?.AvatarUrl == null) // The avatar url is null
    {
      SusCount += 25;
    }

    if (AsUser != null && AsUser.Presence.Status == DSharpPlus.Entities.UserStatus.Online) // New accounts is created with online status.
    {
      SusCount += 25;
    }

    if (AsUser != null && AsUser.Flags == UserFlags.None) // New accs cannot has flags.
    {
      SusCount += 25;
    }

    return SusCount > 50;
  }

}