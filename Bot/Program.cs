namespace DeAuth.Bot;

public class Program
{

  private static void Main(string[] args)
  {
    Console.Clear();
    Console.Title = "DeAuth";

    var Logo = @"

   ___      ___       __  __ 
  / _ \___ / _ |__ __/ /_/ / 
 / // / -_) __ / // / __/ _ \
/____/\__/_/ |_\_,_/\__/_//_/
                             
";

    Console.WriteLine(Logo);
    new Bot().Run().GetAwaiter().GetResult();
  }

  #region Console Handlers

  private static readonly Dictionary<string, string> CMDS = new()
  {
      {"save", "Save the config with autosave module."},
      {"restart", "Restarts the client."},
      {"guilds", "Displays avaliable guilds."},
      {"stats", "Show the stats of bot."},

      {"get cfg total", "Get the count of total config."},
      {"get guild <id>", "Retrieves guild informations with ID."},
      {"get cfg <id>", "Gets the current config of the server.."},
      {"get logs <id>", "Gets the attempt logs of specific server."},

      {"clear cfg <id>", "Re-creates the config of specific server."},
      {"export cfg <id>", "Exports the config of server as AES256 encrypted string."},
      {"import cfg <cfg_id> <guild_id>", "Import the config from another server to specific server with over_logger.LogWarn."}
  };

  /// <summary>
  ///   Enables dynamic console commands.
  /// </summary>
  public static async Task DynamicConsole()
  {
    /*while (true)
    {
      string? Collection = await GetInputAsync();

      if (Collection == null)
      {
        continue;
      }

      // switch dictionary key
      switch ( Collection )
      {
        case "save":

          Serializers.Serialize();
          _logger.LogWarn("Saved the config with autosave module.");
          break;

        case "stats":

          int TotalVerifiedUsers = Consts.Config.Sum(x => x.Attempts.Count(s => s.Status == Status.Verified));
          int TotalUnverifiedUsers = Consts.Config.Sum(x => x.Attempts.Count(s => s.Status == Status.UnVerified));
          int TotalBannedUsers = Consts.Config.Sum(x => x.Attempts.Count(s => s.Status == Status.Kicked));
          _logger.LogWarn("Total Verified Users: " + TotalVerifiedUsers.ToString());
          _logger.LogWarn("Total Unverified Users: " + TotalUnverifiedUsers.ToString());
          _logger.LogWarn("Total Banned Users: " + TotalBannedUsers.ToString());
          break;

        case "restart":
          _logger.LogWarn("Restarting the client");
          Bot._client.ReconnectAsync(true).GetAwaiter().GetResult();
          _logger.LogWarn("Client is alive again.");
          break;

        case "help":
          var r = 225;
          var g = 255;
          var b = 250;

          foreach ( var cmd in CMDS )
          {
           _logger.LogWarn($">> {cmd.Key} | {cmd.Value}\n");
          }

          break;

        case "guilds":
          var Guilds = new StringBuilder();
          Bot._client.Guilds.Keys.ToList().ForEach(x => Guilds.AppendLine($"  - {Bot._client.GetGuildAsync(x).Result.Name} {x}"));
          _logger.LogWarn(Collection + $" ({Guilds.ToString().Split('\n').Length - 1})", $"\n{Guilds}");
          break;

        #region Getters

        case var PARSE_REQUIRED when Collection.Contains("get logs"):

          try
          {
            string GuildID = Collection.Split(' ')[2];
            var Attempts = new StringBuilder();

            Utils.GetConfig(await Bot._client.GetGuildAsync(ulong.Parse(GuildID)))
                 .Attempts.ToList().ForEach(x => Attempts.AppendLine($"  - {Bot._client.GetUserAsync(x.UserID).Result.Username} ({x.UserID}) | {x.Status} | {x.Time}"));

            _logger.LogWarn(Collection + $" ({Attempts.ToString().Split('\n').Length - 1})", $"\n{Attempts}");
          }
          catch ( Exception e )
          {
            _logger.LogWarn(e.Message);
          }

          break;

        case "get cfg total":
          _logger.LogWarn($"Total Configs - {Consts.Config.Count}");
          break;

        case var PARSE_REQUIRED when Collection.Contains("get guild"):
          try
          {
            ulong ID = ulong.Parse(Collection.Split(' ')[2]);
            DiscordGuild? guild = await Bot._client.GetGuildAsync(ID, false);
            Config cfg = Utils.GetConfig(guild);

            if (!cfg.Enabled)
            {
              _logger.LogWarn("Get Guild", "" +
                                 $"\n  + Guild >> {guild.Name} / Disabled");
            }
            else
            {
              _logger.LogWarn("Get Guild", "" +
                                 $"\n  + Guild >> {guild.Name} / Enabled");
            }
          }
          catch ( Exception e )
          {
            _logger.LogWarn("Get Guild", "Guild not found! " + e.Message);
          }

          break;

        #endregion

        #region Config Tools

        case var PARSE_REQUIRED when Collection.Contains("get cfg"):
          try
          {
            ulong ID = ulong.Parse(Collection.Split(' ')[2]);
            DiscordGuild? guild = await Bot._client.GetGuildAsync(ID, false);
            Config cfg = Utils.GetConfig(guild);

            // _logger.LogWarn the guild id from cfg
            _logger.LogWarn("Get Config",
                $"  // Enabled - {cfg.Enabled}\n" +
                $"  // Captcha Len - {cfg.CaptchaOptions.Length}\n" +
                $"  // Captcha Mode - {cfg.CaptchaOptions.Mode}\n" +
                $"  // Captcha Fail Op - {cfg.CaptchaOptions.OnVerifyFail}\n" +
                $"  // Verify Channel - {cfg.VerifyChannel}\n" +
                $"  // Log Channel - {cfg.LogChannel}\n" +
                $"  // Quarantine Role - {cfg.RoleID}\n" +
                $"  // Count of attempts - {cfg.Attempts.Count}");
          }
          catch ( Exception e )
          {
            _logger.LogWarn("Fatal Error", e.Message);
          }

          break;

        case var PARSE_REQUIRED when Collection.Contains("clear cfg"):
          try
          {
            ulong ID = ulong.Parse(Collection.Split(' ')[2]);
            DiscordGuild? guild = await Bot._client.GetGuildAsync(ID, false);
            Config cfg = Utils.GetConfig(guild);

            if (!cfg.Enabled)
            {
              _logger.LogWarn("Ups", "Verification not enabled on this server.");
            }
            else
            {
              cfg.Reload();
              _logger.LogWarn("Reloaded", $"Guild {guild.Name} config has been reloaded.");
            }
          }
          catch ( Exception e )
          {
            _logger.LogWarn("Fatal Error", e.Message);
          }

          break;

        case var PARSE_REQUIRED when Collection.Contains("export cfg"):
          try
          {
            ulong ID = ulong.Parse(Collection.Split(' ')[2]);
            DiscordGuild? guild = await Bot._client.GetGuildAsync(ID, false);
            Config cfg = Utils.GetConfig(guild);

            if (!cfg.Enabled)
            {
              _logger.LogWarn("Ups", "Verification not enabled on this server.");
            }
            else
            {
              _logger.LogWarn("Exported", $"{ConfigManager.SendConfig(cfg, 0)}");
            }
          }
          catch ( Exception e )
          {
            _logger.LogWarn("Fatal Error", e.Message);
          }

          break;

        case var PARSE_REQUIRED when Collection.Contains("import cfg"):
          try
          {
            ulong GID = ulong.Parse(Collection.Split(' ')[2]);
            string EGF = Collection.Split(' ').Last();
            DiscordGuild? guild = await Bot._client.GetGuildAsync(GID, false);
            Config cfg = Utils.GetConfig(guild);

            try
            {
              ConfigManager.ImportConfig(GID, cfg, EGF);
            }
            catch ( Exception e )
            {
              _logger.LogWarn("Fatal Error", "Key may be invalid. " + e.Message);
            }

            _logger.LogWarn("Imported", $"Guild {guild.Name}'s config successfully over_logger.LogWarnd.");
          }
          catch ( Exception e )
          {
            _logger.LogWarn("Fatal Error", e.Message);
          }

          break;

        #endregion

        default:
          _logger.LogWarn("Invalid command.", "This is not a valid command.");
          break;
      }
    }*/
  }

  /// <summary>
  ///   Gathers the console input async.
  /// </summary>
  private static Task<string?> GetInputAsync()
  {
    return Task.Run(Console.ReadLine);
  }

  #endregion

}