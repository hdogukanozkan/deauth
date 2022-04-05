namespace DeAuth.Models;

/// <summary>
///   Flags command as verification must be turned on to use.
/// </summary>
public class VerificationDependency : SlashCheckBaseAttribute
{

  public readonly string _errormessage;

  public VerificationDependency
  (
      string ErrorMessage = "・Looks like verification is disabled on this server. This command requires verification to be enabled.\n" +
                            "・Please enable it by running command `/setup create`.")
  {
    _errormessage = ErrorMessage;
  }

  public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
  {
    Config guild = Utils.GetConfig(ctx.Guild);
    return guild.Enabled;
  }

}