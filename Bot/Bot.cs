namespace DeAuth.Bot;

public class Bot
{

  public static DiscordClient? _client;

  /// <summary>
  ///   The main entry of bot.
  /// </summary>
  public Bot()
  {
    _client = new DiscordClient(new DiscordConfiguration
    {
        Token = "OTQ2MTMzNzU0NzY4OTIwNTc2.YhaRuQ.EYLvEWVgEPu_iVrsX2c8OkGx5q8",
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.GuildMembers | DiscordIntents.Guilds | DiscordIntents.GuildMessages,
        MinimumLogLevel = LogLevel.Information
    });

    _client.UseInteractivity(new InteractivityConfiguration
    {
        Timeout = TimeSpan.FromSeconds(20),
        ButtonBehavior = ButtonPaginationBehavior.Disable,
        AckPaginationButtons = true, // auto handle 
        PaginationBehaviour = PaginationBehaviour.WrapAround
    });

    SlashCommandsExtension? slash = _client.UseSlashCommands();
    _client.GuildAvailable += GuildExposed;
    _client.GuildCreated += GuildAdded;
    _client.ComponentInteractionCreated += ButtonPressed;
    _client.GuildMemberAdded += MemberJoined;
    _client.Ready += ClientOnReady;
    _client.ModalSubmitted += CaptchaHandler;
    slash.SlashCommandErrored += OnError;

    slash.RegisterCommands<GeneralCommands>();
    slash.RegisterCommands<ConfigCommands>();
    slash.RegisterCommands<SetupCommands>();
    slash.RegisterCommands<ModuleCommands>();

    try
    {
      Consts.Config = Serializers.DeSerialize();
    }
    catch ( Exception e )
    {
      Consts.Config = new List<Config>();
    }

    Serializers.BindAutoSave(TimeSpan.FromSeconds(20));
  }

  /// <summary>
  ///   Occurs when guild is created.
  /// </summary>
  private static async Task GuildAdded(DiscordClient sender, GuildCreateEventArgs e)
  {
    if (Consts.Config.All(x => x.GuildID != e.Guild.Id))
    {
      Consts.Config.Add(new Config
      {
          GuildID = e.Guild.Id
      });
    }
  }

  /// <summary>
  ///   Fires when slash command is errored.
  /// </summary>
  private static async Task OnError(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
  {
    Console.WriteLine(e.Exception.Message);
    Console.WriteLine(e.Exception?.StackTrace);

    try
    {
      await e.Context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
          new DiscordInteractionResponseBuilder()
              .AsEphemeral());
    }
    catch
    {
      // ignored
    }

    switch ( e.Exception )
    {
      #region Handling

      case DException de: // A main handler of bot
        await Builders.Edit(
            e.Context,
            de.error_title, $"🔸 {de.error_desc}");
        break;

      case SlashExecutionChecksFailedException slex:
      {
        SlashCheckBaseAttribute? check = slex.FailedChecks.First();

        switch ( check )
        {
          case VerificationDependency ve:
            await Builders.Edit(
                e.Context,
                "Verification Requirement", ve._errormessage);
            break;

          case SlashRequireUserPermissionsAttribute: // User has no permissions
            await Builders.Edit(
                e.Context,
                "403", "・You don't have enough permissions to use this command!");
            break;

          case SlashRequireBotPermissionsAttribute be: // Bot has no permissions
            await Builders.Edit(
                e.Context,
                "Broken Setup",
                $"・Looks like bot [permissions]({Consts.DOCUMENTATION_GITBOOK + "/extra/issues#permissions"}) not setup correctly. Please add the following permissions to **DeAuth** role: {be.Permissions} or re-add the bot to server.");
            break;
        }

        break;
      } // Checkers

      #endregion
    }
  }

  /// <summary>
  ///   A handler for presence events.
  /// </summary>
  private static async Task ClientOnReady(DiscordClient client, ReadyEventArgs e)
  {
    await client.UpdateStatusAsync(new DiscordActivity("/help", ActivityType.Playing));
  }

  /// <summary>
  ///   Handles the members that joined to the server. Quarantine will apply auto.
  /// </summary>
  private static async Task MemberJoined(DiscordClient sender, GuildMemberAddEventArgs e)
  {
    Config cfg = Utils.GetConfig(e.Guild);

    #region Check Member

    // Check if verify enabled.
    if (!cfg.Enabled)
    {
      return;
    }

    // Check to member creation timestampt.
    if (cfg.AgeLimit != null)
    {
      if (e.Member.CreationTimestamp.AddDays(0) < cfg.AgeLimit.Value)
      {
        await e.Member.BanAsync(reason: "Age Limit triggered.");
        return;
      }
    }

    // Check suspicious accounts
    if (cfg.AntiRaid)
    {
      // Get the latest join time of suspicious user.
      double TotalJoinDelay = (DateTimeOffset.Now - Consts.SuspiciousCounter[e.Guild]).TotalSeconds;

      if (e.Member.CalculateSuspiciously()) // Increase the counter of sus users in dict.
      {
        if (TotalJoinDelay < 10)
        {
          await e.Member.BanAsync(reason: "Suspicious account.");
          Utils.Log(e.Guild, e.Member, LogType.RaidDetected);
        }
        else
        {
          Consts.SuspiciousCounter[e.Guild] = DateTimeOffset.Now;
        }
      }
    }

    if (cfg.Locale != null)
    {
      if (!new CultureInfo(e.Member.Locale).EnglishName.Contains(cfg.Locale))
      {
        await e.Member.BanAsync(reason: $"User not joining from {cfg.Locale}");
        Utils.Log(e.Guild, e.Member, LogType.CDIS);
      }
    }

    // Send welcome message
    if (cfg.WelcomeMessage != null)
    {
      try
      {
        await e.Member.SendMessageAsync(cfg.WelcomeMessage);
      }
      catch ( Exception )
      {
      }
    }

    #endregion

    #region Determine which users are going to quarantine

    if ((int) cfg.QuarantineType > 0)
    {
      switch ( cfg.QuarantineType )
      {
        // filter users
        case QuarantineType.OnlyBots when !e.Member.IsBot:
        case QuarantineType.OnlyUsers when e.Member.IsBot:
          return;
      }
    }

    await e.Member.GrantRoleAsync(cfg.GetQuarantineRole());

    cfg.Attempts.Add(new UserStatus() // This should be added rn
    {
        Status = Status.UnVerified,
        UserID = e.Member.Id,
        Time = DateTime.Now
    });

    if (cfg.AgeLimit.HasValue)
    {
      if (e.Member.CreationTimestamp < cfg.AgeLimit)
      {
        await e.Member.BanAsync(0, "The account is too young to join this server.");
        Utils.Log(e.Guild, e.Member, LogType.AgeLimit);
        return;
      }
    }

    #endregion

    Utils.Log(e.Guild, e.Member, LogType.Join);
  }

  /// <summary>
  ///   Handles the button components to create a new verification.
  /// </summary>
  private static async Task ButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs e)
  {
    switch ( e.Id )
    {
      case Consts.VERIFY_COMPONENT_ID:
      {
        Config cfg = Utils.GetConfig(e.Guild);

        #region Check locale for country disallowing module

        if (cfg.Locale != null && e.Interaction?.Locale != null)
        {
          if (Utils.GetCountry(e.Interaction?.Locale) != cfg.Locale)
          {
            string Country = Utils.GetCountry(e.Interaction.Locale);
            DiscordMember member = await e.Guild.GetMemberAsync(e.User.Id);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                                                                                      .AddEmbed(
                                                                                                          Builders.BasicEmbed("Locked",
                                                                                                              $"🔸 Sorry but this server not accepts users that are from **{Country}**."))
                                                                                                      .AsEphemeral());
            Utils.Log(e.Guild, member, LogType.CDIS);
            return;
          }
        }

        #endregion

        #region Check if member cooldown

        if (Consts.ButtonCooldown.ContainsKey(e.User.Id))
        {
          DateTimeOffset cooldown = Consts.ButtonCooldown[e.User.Id];

          if (cooldown.AddSeconds(5) > DateTime.Now)
          {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                                                                                      .AddEmbed(
                                                                                                          Builders.BasicEmbed("Cooldown",
                                                                                                              "🔸 You're triggered the cooldown. Please wait 5 second to try again."))
                                                                                                      .AsEphemeral());
            return;
          }
        }

        Consts.ButtonCooldown[e.User.Id] = DateTime.Now;

        #endregion

        // Config is set to disabled. Verify it rn.
        if (cfg.CaptchaOptions.Mode == CaptchaMode.NoCaptcha)
        {
          await cfg.Verify(e.Interaction);
          return;
        }

        string Captcha = Builders.GenerateCaptcha(cfg.CaptchaOptions.Mode, cfg.CaptchaOptions.Length); // Create captcha code

        DiscordInteractionResponseBuilder? Modal = new DiscordInteractionResponseBuilder()
                                                   .WithCustomId(cfg.GuildID.ToString())
                                                   .WithTitle("Verify")
                                                   .WithContent("🔸 Pass the verification to get access")
                                                   .AddComponents(new TextInputComponent(
                                                       $"{Captcha}",
                                                       Captcha,
                                                       "🔹 Write the code to here.",
                                                       required: true,
                                                       style: TextInputStyle.Short,
                                                       max_length: Captcha.Length));
        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, Modal);

        break;
      }
    }
  }

  /// <summary>
  ///   Handles the guilds to deserialize the guild options.
  /// </summary>
  private static async Task GuildExposed(DiscordClient sender, GuildCreateEventArgs e)
  {
    // Add the config, if not exists
    if (!Consts.Config.Exists(x => x.GuildID == e.Guild.Id))
    {
      Consts.Config.Add(new Config
      {
          GuildID = e.Guild.Id
      });
    }
  }

  /// <summary>
  ///   Handles the submitted captcha codes.
  /// </summary>
  private static async Task CaptchaHandler(DiscordClient sender, ModalSubmitEventArgs e)
  {
    List<string> Values = e.Values.Values.ToList();

    #region Verify User

    DiscordGuild? Guild = e.Interaction.Guild;
    Config Config = Utils.GetConfig(Guild);
    string CaptchaCode = e.Interaction.Data.Components[0].Components.First().CustomId;
    string UserResponse = Values.First();

    if (string.Equals(CaptchaCode, UserResponse, StringComparison.OrdinalIgnoreCase))
    {
      await Config.Verify(e.Interaction);
    }
    else // CaptchaOptions failed
    {
      DiscordMember? Member = await Guild.GetMemberAsync(e.Interaction.User.Id);

      await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
          new DiscordInteractionResponseBuilder()
              .AsEphemeral()
              .AddEmbed(
                  Builders.BuildEmbed(e.Interaction.User,
                      "Verify",
                      "**⟩** Captcha was incorrect, please try again.\n",
                      DiscordColor.Red,
                      Footer: "DeAuth Verification")));

      switch ( Config.CaptchaOptions.OnVerifyFail )
      {
        case VerifyFail.Ban:
          await Member.BanAsync(0, "Banned because of failed captcha.");
          Config.EditStatus(e.Interaction.User.Id, Status.Kicked);
          Utils.Log(e.Interaction.Guild, await e.Interaction.Guild.GetMemberAsync(e.Interaction.User.Id), LogType.VFail);
          break;

        case VerifyFail.Kick:
          await Member.RemoveAsync("Kicked because of failed captcha.");
          Config.EditStatus(e.Interaction.User.Id, Status.Kicked);
          Utils.Log(e.Interaction.Guild, await e.Interaction.Guild.GetMemberAsync(e.Interaction.User.Id), LogType.VFail);
          break;

        case VerifyFail.Nothing:
          Config.EditStatus(e.Interaction.User.Id, Status.UnVerified);
          break;
      }
    }

    #endregion
  }

  /// <summary> Runs the client. </summary>
  public async Task Run()
  {
    await _client.ConnectAsync();
    await Program.DynamicConsole();
    await Task.Delay(Timeout.Infinite);
  }

}