namespace DeAuth.Models;

/// <summary>
///   Holds the shared config data.
/// </summary>
public class ConfigTemplate
{

  public Config   Config    { get; set; }
  public ulong    UserID    { get; set; }
  public DateTime CreatedOn { get; set; }

}