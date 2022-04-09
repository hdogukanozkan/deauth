namespace DeAuth.Commands;

[SlashRequireUserPermissions(Permissions.Administrator)]
[SlashCommandGroup("config", "Edit & style your server config.")]
public class ConfigCommands : ApplicationCommandModule
{

  [SlashCommand("reset", "Reset your config to default.")]
  public async Task Reset(InteractionContext c)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    string? Confirm = Builders.WaitButton(c, "Reset Config", "・Do you want to reset config? your verification panel and roles will keep stay. But your " +
                                                             "captcha options, logs and other preferences will be gone.", 15, new[]
    {
        new DiscordButtonComponent(ButtonStyle.Danger, "confirm", "Reset")
    }).GetAwaiter().GetResult();

    if (Confirm != "confirm")
    {
      await c.DeleteResponseAsync();
      return;
    }

    await Builders.Edit(c, "Clearing", "**⟩** Pouring your config...");
    Config cfg = Utils.GetConfig(c.Guild);

    var config = new Config
    {
        VerifyChannel = cfg.VerifyChannel,
        LogChannel = cfg.LogChannel,
        RoleID = cfg.RoleID,
        GuildID = cfg.GuildID,
        Enabled = cfg.Enabled
    };

    cfg.ReplaceWith(config);
    await Builders.Edit(c, "Cleared", "🔹 Config reset successfully.");
  }

  [SlashCommand("edit", "Customize your config & bot.")]
  public async Task Configure
  (
      InteractionContext c,
      [Option("verify_fail", "Action when user failed to verify.")]
      VerifyFail? FailOperation = null,
      [Option("captcha_length", "The length of captcha code.")]
      double? CaptchaLength = null,
      [Option("log_channel", "Channel to log actions.")]
      DiscordChannel? LogChannel = null,
      [Option("captcha_mode", "The mode of captcha codes.")]
      CaptchaMode? captchaMode = null,
      [Option("quarantine_mode", "Type of users to quarantine.")]
      QuarantineType? quarantineType = null,
      [Option("welcome_message", "The message to send via DM on join.")]
      string? WelcomeMessage = null)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config cfg = Utils.GetConfig(c.Guild);
    bool ShowConfig = FailOperation == null && CaptchaLength == null && LogChannel == null && captchaMode == null && WelcomeMessage == null && quarantineType == null;

    if (ShowConfig)
    {
      string? rm = "**Unkown**", vm = "**Unkown**", lm = "**Unkown**";
      var errors = new StringBuilder();
      // Fill the values

      try
      {
        vm = c.Guild.GetChannel(cfg.VerifyChannel).Mention;
      }
      catch
      {
        errors.AppendLine($"🔸 [Cannot find]({Consts.DOCUMENTATION_GITBOOK + "/extra/issues"}) verify channel.");
      }

      try
      {
        rm = c.Guild.GetRole(cfg.RoleID).Mention;
      }
      catch
      {
        errors.AppendLine($"🔸 [Cannot find]({Consts.DOCUMENTATION_GITBOOK + "/extra/issues"}) quarantine role.");
      }

      if (cfg.LogChannel != 0)
      {
        try
        {
          lm = c.Guild.GetChannel(cfg.LogChannel).Mention;
        }
        catch
        {
          errors.AppendLine($"🔸 [Cannot find]({Consts.DOCUMENTATION_GITBOOK + "/extra/issues"}) log channel.");
        }
      }
      else
      {
        lm = "None";
      }

      var embed = new DiscordEmbedBuilder
      {
          Author = new DiscordEmbedBuilder.EmbedAuthor
          {
              Name = c.Guild.Name,
              IconUrl = c.Guild?.IconUrl
          },
          Color = DiscordColor.Lilac,
          Footer = new DiscordEmbedBuilder.EmbedFooter
          {
              Text = "DeAuth"
          }
      };

      embed.AddField("🔸 Verify Settings",
          $"᲼᲼・Quarantine Role ⟩ {rm}\n" +
          $"᲼᲼・Verify Channel ⟩ {vm}\n" +
          $"᲼᲼・Logging Channel ⟩ {lm}\n" +
          $"᲼᲼・DM Message ⟩ {cfg.WelcomeMessage ?? "**None**"}");

      embed.AddField("🔸 Captcha Settings",
          $"᲼᲼・Verify Fail ⟩ **{cfg.CaptchaOptions.OnVerifyFail}**\n" +
          $"᲼᲼・Length ⟩ **{cfg.CaptchaOptions.Length}**\n" +
          $"᲼᲼・Mode ⟩ **{cfg.CaptchaOptions.Mode}**\n");

      embed.AddField("🔸 Modules",
          $"᲼᲼・Age Limit ⟩ **{cfg.AgeLimit?.ToLogicalString() ?? "False"}**\n" +
          $"᲼᲼・Country Disallowing ⟩ **{cfg.Locale ?? "False"}**\n" +
          $"᲼᲼・Anti Raid ⟩ **{cfg.AntiRaid}**\n" +
          $"᲼᲼・Lockdown ⟩ **{(cfg.LockMode == null ? "False" : cfg.LockMode)}**\n");

      if (errors.Length > 0)
      {
        embed.AddField("Errors", errors.ToString());
      }

      await Builders.Edit(c, embed.Build());
    }
    else
    {
      var sb = new StringBuilder();

      if (LogChannel != null)
      {
        cfg.LogChannel = LogChannel.Id;
        sb.AppendLine($"・Log Channel ⟩ {LogChannel.Mention}");
      }

      if (captchaMode != null)
      {
        cfg.CaptchaOptions.Mode = captchaMode.Value;
        sb.AppendLine($"・Captcha Mode ⟩ `{captchaMode.Value}`");
      }

      if (FailOperation != null)
      {
        cfg.CaptchaOptions.OnVerifyFail = FailOperation.Value;
        sb.AppendLine($"・Captcha Fail Action ⟩ `{FailOperation.Value.ToString()}`");
      }

      if (CaptchaLength != null)
      {
        if (CaptchaLength is > 10 || !int.TryParse(CaptchaLength.ToString(), out _))
        {
          throw new DException("Out of range", "Captcha Length must be between 1 - 10.");
        }

        cfg.CaptchaOptions.Length = (int) CaptchaLength.Value;
        sb.AppendLine($"・Captcha Length ⟩ `{CaptchaLength.Value}`");
      }

      if (quarantineType != null)
      {
        cfg.QuarantineType = quarantineType.Value;
        sb.AppendLine($"・Quarantine Type ⟩ `{quarantineType.Value}`");
      }

      if (WelcomeMessage != null)
      {
        cfg.WelcomeMessage = WelcomeMessage;
        sb.AppendLine($"・Welcome Message ⟩ `{WelcomeMessage}`");
      }

      await Builders.Edit(c, "Config Updated", sb.ToString());
    }
  }

  [VerificationDependency("・You must create a config first to share it.")]
  [SlashCommand("export", "Create a backup/template of your config.")]
  public async Task Export
  (
      InteractionContext c,
      [Option("name", "Nameof config.")] string Name,
      [Option("logs", "Whather logs should included in template also.")]
      bool logs = false)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    if (string.IsNullOrEmpty(Name))
    {
      await Builders.Edit(c, "Hmm?", "🔸 You must provide a name for your config.");
      return;
    }

    if (Name is {Length: > 16 or <= 1}) // Hell,,,, this is best pattern in cs
    {
      await Builders.Edit(c, "Too short... or long", "🔸 Config name must be between **1 - 16** characters.");
      return;
    }

    string? ENCRYPTED_CONFIG_KEY;

    try
    {
      Config ca = Utils.GetConfig(c.Guild);                      // to prevent from resetting original config, create new instance
      ca.Attempts = logs ? ca.Attempts : new List<UserStatus>(); // remove logs if not requested
      ENCRYPTED_CONFIG_KEY = ConfigManager.CreateTemplate(ca, c.User.Id, Name);
    }
    catch
    {
      throw new DException("Upps?", "Failed to export your config. Your config may damaged.");
    }

    await Builders.Edit(c, "Exported",
        $"🔹 Config successfully exported. [Take a look at share docs]({Consts.DOCUMENTATION_GITBOOK + "/basics/config-sharing"}).\n" +
        $"⟩ Key: || {ENCRYPTED_CONFIG_KEY} ||");
  }

  [SlashCommand("import", "Import an config from another server.")]
  public static async Task Import(InteractionContext c, [Option("key", "The key of exported config.")] string Key)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    ConfigTemplate? scfg;

    try
    {
      scfg = ConfigManager.ImportConfig(Key);
      _ = scfg.UserID;
      _ = scfg.Config.GuildID;
    }
    catch
    {
      throw new DException("What is it?", "Failed to import this config. There are two possible reasons:\n" +
                                          "⠀🔹 Key is invalid. Decrypt failed.\n" +
                                          "⠀🔹 Key from old versions of **DeAuth**.");
    }

    string SharerGuild = c.Client.GetGuildAsync(scfg.Config.GuildID).Result?.Name ?? "**Unkown**";
    string SharerUser = c.Client.GetUserAsync(scfg.UserID).Result?.Mention ?? "**Unkown**";

    string? Import = Builders.WaitButton(c,
        "Import Config",
        $"**⟩** Would you like to import `{scfg.Name}`. Current config will be overwritten.\n\n" +
        $"⠀🔹 From server **⟩** {SharerGuild} - {scfg.Config.GuildID}\n" +
        $"⠀🔹 Created by **⟩** {SharerUser}\n" +
        $"⠀🔹 Created on **⟩** {scfg.CreatedOn.ToLogicalString()} **ago**\n", 15,
        new[] {new DiscordButtonComponent(ButtonStyle.Secondary, "import", "Import")}).GetAwaiter().GetResult();

    if (Import != "import")
    {
      throw new AbortException();
    }

    await Builders.Edit(c, "Clearing", "**⟩** Clearing the old verification data...");
    await Utils.RemovePanel(c.Guild);

    Config Guild = Utils.GetConfig(c.Guild);
    await Builders.Edit(c, "Importing", "**⟩** Config setting up...");
    ConfigManager.Overwrite(c.Guild.Id, Guild, Key);

    await Builders.Edit(c, "Success", "🔹 Config successfully imported. Use `/setup create` to finish importing.\n\n" +
                                      $"・[Notes]({Consts.DOCUMENTATION_GITBOOK}/basics/config-sharing/import)\n");
  }

}