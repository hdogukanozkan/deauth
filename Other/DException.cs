namespace DeAuth.Other;

[Serializable]
internal class DException : Exception
{

  public string error_desc;

  public string error_title;

  public DException(string title, string errorDesc)
  {
    error_title = title;
    error_desc = errorDesc;
  }

}