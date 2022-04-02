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
    bool ShowConfig = FailOperation == null && CaptchaLength == null && LogChannel == null && captchaMode == null && WelcomeMessage == null;

    if (ShowConfig)
    {
      string? rm = "Unkown", vm = "Unkown", lm = "Unkown";
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
          Title = "Config",
          Author = new DiscordEmbedBuilder.EmbedAuthor
          {
              Name = c.Guild.Name,
              IconUrl = c.Guild?.IconUrl
          },
          Color = DiscordColor.Rose,
          Footer = new DiscordEmbedBuilder.EmbedFooter
          {
              Text = "DeAuth"
          }
      };

      embed.AddField("Verify Settings",
          $"᲼᲼・Quarantine Role ⟩ {rm}\n" +
          $"᲼᲼・Verify Channel ⟩ {vm}\n" +
          $"᲼᲼・Logging Channel ⟩ {lm}\n" +
          $"᲼᲼・DM Message ⟩ {cfg.WelcomeMessage}");

      embed.AddField("Captcha Settings",
          $"᲼᲼・Verify Fail ⟩ **{cfg.CaptchaOptions.OnVerifyFail}**\n" +
          $"᲼᲼・Length ⟩ **{cfg.CaptchaOptions.Length}**\n" +
          $"᲼᲼・Mode ⟩ **{cfg.CaptchaOptions.Mode}**\n");

      embed.AddField("Modules (/module)",
          $"᲼᲼・Account Age Limit ⟩ **{((cfg.AgeLimit != null) ? $"{cfg.AgeLimit.Value.LogicalTime()}" : "False")}**\n" +
          $"᲼᲼・Country Disallowing ⟩ **{((cfg.Locale != null) ? $"{cfg.Locale}" : "False")}**\n" +
          $"᲼᲼・Anti Raid ⟩ **{cfg.AntiRaid}**");

      if (errors.Length > 0)
      {
        embed.AddField("Health", errors.ToString());
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
  public async Task Export(InteractionContext c, [Option("logs", "Logs should included in template also.")] bool logs = false)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    string ENCRYPTED_CONFIG_KEY = null;

    try
    {
      Config ca = Utils.GetConfig(c.Guild);                      // to prevent from resetting original config, create new instance
      ca.Attempts = logs ? ca.Attempts : new List<UserStatus>(); // remove logs if not requested
      ENCRYPTED_CONFIG_KEY = ConfigManager.SendConfig(ca, c.User.Id);
    }
    catch
    {
      throw new DException("Where is wumpus?", "Failed to export your config. Your config may damaged.");
    }

    await Builders.Edit(c, "Exported",
        $"🔹 Config successfully exported. [Take a look at share docs]({Consts.DOCUMENTATION_GITBOOK + "/basics/config-sharing"}).\n" +
        $"⟩ Key: || {ENCRYPTED_CONFIG_KEY} ||");
  }

  [SlashCommand("import", "Import an config from other servers.")]
  public static async Task Import(InteractionContext c, [Option("key", "The key of exported config.")] string Key)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    SharedConfig? scfg;
    Config cfg;
    ulong Gid, Uid;

    try
    {
      scfg = ConfigManager.ReceiveConfig(Key);
      _ = scfg.UserID;
      _ = scfg.Config.GuildID;
    }
    catch
    {
      throw new DException("Incorrect Key?", $"An key || {Key} || was not valid template key.");
    }

    string SharerGuild = c.Client.GetGuildAsync(scfg.Config.GuildID).Result?.Name ?? "**Unkown**";
    string SharerUser = c.Client.GetUserAsync(scfg.UserID).Result?.Mention ?? "**Unkown**";

    string? Import = Builders.WaitButton(c,
        "Import Config",
        "**⟩** Do you want import this config? the current config will be overwritten.\n\n" +
        $"⠀🔹 From server **⟩** {SharerGuild} - {scfg.Config.GuildID}\n" +
        $"⠀🔹 Created by **⟩** {SharerUser}\n" +
        $"⠀🔹 Created on **⟩** {scfg.CreatedOn}\n", 15,
        new[] {new DiscordButtonComponent(ButtonStyle.Secondary, "import", "Import")}).GetAwaiter().GetResult();

    if (Import != "import")
    {
      await Builders.Edit(c, "Aborted", "");
      return;
    }

    await Builders.Edit(c, "Clearing", "**⟩** Clearing the old verification data...");
    await Utils.RemovePanel(c.Guild);

    Config Guild = Utils.GetConfig(c.Guild);
    await Builders.Edit(c, "Importing", "**⟩** Config setting up...");
    ConfigManager.ImportConfig(c.Guild.Id, Guild, Key);

    await Builders.Edit(c, "Success", "🔹 Config successfully imported. Use `/setup create` to finish setup.\n\n" +
                                      $"・[Notes]({Consts.DOCUMENTATION_GITBOOK}/basics/config-sharing/import)\n");
  }

}