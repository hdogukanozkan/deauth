namespace DeAuth.Models;

/// <summary>
///   Prepares a private config for guild.
/// </summary>
public class Config
{

  /// <summary>  Whather verification is enabled or not. </summary>
  public bool Enabled { get; set; } = false;

  /// <summary>  The message to send user when new member is joined. </summary>
  public string? WelcomeMessage { get; set; } = null;

  /// <summary> Captcha options. </summary>
  public Captcha CaptchaOptions { get; set; } = new();

  /// <summary> Kind of users to quarantine it. </summary>
  public QuarantineType QuarantineType { get; set; } = QuarantineType.Both;

  /// <summary> Performed action list. </summary>
  public List<UserStatus> Attempts { get; set; } = new();

  #region Modules

  /// <summary>  Whather anti raid is enabled. </summary>
  public bool AntiRaid { get; set; } = false;

  /// <summary> Local language of server. Should used for country-disallower. </summary>
  public string? Locale { get; set; } = null;

  /// <summary> The limit of account creation timestampt. </summary>
  public DateTime? AgeLimit { get; set; } = null;

  /// <summary> Whather server is locked to verify. </summary>
  public LockMode? LockMode { get; set; } = null;

  #endregion

  #region Components

  /// <summary> Main guild ID. </summary>
  public ulong GuildID { get; set; } = 0;

  /// <summary> Quarantine role ID. </summary>
  public ulong RoleID { get; set; } = 0;

  /// <summary> Verification channel ID. </summary>
  public ulong VerifyChannel { get; set; } = 0;

  /// <summary> Logging Channel ID </summary>
  public ulong LogChannel { get; set; } = 0;

  #endregion

}