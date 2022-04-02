namespace DeAuth.Models;

public class UserStatus
{

  /// <summary> ID of performed user. </summary>
  public ulong UserID { get; set; } = 0;

  /// <summary> A time of action perform. </summary>
  public DateTime Time { get; set; } = default;

  /// <summary> Verification status of user. </summary>
  public Status Status { get; set; } = Status.UnVerified;

}