namespace DeAuth.Modules;

public static class Builders
{

  #region Builders

  /// <summary>
  ///   Edits the interaction with custom embed.
  /// </summary>
  public static async Task Edit(InteractionContext c, DiscordEmbed Embed)
  {
    DiscordWebhookBuilder? builder = new DiscordWebhookBuilder().AddEmbed(Embed);
    await c.EditResponseAsync(builder);
  }

  /// <summary>
  ///   Edits the interaction with custom embed.
  /// </summary>
  public static async Task Edit(InteractionContext c, string Title, string Description)
  {
    DiscordWebhookBuilder? builder = new DiscordWebhookBuilder().AddEmbed(BuildEmbed(c.Member, Title, Description));
    await c.EditResponseAsync(builder);
  }

  /// <summary>
  ///   Waits the next message from user.
  ///   null.
  /// </summary>
  public static async Task<DiscordMessage?> WaitMessage
  (
      InteractionContext c,
      string Title,
      string Desc,
      int Timeout = 10)
  {
    await Edit(c, Title, Desc);

    var r = await c.Client.GetInteractivity()
                   .WaitForMessageAsync(x => x.Author == c.User, TimeSpan.FromSeconds(Timeout));

    return r.TimedOut ? null : r.Result;
  }

  /// <summary>
  ///   Waits to user click to button with pre-has embed. Returns the ID of clicked button, or if timed out will return
  ///   null.
  /// </summary>
  public static async Task<string?> WaitButton
  (
      InteractionContext c,
      string Title,
      string Desc,
      int Timeout,
      IEnumerable<DiscordButtonComponent> Buttons)
  {
    await c.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(
        BuildEmbed(c.Member, Title, Desc)).AddComponents(Buttons));

    var r = await c.Client.GetInteractivity()
                   .WaitForButtonAsync(await c.GetOriginalResponseAsync(), new TimeSpan(0, 0, Timeout));
    return r.TimedOut ? null : r.Result.Id;
  }

  /// <summary>
  ///   Waits to user click to any of dropdown buttons. Returns the ID of clicked button, or if timed out will return null.
  /// </summary>
  public static async Task<InteractivityResult<ComponentInteractionCreateEventArgs>> WaitDropdown
  (
      string Title,
      string Description,
      DiscordSelectComponent Dropdowns,
      InteractionContext c,
      int Timeout = 10)
  {
    await c.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(BuildEmbed(
        c.Member, Title, Description, DiscordColor.Gold)).AddComponents(Dropdowns));

    var r = await c.Client.GetInteractivity()
                   .WaitForSelectAsync(await c.GetOriginalResponseAsync(), x =>
                       x.User == c.User, TimeSpan.FromSeconds(Timeout));

    return r;
  }

  #region Embed Builders

  /// <summary>
  ///   Creates the embed with basic arguments.
  /// </summary>
  public static DiscordEmbed BuildEmbed
  (
      DiscordUser? Executed,
      string Title,
      string Description,
      DiscordColor? Color = null,
      string? alternativeUrl = null,
      string Footer = "DeAuth")
  {
    var eb = new DiscordEmbedBuilder
    {
        Title = Title,
        Description = Description,
        Footer = new DiscordEmbedBuilder.EmbedFooter {Text = Footer},
        Color = Color ?? DiscordColor.Yellow
    };

    return eb.Build();
  }

  /// <summary>
  ///   Creates the embed with basic arguments.
  /// </summary>
  public static DiscordEmbed BuildEmbed
  (
      DiscordMember? Executed,
      string Title,
      string Description,
      DiscordColor? Color = null,
      bool IncludeTimestamp = false)
  {
    var eb = new DiscordEmbedBuilder
    {
        Title = Title,
        Description = Description,
        Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "DeAuth"},
        Color = Color ?? DiscordColor.Yellow,
        Author = new DiscordEmbedBuilder.EmbedAuthor {IconUrl = Executed?.AvatarUrl, Name = Executed?.DisplayName}
    };

    if (IncludeTimestamp) eb.Timestamp = DateTimeOffset.Now;
    return eb.Build();
  }

  /// <summary>
  ///   Creates so basic embed without author header.
  /// </summary>
  public static DiscordEmbed BasicEmbed(string Title, string Description, DiscordColor? Color = null, string? Footer = null)
  {
    var eb = new DiscordEmbedBuilder
    {
        Title = Title,
        Description = Description,
        Footer = new DiscordEmbedBuilder.EmbedFooter {Text = Footer ?? "DeAuth"},
        Color = Color ?? DiscordColor.Blue
    };

    eb.WithTimestamp(DateTime.Now);
    return eb.Build();
  }

  public static string GenerateCaptcha(CaptchaMode Mode, int Length)
  {
    // Mode 1 = Numbers | Letters only
    char[] main = "QWERTYUOPLKJHGFDSAZXCVBNM1234567890".ToCharArray();

    // Mode 2 = Numbers | Letters | Symbols
    if ((int) Mode > 0)
    {
      char[] symbols = "!@#$%^&*()_+{}[]|:;<>?,./".ToCharArray();
      main = main.Concat(symbols).ToArray();
    }

    #region Randomize array of chars

    // generate random string of main char[]
    var random = new Random();
    var chars = new char[Length];

    for ( var i = 0; i < Length; i++ ) chars[i] = main[random.Next(0, main.Length)];

    return new string(chars);

    #endregion
  }

  #endregion

  #endregion

}