namespace DeAuth.Modules;

public static class Utils
{

  private static readonly DiscordClient? client = Bot.Bot._client;

  /// <summary>
  ///   Returns config of specific guild.
  /// </summary>
  public static Config GetConfig(DiscordGuild guild)
  {
    return Consts.Config.First(x => x.GuildID == guild.Id);
  }

  /// <summary>
  ///   Logs specific log header to log channel.
  /// </summary>
  public static void Log(DiscordGuild guild, DiscordMember member, LogType Type)
  {
    string Title = "", Description = "";
    DiscordColor color = DiscordColor.Gold;

    Config Guild = GetConfig(guild);
    ulong channel = Guild.LogChannel;

    // If logger isnt enabled or verification isnt enabled return;
    if (Guild.LogChannel == 0 || !Guild.Enabled)
    {
      return;
    }

    switch ( Type )
    {
      case LogType.New_Join:
        Title = "Member Joined";

        Description = $">>> {member.Mention} has joined to the server. Waiting for **verification**... \n\n" +
                      $"✓ Created on **/** `({member.CreationTimestamp:dd/MM/yyyy HH:mm:ss})`\n";
        color = DiscordColor.Blurple;
        break;

      case LogType.New_Verify:
        string elapsed_v_time = (DateTime.Now - member.JoinedAt).LogicalTime();
        Title = "Member Verified";

        Description = $"> {member.Mention} has been successfully verified.\n\n" +
                      $"✓ Elapsed Verify Time **/** `{elapsed_v_time}`\n";
        color = DiscordColor.Green;
        break;

      case LogType.New_Fail:
        Title = "Member Kicked";

        Description =
            $">>> {member.Mention} has been **{(Guild.CaptchaOptions.OnVerifyFail == VerifyFail.Kick ? "Kicked" : "Banned")}** due `Captcha Verify Fail` mode.\n\n";
        color = DiscordColor.Red;
        break;

      case LogType.AgeLimit:
        Title = "Member Kicked";

        if (Guild.AgeLimit != null)
        {
          Description = $"> {member.Mention} has been banned auto due to **Age limit**.\n\n" +
                        $"・Current age limit: `{Guild.AgeLimit} | ({Guild.AgeLimit.Value.Day})` days.\n" +
                        $"・User account age: `{member.CreationTimestamp}`";
        }

        color = DiscordColor.Red;
        break;

      case LogType.New_Bot:
        Title = "Bot Added";
        Description = $">>> A bot {member.Mention} is added to server and quarantined. Check the bot before removing quarantine from it.\n\n";
        color = DiscordColor.Red;
        break;

      case LogType.New_Raider:
        Title = "Raid Detected";
        Description = $">>> Member {member.Mention} is banned due being sus.\n\n";
        color = DiscordColor.Red;
        break;

      case LogType.Country_Disallowing:
        Title = "Country Disallowing";
        Description = $">>> Member {member.Mention}'s country wasn't matching with server's country. It banned.\n\n";
        color = DiscordColor.Red;
        break;

      default: throw new ArgumentOutOfRangeException(nameof(Type), Type, null);
    }

    DiscordEmbed embed = Builders.BuildEmbed(member, Title, Description, color, true);
    guild.GetChannel(channel).SendMessageAsync(embed);
  }

  #region Panel-Verification Utils

  /// <summary>
  ///   Removes all verify roles, channels in server. Also cleares and creates a new config for the guild from the config.
  /// </summary>
  /// <param name="Guild">Guild to clear.</param>
  public static async Task RemovePanel(DiscordGuild Guild)
  {
    #region Clear channels and roles

    // Delete channel overwrites.
    try
    {
      foreach ( (ulong _, DiscordChannel? channel) in Guild.Channels )
        if (channel.Name == Consts.VERIFY_CHANNEL_NAME) // Delete if channel is verification channel.
        {
          await channel.DeleteAsync();
        }
    }
    catch
    {
      // ignored
    }

    // Delete Quarantine roles
    try
    {
      foreach ( var role in Guild.Roles.Where(x => x.Value.Name == Consts.QUARANTINE_ROLE_NAME) )
        await role.Value.DeleteAsync();
    }
    catch
    {
    }

    #endregion
  }

  /// <summary>
  ///   Creates a new verification channels, roles and panels for guild
  /// </summary>
  /// <param name="Guild">Guild to clear.</param>
  public static async Task<DiscordChannel> CreatePanel(DiscordGuild Guild, string PanelTitle, string PanelDesc, string ButtonText)
  {
    await RemovePanel(Guild); // Delete old verification traces.
    Config guild = GetConfig(Guild);
    DiscordRole? Role = guild.GetQuarantineRole();

    #region Override Channels

    // Override the verification panel channel permissions. Set to Commands.QuarantineRoleName roled users can see it.
    var channels = Guild.Channels;

    foreach ( var channel in channels )
      try
      {
        await channel.Value.AddOverwriteAsync(Role, Permissions.None, Permissions.AccessChannels);
      }
      catch
      {
      }

    #endregion

    #region Create Verification Panel

    // Create overwritten verification channel.
    var overwrites = new List<DiscordOverwriteBuilder>
    {
        new DiscordOverwriteBuilder // Verified users shouldn't see this channel.
                (Guild.EveryoneRole)
            .Deny(Permissions.AccessChannels),
        new DiscordOverwriteBuilder // Allow verify for non-verifed users.
                (Role)
            .Allow(Permissions.AccessChannels)
    };
    DiscordChannel verificationChannel;

    try
    {
      verificationChannel = await Guild.CreateChannelAsync(Consts.VERIFY_CHANNEL_NAME, ChannelType.Text, overwrites: overwrites);
    }
    catch
    {
      throw new DException(":(",
          $"Failed to create verification channel. [Check the bot permissions]({Consts.DOCUMENTATION_GITBOOK + "/extra/issues#permissions"})");
    }

    #endregion

    #region Initialize Components

    DiscordMessageBuilder? Modal = new DiscordMessageBuilder()
                                   .AddEmbed(new DiscordEmbedBuilder
                                   {
                                       Title = PanelTitle,
                                       Description = PanelDesc,
                                       Author = new DiscordEmbedBuilder.EmbedAuthor
                                           {IconUrl = Guild?.IconUrl, Name = Guild.Name},
                                       Footer = new DiscordEmbedBuilder.EmbedFooter
                                           {Text = "DeAuth Verification"},
                                       Color = DiscordColor.Grayple
                                   })
                                   .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, Consts.VERIFY_COMPONENT_ID, ButtonText));

    #endregion

    #region Overwrite config

    try
    {
      await verificationChannel.SendMessageAsync(Modal);
    }
    catch
    {
      throw new DException("Failed to send message!", "Failed to send message to verification channel! check if bot is setup correctly.");
    }

    guild.Enabled = true;
    guild.VerifyChannel = verificationChannel.Id;

    #endregion

    return verificationChannel;
  }

  #endregion

  #region Extensions / Methods not related with discord

 private static Dictionary<string, string> CountryCodes = new Dictionary<string, string>()
  {
      {"EN", "United States"},
      {"AF", "Afghanistan"},
      {
          "AL", "Albania"
      },
      {"DZ", "Algeria"},
      {"AS", "American Samoa"},
      {"AD", "Andorra"},
      {
          "AO", "Angola"
      },
      {
          "AI", "Anguilla"
      },
      {
          "AQ", "Antarctica"
      },
      {
          "AG", "Antigua and Barbuda"
      },
      {
          "AR", "Argentina"
      },
      {
          "AM", "Armenia"
      },
      {
          "AW", "Aruba"
      },
      {
          "AU", "Australia"
      },
      {
          "AT", "Austria"
      },
      {
          "AZ", "Azerbaijan"
      },
      {
          "BS", "Bahamas (the)"
      },
      {
          "BH", "Bahrain"
      },
      {
          "BD", "Bangladesh"
      },
      {
          "BB", "Barbados"
      },
      {
          "BY", "Belarus"
      },
      {
          "BE", "Belgium"
      },
      {
          "BZ", "Belize"
      },
      {
          "BJ", "Benin"
      },
      {
          "BM", "Bermuda"
      },
      {
          "BT", "Bhutan"
      },
      {
          "BO", "Bolivia (Plurinational State of)"
      },
      {
          "BQ", "Bonaire, Sint Eustatius and Saba"
      },
      {
          "BA", "Bosnia and Herzegovina"
      },
      {
          "BW", "Botswana"
      },
      {
          "BV", "Bouvet Island"
      },
      {
          "BR", "Brazil"
      },
      {
          "IO", "British Indian Ocean Territory (the)"
      },
      {
          "BN", "Brunei Darussalam"
      },
      {
          "BG", "Bulgaria"
      },
      {
          "BF", "Burkina Faso"
      },
      {
          "BI", "Burundi"
      },
      {
          "CV", "Cabo Verde"
      },
      {
          "KH", "Cambodia"
      },
      {
          "CM", "Cameroon"
      },
      {
          "CA", "Canada"
      },
      {
          "KY", "Cayman Islands (the)"
      },
      {
          "CF", "Central African Republic (the)"
      },
      {
          "TD", "Chad"
      },
      {
          "CL", "Chile"
      },
      {
          "CN", "China"
      },
      {
          "CX", "Christmas Island"
      },
      {
          "CC", "Cocos (Keeling) Islands (the)"
      },
      {
          "CO", "Colombia"
      },
      {
          "KM", "Comoros (the)"
      },
      {
          "CD", "Congo (the Democratic Republic of the)"
      },
      {
          "CG", "Congo (the)"
      },
      {
          "CK", "Cook Islands (the)"
      },
      {
          "CR", "Costa Rica"
      },
      {
          "HR", "Croatia"
      },
      {
          "CU", "Cuba"
      },
      {
          "CW", "Curaçao"
      },
      {
          "CY", "Cyprus"
      },
      {
          "CZ", "Czechia"
      },
      {
          "CI", "Côte d'Ivoire"
      },
      {
          "DK", "Denmark"
      },
      {
          "DJ", "Djibouti"
      },
      {
          "DM", "Dominica"
      },
      {
          "DO", "Dominican Republic (the)"
      },
      {
          "EC", "Ecuador"
      },
      {
          "EG", "Egypt"
      },
      {
          "SV", "El Salvador"
      },
      {
          "GQ", "Equatorial Guinea"
      },
      {
          "ER", "Eritrea"
      },
      {
          "EE", "Estonia"
      },
      {
          "SZ", "Eswatini"
      },
      {
          "ET", "Ethiopia"
      },
      {
          "FK", "Falkland Islands (the) [Malvinas]"
      },
      {
          "FO", "Faroe Islands (the)"
      },
      {
          "FJ", "Fiji"
      },
      {
          "FI", "Finland"
      },
      {
          "FR", "France"
      },
      {
          "GF", "French Guiana"
      },
      {
          "PF", "French Polynesia"
      },
      {
          "TF", "French Southern Territories (the)"
      },
      {
          "GA", "Gabon"
      },
      {
          "GM", "Gambia (the)"
      },
      {
          "GE", "Georgia"
      },
      {
          "DE", "Germany"
      },
      {
          "GH", "Ghana"
      },
      {
          "GI", "Gibraltar"
      },
      {
          "GR", "Greece"
      },
      {
          "GL", "Greenland"
      },
      {
          "GD", "Grenada"
      },
      {
          "GP", "Guadeloupe"
      },
      {
          "GU", "Guam"
      },
      {
          "GT", "Guatemala"
      },
      {
          "GG", "Guernsey"
      },
      {
          "GN", "Guinea"
      },
      {
          "GW", "Guinea-Bissau"
      },
      {
          "GY", "Guyana"
      },
      {
          "HT", "Haiti"
      },
      {
          "HM", "Heard Island and McDonald Islands"
      },
      {
          "VA", "Holy See (the)"
      },
      {
          "HN", "Honduras"
      },
      {
          "HK", "Hong Kong"
      },
      {
          "HU", "Hungary"
      },
      {
          "IS", "Iceland"
      },
      {
          "IN", "India"
      },
      {
          "ID", "Indonesia"
      },
      {
          "IR", "Iran (Islamic Republic of)"
      },
      {
          "IQ", "Iraq"
      },
      {
          "IE", "Ireland"
      },
      {
          "IM", "Isle of Man"
      },
      {
          "IL", "Israel"
      },
      {
          "IT", "Italy"
      },
      {
          "JM", "Jamaica"
      },
      {
          "JP", "Japan"
      },
      {
          "JE", "Jersey"
      },
      {
          "JO", "Jordan"
      },
      {
          "KZ", "Kazakhstan"
      },
      {
          "KE", "Kenya"
      },
      {
          "KI", "Kiribati"
      },
      {
          "KP", "Korea (the Democratic People's Republic of)"
      },
      {
          "KR", "Korea (the Republic of)"
      },
      {
          "KW", "Kuwait"
      },
      {
          "KG", "Kyrgyzstan"
      },
      {
          "LA", "Lao People's Democratic Republic (the)"
      },
      {
          "LV", "Latvia"
      },
      {
          "LB", "Lebanon"
      },
      {
          "LS", "Lesotho"
      },
      {
          "LR", "Liberia"
      },
      {
          "LY", "Libya"
      },
      {
          "LI", "Liechtenstein"
      },
      {
          "LT", "Lithuania"
      },
      {
          "LU", "Luxembourg"
      },
      {
          "MO", "Macao"
      },
      {
          "MG", "Madagascar"
      },
      {
          "MW", "Malawi"
      },
      {
          "MY", "Malaysia"
      },
      {
          "MV", "Maldives"
      },
      {
          "ML", "Mali"
      },
      {
          "MT", "Malta"
      },
      {
          "MH", "Marshall Islands (the)"
      },
      {
          "MQ", "Martinique"
      },
      {
          "MR", "Mauritania"
      },
      {
          "MU", "Mauritius"
      },
      {
          "YT", "Mayotte"
      },
      {
          "MX", "Mexico"
      },
      {
          "FM", "Micronesia (Federated States of)"
      },
      {
          "MD", "Moldova (the Republic of)"
      },
      {
          "MC", "Monaco"
      },
      {
          "MN", "Mongolia"
      },
      {
          "ME", "Montenegro"
      },
      {
          "MS", "Montserrat"
      },
      {
          "MA", "Morocco"
      },
      {
          "MZ", "Mozambique"
      },
      {
          "MM", "Myanmar"
      },
      {
          "NA", "Namibia"
      },
      {
          "NR", "Nauru"
      },
      {
          "NP", "Nepal"
      },
      {
          "NL", "Netherlands (the)"
      },
      {
          "NC", "New Caledonia"
      },
      {
          "NZ", "New Zealand"
      },
      {
          "NI", "Nicaragua"
      },
      {
          "NE", "Niger (the)"
      },
      {
          "NG", "Nigeria"
      },
      {
          "NU", "Niue"
      },
      {
          "NF", "Norfolk Island"
      },
      {
          "MP", "Northern Mariana Islands (the)"
      },
      {
          "NO", "Norway"
      },
      {
          "OM", "Oman"
      },
      {
          "PK", "Pakistan"
      },
      {
          "PW", "Palau"
      },
      {
          "PS", "Palestine, State of"
      },
      {
          "PA", "Panama"
      },
      {
          "PG", "Papua New Guinea"
      },
      {
          "PY", "Paraguay"
      },
      {
          "PE", "Peru"
      },
      {
          "PH", "Philippines (the)"
      },
      {
          "PN", "Pitcairn"
      },
      {
          "PL", "Poland"
      },
      {
          "PT", "Portugal"
      },
      {
          "PR", "Puerto Rico"
      },
      {
          "QA", "Qatar"
      },
      {
          "MK", "Republic of North Macedonia"
      },
      {
          "RO", "Romania"
      },
      {
          "RU", "Russian Federation (the)"
      },
      {
          "RW", "Rwanda"
      },
      {
          "RE", "Réunion"
      },
      {
          "BL", "Saint Barthélemy"
      },
      {
          "SH", "Saint Helena, Ascension and Tristan da Cunha"
      },
      {
          "KN", "Saint Kitts and Nevis"
      },
      {
          "LC", "Saint Lucia"
      },
      {
          "MF", "Saint Martin (French part)"
      },
      {
          "PM", "Saint Pierre and Miquelon"
      },
      {
          "VC", "Saint Vincent and the Grenadines"
      },
      {
          "WS", "Samoa"
      },
      {
          "SM", "San Marino"
      },
      {
          "ST", "Sao Tome and Principe"
      },
      {
          "SA", "Saudi Arabia"
      },
      {
          "SN", "Senegal"
      },
      {
          "RS", "Serbia"
      },
      {
          "SC", "Seychelles"
      },
      {
          "SL", "Sierra Leone"
      },
      {
          "SG", "Singapore"
      },
      {
          "SX", "Sint Maarten (Dutch part)"
      },
      {
          "SK", "Slovakia"
      },
      {
          "SI", "Slovenia"
      },
      {
          "SB", "Solomon Islands"
      },
      {
          "SO", "Somalia"
      },
      {
          "ZA", "South Africa"
      },
      {
          "GS", "South Georgia and the South Sandwich Islands"
      },
      {
          "SS", "South Sudan"
      },
      {
          "ES", "Spain"
      },
      {
          "LK", "Sri Lanka"
      },
      {
          "SD", "Sudan (the)"
      },
      {
          "SR", "Suriname"
      },
      {
          "SJ", "Svalbard and Jan Mayen"
      },
      {
          "SE", "Sweden"
      },
      {
          "CH", "Switzerland"
      },
      {
          "SY", "Syrian Arab Republic"
      },
      {
          "TW", "Taiwan"
      },
      {
          "TJ", "Tajikistan"
      },
      {
          "TZ", "Tanzania, United Republic of"
      },
      {
          "TH", "Thailand"
      },
      {
          "TL", "Timor-Leste"
      },
      {
          "TG", "Togo"
      },
      {
          "TK", "Tokelau"
      },
      {
          "TO", "Tonga"
      },
      {
          "TT", "Trinidad and Tobago"
      },
      {
          "TN", "Tunisia"
      },
      {
          "TR", "Turkey"
      },
      {
          "TM", "Turkmenistan"
      },
      {
          "TC", "Turks and Caicos Islands (the)"
      },
      {
          "TV", "Tuvalu"
      },
      {
          "UG", "Uganda"
      },
      {
          "UA", "Ukraine"
      },
      {
          "AE", "United Arab Emirates (the)"
      },
      {
          "GB", "United Kingdom of Great Britain and Northern Ireland (the)"
      },

      {
          "UM", "United States Minor Outlying Islands (the)"
      },

      {
          "US", "United States of America (the)"
      },

      {
          "UY", "Uruguay"
      },

      {
          "UZ", "Uzbekistan"
      },

      {
          "VU", "Vanuatu"
      },

      {
          "VE", "Venezuela (Bolivarian Republic of)"
      },

      {
          "VN", "Viet Nam"
      },

      {
          "VG", "Virgin Islands (British)"
      },

      {
          "VI", "Virgin Islands (U.S.)"
      },

      {
          "WF", "Wallis and Futuna"
      },

      {
          "EH", "Western Sahara"
      },

      {
          "YE", "Yemen"
      },

      {
          "ZM", "Zambia"
      },

      {
          "ZW", "Zimbabwe"
      },

      {
          "AX", "Åland Islands"
      },
  };

 /// <summary>
 ///  Gets country name with TwoLetterISOCode.
 /// </summary>
 /// <param name="TwoLetterISOCode"></param>
 /// <returns></returns>
  public static string GetCountry(string TwoLetterISOCode)
  {
    var Locale = new CultureInfo(TwoLetterISOCode);
    string Country = CountryCodes[Locale.TwoLetterISOLanguageName.ToUpper()];
    return Country;
  }

  /// <summary>
  ///   Converts DateTime to logical time string.
  /// </summary>
  /// <param name="dt"></param>
  /// <returns></returns>
  public static string LogicalTime(this DateTime dt)
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

    if (span.Seconds > 5)
    {
      return $"{span.Seconds} Seconds";
    }

    return span.Seconds <= 5 ? "Instantly" : string.Empty;
  }

  /// <summary>
  ///   Converts TimeSpan to logical time string.
  /// </summary>
  /// <param name="dt"></param>
  /// <returns></returns>
  public static string LogicalTime(this TimeSpan dt)
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
    DiscordRole? ReturnRole = null;

    (_, DiscordRole? value) = guild.Roles.FirstOrDefault(x => x.Value.Name == Consts.QUARANTINE_ROLE_NAME);

    ReturnRole =
        value?.Name is null
            ? Create // If create is selected, create new role. Otherwise, return null.
                ? guild.CreateRoleAsync(Consts.QUARANTINE_ROLE_NAME, Consts.QuarantineRolePerm, DiscordColor.DarkRed).Result
                : value
            : value;

    opt.RoleID = ReturnRole?.Id ?? 0;
    return ReturnRole;

    #endregion

    return ReturnRole;
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
                    $"**⟩** You are successfully verified in **{e.Guild.Name}**. Now you can access to channels.",
                    DiscordColor.Purple,
                    Footer: "DeAuth")));

    // await e.Guild.GetMemberAsync(e.User.Id).Result.ReplaceRolesAsync(new[] {e.Guild.EveryoneRole});
    await e.Guild.GetMemberAsync(e.User.Id).Result.RevokeRoleAsync(GetConfig(e.Guild).GetQuarantineRole());
    opt.EditStatus(e.User.Id, Status.Verified);
    Log(e.Guild, await e.Guild.GetMemberAsync(e.User.Id), LogType.New_Verify);
  }

  #endregion

}