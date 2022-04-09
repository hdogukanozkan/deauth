namespace DeAuth.Models;

public class Captcha
{

  /// <summary> Length of the captcha code. </summary>
  public int Length { get; set; } = 7;

  /// <summary> The operation that will be performed when captcha failed. </summary>
  public VerifyFail OnVerifyFail { get; set; } = VerifyFail.Nothing;

  /// <summary> The mode of captcha creation. </summary>
  public CaptchaMode Mode { get; set; } = CaptchaMode.Classic;

}

public enum CaptchaMode
{

  [ChoiceName("Classic | Numbers & Letters")]
  Classic = 0,
  [ChoiceName("Hard | With Symbols")] Hard = 1,

  [ChoiceName("No Captcha | Verify With Button")]
  NoCaptcha = 2

}

public enum VerifyFail
{

  [ChoiceName("Ban")]  Ban,
  [ChoiceName("Kick")] Kick,

  [ChoiceName("Do nothing. They will stay as Quarantined as long as not verified.")]
  Nothing

}