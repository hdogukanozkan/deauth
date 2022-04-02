namespace DeAuth.Other;

public class ConfigManager
{

	/// <summary>
	///   Imports a config from encrypted json string to guild.
	/// </summary>
	/// <param name="GuildToWrite"></param>
	/// <param name="Cfg"></param>
	/// <param name="Key"></param>
	public static void ImportConfig(ulong GuildToWrite, Config Cfg, string Key)
  {
    Config template = ReceiveConfig(Key).Config;
    template.GuildID = GuildToWrite;
    template.RoleID = 0;
    template.LogChannel = 0;
    template.VerifyChannel = 0;
    template.Enabled = false;

    int index = Consts.Config.IndexOf(Cfg);
    Consts.Config[index] = template;
  }

	/// <summary>
	///   Creates a encrypted data to make able servers share their config to other servers.
	/// </summary>
	/// <param name="Config">Config to share with.</param>
	/// <param name="Sharer">The user id of sharer.</param>
	/// <returns>A encrypted AES256 json object.</returns>
	public static string SendConfig(Config Config, ulong Sharer)
  {
    var ShareData = new SharedConfig
    {
        Config = Config,
        CreatedOn = DateTime.Now,
        UserID = Sharer
    };

    string json = JsonConvert.SerializeObject(ShareData);
    string encrypted = Encryption.Encrypt(json, Consts.AES_KEY);
    return encrypted;
  }

	/// <summary>
	///   Decryptes a encrypted json based class.
	/// </summary>
	/// <param name="EncryptedConfigString">A encrypted json object to decrypt.</param>
	/// <returns>Decrypt-safe config class.</returns>
	public static SharedConfig? ReceiveConfig(string EncryptedConfigString)
  {
    string Decypted = Encryption.Decrypt(EncryptedConfigString, Consts.AES_KEY);
    var extracted = JsonConvert.DeserializeObject<SharedConfig>(Decypted);
    return extracted;
  }

}