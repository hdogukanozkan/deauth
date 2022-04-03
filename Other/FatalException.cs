namespace DeAuth.Other;

[Serializable]
internal class FatalException : Exception
{

  public string _errorCode = "";

  public FatalException(string Code)
  {
    _errorCode = Code;
  }

}