namespace DeAuth.Commands;

[RequireUserPermissions(Permissions.Administrator, false)]
[VerificationDependency("Extra modules only can be used when verification is enabled.")]
[SlashCommandGroup("module", "Powerfully extra protection modules for your server.")]
public class ModuleCommands : ApplicationCommandModule
{

  [SlashCommand("antiraid", "Block the raids on your server.")]
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

  [SlashCommand("cdis", "Auto ban the users that not from your country.")]
  public async Task CountryDisallower(InteractionContext c, [Option("on", "Whether country-disallower is enabled or not.")] bool Enabled)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);

    if (c.Interaction?.Locale == null)
    {
      throw new DException("Uhm?", "This command not available in this server. ");
    }

    string Country = Utils.GetCountry(c.Interaction.Locale);

    switch ( Enabled )
    {
      case true:
      {
        string? enable = Builders.WaitButton(c, "Country Disallowing", $"🔹 Server country has detected as `{Country}`. " +
                                                                       "Do you want to block users that joining from other countries?\n\n" +
                                                                       "・**Warning!** || This feature is currently on beta. Our country detection mechanism still improving, and it may not work in some cases. ||"
            , 20, new[]
            {
                new DiscordButtonComponent(ButtonStyle.Secondary, "disallow_country_true", "", false, new DiscordComponentEmoji("✅")),
                new DiscordButtonComponent(ButtonStyle.Secondary, "disallow_country_false", "", false, new DiscordComponentEmoji("❌"))
            }).GetAwaiter().GetResult();

        if (enable == "disallow_country_true")
        {
          cfg.Locale = Country;

          await Builders.Edit(c, "Country Disallowing",
              $"🔹 [Country Disallowing]({Consts.DOCUMENTATION_GITBOOK + "/more/modules/country-disallower"}) is enabled. **DeAuth** will ban the members that not joining from **{Country}**.");
        }
        else
        {
          await Builders.Edit(c, "Country Disallowing", "🔸 Aborted.");
        }

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

  [SlashCommand("agelimit", "Block the raids on your server.")]
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
            label: "3 Day",
            description: "Just keep away the new accounts!",
            value: "agelimit_3d",
            emoji: new DiscordComponentEmoji("👋"))
    );

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            label: "1 Week",
            description: "Ideal value for small servers.",
            value: "agelimit_1w",
            emoji: new DiscordComponentEmoji("🌑"))
    );

    SelectOptions.Add(new DiscordSelectComponentOption
    (
        label: "1 Month",
        description: "Good for most server.",
        value: "agelimit_1m",
        emoji: new DiscordComponentEmoji("☄")
    ));

    SelectOptions.Add(new DiscordSelectComponentOption
        (
            label: "3 Month",
            description: "Heavily protected server.",
            value: "agelimit_3m",
            emoji: new DiscordComponentEmoji("➖"))
    );

    #endregion

    var SelectMenu = new DiscordSelectComponent("d1", "Ban the accounts that younger than...", SelectOptions);

    string DropdownResult = Builders
                            .WaitDropdown("Age Limit", "🔹 Pick up an time to put account age limit into server.",
                                SelectMenu, c)
                            .GetAwaiter().GetResult().Result.Values.First();

    int BanDays = 0;

    switch ( DropdownResult )
    {
      case null:
        await Builders.Edit(c, "Aborted", "🔸 Aborted.");
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
    }

    DateTime when = DateTime.Now.AddDays(-BanDays);
    cfg.AgeLimit = when;
    await Builders.Edit(c, "Limited OUT", $"🔹 Age limit is **enabled**. **DeAuth** will ban the members that younger than **{BanDays}** days.");
  }

}