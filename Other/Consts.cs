namespace DeAuth.Other;

public static class Consts
{

  public const string AES_KEY = "b17c15198a4341c3bbce4ea6915a9d10";

  public const Permissions QuarantineRolePerm = Permissions.None; // Qaarentine 

  public const string VERIFY_COMPONENT_ID   = "deauth-verify";                   // Verify component id | button
  public const string QUARANTINE_ROLE_NAME  = "Quarantine";                      // Quarantine role name
  public const string VERIFY_CHANNEL_NAME   = "verify";                          // Verify channel name that will created
  public const string DOCUMENTATION_GITBOOK = "https://arsh3.gitbook.io/deauth"; // Documentation gitbook

  /// <summary> Calculates the joining time of suspicious users. </summary>
  public static readonly ConcurrentDictionary<DiscordGuild, DateTimeOffset> SuspiciousCounter = new();

  /// <summary>
  ///   Handles the cooldown times of verification button. Stores <b>UserID as ULONG | Time as DateTimeOffset</b>
  /// </summary>
  public static readonly ConcurrentDictionary<ulong, DateTimeOffset> ButtonCooldown = new();

  /// <summary> Holds the whole client server configs. </summary>
  public static List<Config> Config = new();

}