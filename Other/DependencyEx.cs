namespace DeAuth.Other;

[Serializable]
internal class DependencyEx : Exception
{

  public DependencyEx()
  {
  }

  public DependencyEx(string ErrorDesc)
      : base(string.Format(ErrorDesc))
  {
  }

}