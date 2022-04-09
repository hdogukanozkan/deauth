namespace DeAuth.Commands;

[SlashRequireUserPermissions(Permissions.Administrator)]
[VerificationDependency("Extra modules only can be used when verification is enabled.")]
[SlashCommandGroup("module", "Powerfully extra protection modules for your server.")]
public class ModuleCommands : ApplicationCommandModule
{

  [SlashCommand("antiraid", "Protect your server from raids.")]
  public async Task AntiRaid(InteractionContext c, [Option("on", "Whether anti-raid is enabled or not.")] bool Enabled)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);

    if (Enabled)
    {
      await Builders.Edit(c, "Anti Raid", $"🔹 [Anti Raid]({Consts.DOCUMENTATION_GITBOOK + "/more/modules/anti-raid"}) is `turned on`. " +
                                          $"{(!cfg.AgeLimit.HasValue ? "\n\n⠀🔹 Enabling age limit is recommended." : "")}");
    }
    else
    {
      await Builders.Edit(c, "Anti Raid", $"🔸 [Anti Raid]({Consts.DOCUMENTATION_GITBOOK + "/more/modules/anti-raid"}) is `turned off`.");
    }
  }

  [SlashCommand("cdis", "Disallow the joining of users based on country.")]
  public async Task CountryDisallower(InteractionContext c, [Option("on", "Whether country-disallower is enabled or not.")] bool Enabled)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);

    if (c.Interaction?.Locale == null)
    {
      throw new DException("Uhm?", "Failed to determine country of your account.");
    }

    if (!Utils.TryGetCountry(c.Interaction.Locale, out string Country))
    {
      throw new DException("Uhm?", "Failed to determine country of your account.");
    }

    switch ( Enabled )
    {
      case true:
      {
        string? enable = Builders.WaitButton(c, "Country Disallowing // BETA", $"🔹 **DeAuth** detected country as `{Country}`. " +
                                                                               $"Do you want to auto-ban new users that not joining from {Country}?\n\n" +
                                                                               "・**Warning!** || This feature is currently on beta. Country detection mechanism still improving, and it may not work in some cases. Use at your own risk. ||"
            , 20, new[]
            {
                new DiscordButtonComponent(ButtonStyle.Secondary, "disallow_country_true", "", false, new DiscordComponentEmoji("✅")),
                new DiscordButtonComponent(ButtonStyle.Secondary, "disallow_country_false", "", false, new DiscordComponentEmoji("❌"))
            }).GetAwaiter().GetResult();

        if (enable != "disallow_country_true")
        {
          throw new AbortException();
        }

        cfg.Locale = Country;

        await Builders.Edit(c, "Country Disallowing",
            $"🔹 [Country Disallowing]({Consts.DOCUMENTATION_GITBOOK + "/more/modules/country-disallower"}) is **enabled** for `{Country}`.");

        break;
      }

      default:
      {
        if (cfg.Locale != null)
        {
          cfg.Locale = null;
          await Builders.Edit(c, "Country Disallowing", $"🔸 [Country Disallowing]({Consts.DOCUMENTATION_GITBOOK + "/extra/modules/country-disallower"}) is disabled.");
        }
        else
        {
          await Builders.Edit(c, "Country Disallowing", "🔸 You aren't using this module already.");
        }

        break;
      }
    }
  }

  [SlashCommand("agelimit", "Put age limit of accounts for new members.")]
  public async Task AgeLimit(InteractionContext c, [Option("on", "Whether age limit is enabled or not.")] bool Enabled)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);

    if (!Enabled)
    {
      await Builders.Edit(c, "Age Limit", "🔸 Age limit is removed.");
      return;
    }

    #region Setup Select Options

    var SelectOptions = new List<DiscordSelectComponentOption>();

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            "3 Day",
            description: "Just keep away the new accounts!",
            value: "agelimit_3d",
            emoji: new DiscordComponentEmoji("👋"))
    );

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            "1 Week",
            description: "Ideal value for small servers.",
            value: "agelimit_1w",
            emoji: new DiscordComponentEmoji("🌑"))
    );

    SelectOptions.Add(new DiscordSelectComponentOption
    (
        "1 Month",
        description: "Recommended for most server.",
        value: "agelimit_1m",
        emoji: new DiscordComponentEmoji("☄")
    ));

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            "3 Month",
            description: "Heavily protect the server.",
            value: "agelimit_3m",
            emoji: new DiscordComponentEmoji("➖"))
    );

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            "Custom",
            description: "Choose it yourself.",
            value: "agelimit_custom",
            emoji: new DiscordComponentEmoji("🔵"))
    );

    #endregion

    var SelectMenu = new DiscordSelectComponent("d1", "Ban the accounts that younger than", SelectOptions);

    string DropdownResult = Builders
                            .WaitDropdown("Age Limit", "🔹 Pick up an time limit to put account age limit into server.",
                                SelectMenu, c)
                            .GetAwaiter().GetResult().Result.Values.First();

    var BanDays = 0;

    switch ( DropdownResult )
    {
      case null:
        throw new AbortException();
        break;

      // fill the ban days with the value
      case "agelimit_3d":
        BanDays = 3;
        break;

      case "agelimit_1w":
        BanDays = 7;
        break;

      case "agelimit_1m":
        BanDays = 30;
        break;

      case "agelimit_3m":
        BanDays = 90;
        break;

      case "agelimit_custom":
        DiscordMessage? nextMsg = Builders.WaitMessage(c, "Age Limit", "🔹 Please specify the limit as **DAYS**. (Ex: **10**, **30**)").GetAwaiter().GetResult();

        if (nextMsg == null)
        {
          await Builders.Edit(c, "You forgot me :(", "🔸 Aborted.");
          return;
        }

        if (!int.TryParse(nextMsg.Content, out BanDays))
        {
          await Builders.Edit(c, "Wrong Format", "🔸 Please just specify as days. Do not enter characters.");
          return;
        }

        try
        {
          await c.Channel.DeleteMessageAsync(nextMsg);
        }
        catch
        {
        }

        break;
    }

    DateTime when = DateTime.Now.AddDays(-BanDays);
    cfg.AgeLimit = when;
    await Builders.Edit(c, "Limited OUT", $"🔹 Age limit is **enabled**. **DeAuth** will ban the new members that younger than **{when.ToLogicalString()}**.");
  }

  [SlashCommand("lock", "Enable/Disable joinings. Useful to lock your server fully to outside")]
  public async Task Lock
  (
      InteractionContext c,
      [Option("on", "Whather your server is locked.")]
      bool Enabled,
      [Option("mode", "Mode of locking.")] LockMode Mode)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);

    if (Enabled)
    {
      await Builders.Edit(c, "Locked", $"🔸 Your server is locked to outside. Current lock mode: `{Mode.ToString()}`");
      cfg.LockMode = Mode;
      return;
    }

    // if already false
    if (cfg.LockMode == null)
    {
      await Builders.Edit(c, "Ahh...", "🔸 Your server already not using this module.");
      return;
    }

    await Builders.Edit(c, "Unlocked", "🔹 Lock removed.");
    cfg.LockMode = null;
  }

}