namespace DeAuth.Bot;

internal class Serializers
{

  /// <summary>
  ///   Deserializes an XML string into an object.
  /// </summary>
  /// <param name="FilePath">File path to take source from xml file.</param>
  /// <returns>Deserialized XML Object.</returns>
  protected static List<Config> DeSerialize(string FilePath = "Config.xml")
  {
    var serializer = new XmlSerializer(typeof(List<Config>));

    using (var stream = new StreamReader(FilePath))
    {
      return (List<Config>) serializer.Deserialize(stream)!;
    }
  }

  /// <summary>
  ///   Serializes an object to an XML file
  /// </summary>
  private static void Serialize(string FilePath = "Config.xml")
  {
    try
    {
      File.Delete(FilePath);
    }
    catch ( Exception ex )
    {
      Console.WriteLine(ex.Message);
    }

    using (var sw = new StreamWriter(FilePath))
    {
      var xmlSerializer = new XmlSerializer(typeof(List<Config>));
      xmlSerializer.Serialize(sw, Consts.Config);
      sw.Close();
    }
  }

  #region Auto Save

  private static Timer? timer;

  /// <summary>
  ///   Creates a thread for autoSaveModule for config.
  /// </summary>
  protected static void BindAutoSave(TimeSpan Interval)
  {
    timer = new Timer(Interval.TotalMilliseconds);
    timer.AutoReset = true;

    timer.Elapsed += delegate { Serialize(); };
    timer.Start();
  }

  #endregion

}