namespace DeAuth.Modules;

public static class Extensions
{

  private static readonly DiscordClient? client = Bot.Bot._client;

  /// <summary>
  ///   Converts discord user to member.
  /// </summary>
  /// <param name="User"></param>
  /// <param name="GuildID"></param>
  /// <returns></returns>
  public static async Task<DiscordMember?> ToMember(this DiscordUser User, ulong GuildID)
  {
    try
    {
      return await client.GetGuildAsync(GuildID).Result.GetMemberAsync(User.Id);
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  ///   Calculates the percentage of suspiciously of member.
  /// </summary>
  /// <param name="Member"></param>
  /// <returns></returns>
  public static bool CalculateSuspiciously(this DiscordMember Member)
  {
    
    int SusCount = 0;
    DiscordUser AsUser = Member;
    
    // No connected accounts


    // check if this member is fake or alt account
    

    if (AsUser is {IsBot: true} or {IsSystem: true})
    {
      return false;
    }

    if (Member?.AvatarUrl == null || string.IsNullOrEmpty(Member?.GetAvatarUrl(ImageFormat.Auto))) // The avatar url is null
    {
      SusCount += 25;
    }

    if (AsUser.Presence.Status == DSharpPlus.Entities.UserStatus.Online) // New accounts is created with online status.
    {
      SusCount += 25;
    }

    if (AsUser.Flags == UserFlags.None) // New accs cannot has flags.
    {
      SusCount += 25;
    }

    return SusCount > 50;
  }

  /// <summary>
  ///   Converts DateTime to logical time string.
  /// </summary>
  /// <param name="dt"></param>
  /// <returns></returns>
  public static string ToLogicalString(this DateTime dt)
  {
    // convert datetime to timespan
    TimeSpan span = DateTime.Now - dt;

    switch ( span.Days )
    {
      case > 365:
      {
        int years = span.Days / 365;

        if (span.Days % 365 != 0)
        {
          years += 1;
        }

        return $"{years} {(years == 1 ? "Year" : "Years")}";
      }

      case > 30:
      {
        int months = span.Days / 30;

        if (span.Days % 31 != 0)
        {
          months += 1;
        }

        return $"{months} {(months == 1 ? "Month" : "Months")}";
      }

      case > 0:
        return $"{span.Days} {(span.Days == 1 ? "Day" : "Days")}";
    }

    if (span.Hours > 0)
    {
      return $"{span.Hours} {(span.Hours == 1 ? "Hour" : "Hours")}";
    }

    if (span.Minutes > 0)
    {
      return $"{span.Minutes} {(span.Minutes == 1 ? "Minute" : "Minutes")}";
    }

    if (span.Seconds > 3)
    {
      return $"{span.Seconds} Seconds";
    }

    return span.Seconds <= 3 ? "Instantly" : string.Empty;
  }

  /// <summary>
  ///   Converts TimeSpan to logical time string.
  /// </summary>
  /// <param name="dt"></param>
  /// <returns></returns>
  public static string ToLogicalString(this TimeSpan dt)
  {
    TimeSpan span = dt;

    switch ( span.Days )
    {
      case > 365:
      {
        int years = span.Days / 365;

        if (span.Days % 365 != 0)
        {
          years += 1;
        }

        return $"{years} {(years == 1 ? "Year" : "Years")}";
      }

      case > 30:
      {
        int months = span.Days / 30;

        if (span.Days % 31 != 0)
        {
          months += 1;
        }

        return $"{months} {(months == 1 ? "Month" : "Months")}";
      }

      case > 0:
        return $"{span.Days} {(span.Days == 1 ? "Day" : "Days")}";
    }

    if (span.Hours > 0)
    {
      return $"{span.Hours} {(span.Hours == 1 ? "Hour" : "Hours")}";
    }

    if (span.Minutes > 0)
    {
      return $"{span.Minutes} {(span.Minutes == 1 ? "Minute" : "Minutes")}";
    }

    if (span.Seconds > 5)
    {
      return $"{span.Seconds} Seconds";
    }

    return span.Seconds <= 5 ? "Instantly" : string.Empty;
  }

  /// <summary>
  ///   Replaces existing config with new one.
  /// </summary>
  /// <param name="opt"></param>
  /// <param name="new"></param>
  public static void ReplaceWith(this Config opt, Config @new)
  {
    int index = Consts.Config.IndexOf(opt);
    Consts.Config[index] = @new;
  }

  /// <summary>
  ///   Returns a default Quarantine role of guild.
  /// </summary>
  public static DiscordRole? GetQuarantineRole(this Config opt, bool Create = true)
  {
    #region Delete Overrides

    DiscordGuild guild = client.GetGuildAsync(opt.GuildID).Result;

    (_, DiscordRole? value) = guild.Roles.FirstOrDefault(x => x.Value.Name == Consts.QUARANTINE_ROLE_NAME);

    DiscordRole? ReturnRole = value?.Name is null
        ? Create // If create is selected, create new role. Otherwise, return null.
            ? guild.CreateRoleAsync(Consts.QUARANTINE_ROLE_NAME, Consts.QuarantineRolePerm, DiscordColor.DarkRed).Result
            : value
        : value;

    opt.RoleID = ReturnRole?.Id ?? 0;
    return ReturnRole;

    #endregion
  }

  /// <summary>
  ///   Reloads a configuration of specific guild. Deletes all properties that related with this guild and creates a new
  ///   config for guild.
  /// </summary>
  public static void Reload(this Config opt)
  {
    Consts.Config.Remove(Consts.Config.First(x => x.GuildID == opt.GuildID));

    Consts.Config.Add(new Config
    {
        GuildID = opt.GuildID
    });
  }

  /// <summary>
  ///   Edits the user verification status to specific status.
  /// </summary>
  /// <param name="State">Status to edit.</param>
  public static void EditStatus(this Config opt, ulong UserID, Status State)
  {
    // if user is not in the list, add it.
    if (opt.Attempts.FirstOrDefault(x => x.UserID == UserID) is null)
    {
      opt.Attempts.Add(new UserStatus
      {
          UserID = UserID,
          Status = Status.UnVerified,
          Time = DateTime.Now
      });
    }

    UserStatus userStatus = opt.Attempts.First(user => user.UserID == UserID);
    userStatus.Status = State;
    userStatus.Time = DateTime.Now;
  }

  /// <summary>
  ///   Verifies the user and make able to access all channels.
  /// </summary>
  public static async Task Verify(this Config opt, DiscordInteraction e)
  {
    await e.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
        new DiscordInteractionResponseBuilder()
            .AsEphemeral()
            .AddEmbed(
                Builders.BuildEmbed(e.User,
                    "Verify",
                    $"🔹 You are verified in **{e.Guild.Name}**.",
                    DiscordColor.NotQuiteBlack)));

    // await e.Guild.GetMemberAsync(e.User.Id).Result.ReplaceRolesAsync(new[] {e.Guild.EveryoneRole});
    await e.Guild.GetMemberAsync(e.User.Id).Result.RevokeRoleAsync(Utils.GetConfig(e.Guild).GetQuarantineRole());
    opt.EditStatus(e.User.Id, Status.Verified);
    Utils.Log(e.Guild, await e.Guild.GetMemberAsync(e.User.Id), LogType.Verify);
  }

}