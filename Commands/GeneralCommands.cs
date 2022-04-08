namespace DeAuth.Commands;

public class GeneralCommands : ApplicationCommandModule
{

  [SlashCommand("help", "Who am I?")]
  public async Task Help(InteractionContext c)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    var embed = new DiscordEmbedBuilder
    {
        Title = "Help",
        Description = $"**⟩** [DeAuth]({Consts.PAGE}) is a **security/verification** discord bot. Aimed to automate your" +
                      " verification processes and provide a **secure & efficient** way to **verify** your members and keeping out the bot/spam accounts from your server.",
        Color = DiscordColor.VeryDarkGray
    };
    embed.AddField("Simple & Fast", "᲼᲼🔹 Do you even have to set everything up manually? we doing it auto. Creating roles, channel panels has never been easier!");
    embed.AddField("Watch", "᲼᲼🔹 Watch all operations & verification logs/attempts on your server. Because its your server. Isn't it?");
    embed.AddField("Manage", "᲼᲼🔹 Batch operations, freedom of mistakes. Manage your members with built-in presets & easily manage all.");

    embed.AddField("Customize",
        "᲼᲼🔹 Set up everything as you want, we have a lot of options. Customize your bot to your needs. Or even share/take it with your friends!");

    var components = new List<DiscordActionRowComponent>
    {
        new(new List<DiscordLinkButtonComponent>
        {
            new(Consts.PAGE, "Bot Page"),
            new($"{Consts.DOCUMENTATION_GITBOOK}/setup/enabling-verification", "Quick Setup"),
            new(Consts.DOCUMENTATION_GITBOOK, "Full Docs")
        })
    };

    await c.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(components));
  }

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [VerificationDependency("You cannot make cleanup without verification enabled.")]
  [SlashCommand("cleanup", "Prune your members from the server.")]
  public async Task CLUP(InteractionContext c)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    var SelectOptions = new List<DiscordSelectComponentOption>
    {
        new("Force to verify", description: "Quarantine all users and force them to verify itselfs.", value: "q_all",
            emoji: new DiscordComponentEmoji("🌑"))
    };

    var SelectMenu = new DiscordSelectComponent("d1", "Preset to apply.", SelectOptions);

    string? DropdownResult = Builders
                             .WaitDropdown("Cleanup", "🔹 Select a option that will be executed on all members.",
                                 SelectMenu, c)
                             .GetAwaiter().GetResult().Result.Values.First();

    switch ( DropdownResult )
    {
      case null:
        await Builders.Edit(c, "Time Out", "🔸 Operation canceled.");
        break;

      case "q_all":
      {
        var success = 0;
        var fail = 0;
        var already_quarantined = 0;
        await Builders.Edit(c, "Being Quarantine", "🔹 Preparing to quarantine all users. That may take a few seconds.");
        DiscordRole? Qrole = Utils.GetConfig(c.Guild).GetQuarantineRole();

        foreach ( DiscordMember? member in await c.Guild.GetAllMembersAsync() )
        {
          if (member.IsBot) continue;
          if (member.IsOwner) continue;

          try
          {
            if (member.Roles.Any(x => x == Qrole))
            {
              already_quarantined++;
              continue;
            }

            await member.GrantRoleAsync(Qrole);
            success++;
          }
          catch
          {
            fail++;
          }
        }

        await Builders.Edit(c, "Done", $"🔹 All users are quarantened successfully.\n⠀⏣ Success: `{success}`\n️⠀⏣ Fail: `{fail}`\n" +
                                       $"⠀⏣ Already Quarantined: `{already_quarantined}`");
        break;
      }
    }
  }

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [SlashCommand("manage", "Manage the logs and users.")]
  public async Task Manage(InteractionContext c)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config Guild = Utils.GetConfig(c.Guild);
    var Quarantineers = Guild.Attempts;

    if (Quarantineers.Count == 0 || Quarantineers == null)
    {
      throw new DException("Nobody in there", "There are no users to filter. Request **verify** from your members!");
    }

    int total = Quarantineers.Count;
    int total_verified = Quarantineers.Count(x => x.Status == Status.Verified);
    int total_unverified = Quarantineers.Count(x => x.Status == Status.UnVerified);
    int total_kicked = Quarantineers.Count(x => x.Status == Status.Kicked);

    List<DiscordButtonComponent> buttons = new()
    {
        new DiscordButtonComponent(ButtonStyle.Secondary, "v_all", "Verify All",
            Quarantineers.All(x => x.Status != Status.UnVerified)), // If there are any user that unverified. Enable the verify all button.
        new DiscordButtonComponent(ButtonStyle.Primary, "u_all", "Unverify All",
            Quarantineers.All(x => x.Status != Status.Verified)), // If there are any user that verified. Enable the unverify all button.
        new DiscordButtonComponent(ButtonStyle.Danger, "k_all", "Kick UnVerified Users",
            Quarantineers.All(x => x.Status != Status.UnVerified)), // If there are any user that unverified. Enable the kick all button.
        new DiscordButtonComponent(ButtonStyle.Danger, "b_all", "Ban UnVerified Users",
            Quarantineers.All(x => x.Status != Status.UnVerified)), // If there are any user that unverified. Ban the kick all button.
        new DiscordButtonComponent(ButtonStyle.Danger, "c_all", "Clear Logs")
    };

    string? ButtonResult = Builders.WaitButton(c, "Manage",
                                       $"・Total Log *⟩** **{total}**\n" +
                                       $"・Last Verify *⟩** <@{Quarantineers?.Last(x => x.Status == Status.Verified)?.UserID ?? 0}>\n" +
                                       $"・Members *⟩** **{c.Guild.MemberCount}**\n" +
                                       $"・Verified Users *⟩** **{total_verified}**\n" +
                                       $"・Unverified Users *⟩** **{total_unverified}**\n" +
                                       $"・Kicked Users *⟩** **{total_kicked}**\n",
                                       10 * 60 * 1000,
                                       buttons)
                                   .GetAwaiter().GetResult();

    string ActionResult = null;
    var ActPerformed = 0;
    DiscordRole? QRole = Guild.GetQuarantineRole();

    // switch case buttno result
    switch ( ButtonResult )
    {
      case null:
        await Builders.Edit(c, "Canceled", "・You canceled the actions or time out.");
        return;

      case "c_all":
        Guild.Attempts.Clear();
        await Builders.Edit(c, "Cleared", "・All logs are wiped successfully.");
        return;

      case "v_all":
        foreach ( UserStatus Quarantineer in Quarantineers )
        {
          Quarantineer.Status = Status.Verified;
          DiscordMember? User = await c.Guild.GetMemberAsync(Quarantineer.UserID);
          await User.RevokeRoleAsync(QRole);
          ActPerformed++;
        }

        ActionResult = $"All users are verified. `({ActPerformed})`";

        break;

      case "u_all":
        foreach ( UserStatus Quarantineer in Quarantineers )
          try
          {
            Quarantineer.Status = Status.UnVerified;
            DiscordMember? User = await c.Guild.GetMemberAsync(Quarantineer.UserID);
            await User.GrantRoleAsync(QRole);
            ActPerformed++;
          }
          catch
          {
          }

        ActionResult = $"All users are unverified. `({ActPerformed})`";

        break;

      case "k_all":
        foreach ( UserStatus Quarantineer in Quarantineers.Where(x => x.Status == Status.UnVerified) )
          try
          {
            DiscordMember? User = await c.Guild.GetMemberAsync(Quarantineer.UserID);
            await User.RemoveAsync();
            ActPerformed++;
          }
          catch
          {
            // ignored
          }

        ActionResult = $"All unverified users are kicked. `({ActPerformed})`";

        break;

      case "b_all":
        foreach ( UserStatus Quarantineer in Quarantineers.Where(x => x.Status == Status.UnVerified) )
          try
          {
            DiscordMember? User = await c.Guild.GetMemberAsync(Quarantineer.UserID);
            await User.BanAsync();
            ActPerformed++;
          }
          catch
          {
            // ignored
          }

        ActionResult = $"All unverified users are banned. `({ActPerformed})`";

        break;
    }

    await Builders.Edit(c, "Done!", $"🔹 {ActionResult}");
  }

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [SlashCommand("logs", "Show the full list of verify logs.")]
  public async Task ShowLogs(InteractionContext c)
  {
    // await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
    //     new DiscordInteractionResponseBuilder().AsEphemeral());
    // Pagination interactivity response will auto create response already

    Config Guild = Utils.GetConfig(c.Guild);
    var Quarantineers = Guild.Attempts;

    if (Quarantineers.Count == 0)
    {
      throw new DException("Nobody in there", "There are no users to filter. Request **verify** from your members!");
    }

    StringBuilder sb = new();

    foreach ( UserStatus Quarantineer in Quarantineers )
      sb.AppendLine($"<@{Quarantineer.UserID}> | `{Quarantineer.Status.ToString()}` `({Quarantineer.Time.ToString("dd/MM/yyyy HH:mm:ss")})`");

    InteractivityExtension? interactivity = c.Client.GetInteractivity();

    var Pages = interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line,
        new DiscordEmbedBuilder
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
                {Name = c.User.Username, IconUrl = c.User?.AvatarUrl},
            Title = "Verify Attempts",
            Description = "・All `verify` attempts are listed here.",
            Color = DiscordColor.Green,
            Timestamp = DateTimeOffset.Now
        });

    await interactivity.SendPaginatedResponseAsync(c.Interaction, true, c.User, Pages);
  }

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [SlashCommand("whois", "Show the information of user.")]
  public async Task Whois
  (
      InteractionContext c,
      [Option("member", "The member to process.")]
      DiscordUser member)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    DiscordMember? User;

    try
    {
      User = await c.Guild.GetMemberAsync(member.Id);
    }
    catch
    {
      throw new DException("Even searching the space", "Unable to find this user!");
    }

    Config Config = Utils.GetConfig(c.Guild);

    var eb = new DiscordEmbedBuilder
    {
        Title = "Whois",
        Author = new DiscordEmbedBuilder.EmbedAuthor
            {Name = User.Username, IconUrl = User.AvatarUrl, Url = User.AvatarUrl},
        Color = DiscordColor.Red,
        Description = $"⟩ Member **/** {User.Mention}\n" +
                      $"⟩ Joined **/** `{User.JoinedAt.ToString()}`\n" +
                      $"⟩ Created **/** `{User.CreationTimestamp.ToString()}`\n" +
                      $"⟩ Verify Status **/** `{Config.Attempts.FirstOrDefault(x => x.UserID == User.Id)?.Status.ToString() ?? "Unkown"}`\n\n\n",
        Footer = new DiscordEmbedBuilder.EmbedFooter
            {Text = "Deauth"}
    };

    await Builders.Edit(c, eb.Build());
  }

  #region Ban Commands

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [SlashCommand("ban", "Ban a member.")]
  public async Task Ban
  (
      InteractionContext c,
      [Option("member", "User to ban.")] DiscordUser User,
      [Option("reason", "Why you're banning them?")]
      string? Reason = null,
      [Option("delete", "Delete days of user messages.")]
      double DelDays = 0)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    if (DelDays is < 1 or > 14)
    {
      await Builders.Edit(c, "Ban Days", "🔸 Ban days must be between **1-14**.");
      return;
    }

    try
    {
      await c.Guild.BanMemberAsync(await User.ToMember(c.Guild.Id), (int) DelDays, Reason ?? "No Reason");

      await Builders.Edit(c, "DeAuth Airports", $"🔸 **{User.Mention}** has been banned. **Have a good flight!**\n" +
                                                $"᲼᲼🔸 Reason: {Reason ?? "No Reason"}\n" +
                                                $"᲼᲼🔸 Delete Days: {DelDays}");
    }
    catch
    {
      await Builders.Edit(c, "Failed", "🔸 Failed to ban member. The ID may wrong.");
    }
  }

  [SlashRequireUserPermissions(Permissions.Administrator)]
  [SlashCommand("unban", "UnBan a user.")]
  public async Task UnBan
  (
      InteractionContext c,
      [Option("id", "User ID to unban.")] SnowflakeObject ID,
      [Option("reason", "Why you're unbanning them?")]
      string? Reason = null)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    try
    {
      await c.Guild.UnbanMemberAsync(ID.Id, Reason);

      await Builders.Edit(c, "Unban", $"🔸 User {await c.Client.GetUserAsync(ID.Id)} has been unbanned.\n" +
                                      $"᲼᲼🔸 Reason: {Reason ?? "No Reason"}");
    }
    catch
    {
      await Builders.Edit(c, "Uppps!", "🔸 Failed to unban user. Please Check the ID.");
    }
  }

  #endregion

}