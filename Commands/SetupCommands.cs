namespace DeAuth.Commands;

[SlashRequirePermissions(Permissions.Administrator)] // check for the bot also
[SlashCommandGroup("setup", "Setup verification.")]
public class SetupCommands : ApplicationCommandModule
{

  [SlashCommand("create", "Creates a new verification panel to new channel.")]
  public static async Task Create
  (
      InteractionContext c,
      [Option("title", "Title of panel embed.")]
      string? panelTitle = "Verify",
      [Option("description", "Description of embed panel")]
      string? panelDescription = "To gain access in this server, pass the captcha verification.",
      [Option("button", "Text of the verification button.")]
      string? buttonText = "Verify")
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config config = Utils.GetConfig(c.Guild);

    if (config.Enabled) // Already Initialized
    {
      await Builders.Edit(c, "Already Activated", "・Seems like you are already enabled the verification panel " +
                                                  $"(<#{config.VerifyChannel}>). If you want reset it use `/setup remove`.");
      return;
    }

    await c.EditResponseAsync(new DiscordWebhookBuilder()
                              .AddEmbed(Builders.BuildEmbed(c.Member, "Create Verification",
                                  "**⟩** Do you want to create new **verification** panel and enable the verification?"))
                              .AddComponents(new DiscordButtonComponent(ButtonStyle.Success,
                                  "verify_continue", "Create"))
                              .AddComponents(new DiscordButtonComponent(ButtonStyle.Danger,
                                  "verify_cancel", "Abort")));

    InteractivityExtension? Interactivity = c.Client.GetInteractivity();

    var ButtonResult = Interactivity.WaitForButtonAsync(await c.GetOriginalResponseAsync(), c.User)
                                    .GetAwaiter().GetResult();

    if (ButtonResult.Result == null)
    {
      await Builders.Edit(c, "Timeout", "🔸 You was too late. But you can try again!");
      return;
    }

    if (ButtonResult.Result.Id == "verify_cancel")
    {
      await Builders.Edit(c, "Canceled", "🔸 You canceled the setup.");
      return;
    }

    await Builders.Edit(c, "Creating", "**⟩** Creating panel components...");
    DiscordChannel panelChannel;

    try
    {
      panelChannel = await Utils.CreatePanel(c.Guild, panelTitle, panelDescription, buttonText);
    }
    catch
    {
      throw new DException("Hmm?", "Something went wrong while creating the verification panel. Are you sure i have the permissions to create channels?");
    }

    await Builders.Edit(c, "Enabled", $"🔹 **Verification** successfully enabled. ({panelChannel.Mention})\n" +
                                      $"・ See the [Docs]({Consts.DOCUMENTATION_GITBOOK}) for configuration **docs**.");

    Serializers.Serialize();
  }

  [VerificationDependency("・Verification not enabled already.\n")]
  [SlashCommand("remove", "Removes verification from server.")]
  public static async Task Remove(InteractionContext c)
  {
    await c.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
        new DiscordInteractionResponseBuilder().AsEphemeral());

    Config guild = Utils.GetConfig(c.Guild);

    await c.EditResponseAsync(new DiscordWebhookBuilder()
                              .AddEmbed(Builders.BuildEmbed(c.Member, "Clear",
                                  "・Are you sure to disable verification?", DiscordColor.Red))
                              .AddComponents(new DiscordButtonComponent(ButtonStyle.Success,
                                  "clear_continue", "Remove"))
                              .AddComponents(new DiscordButtonComponent(ButtonStyle.Danger,
                                  "clear_cancel", "Abort")));

    InteractivityExtension? Interactivity = c.Client.GetInteractivity();

    var ButtonResult = Interactivity.WaitForButtonAsync(await c.GetOriginalResponseAsync(), c.User)
                                    .GetAwaiter().GetResult();

    if (ButtonResult.Result == null)
    {
      await Builders.Edit(c, "Timeout", "🔸 You was too late. But you can try again!");
      return;
    }

    if (ButtonResult.Result.Id == "clear_cancel")
    {
      await Builders.Edit(c, "Canceled", "🔸 You canceled the clear process.");
      return;
    }

    await Builders.Edit(c, "Cleaning UP", "**⟩** Cleaning up verification components...");
    await Utils.RemovePanel(c.Guild);
    guild.Reload(); 
    await Builders.Edit(c, "Cleared", "🔹 Verification has been successfully removed from server. All verification data has been removed.");

    Serializers.Serialize();
  }

}