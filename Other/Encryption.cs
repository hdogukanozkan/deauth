namespace DeAuth.Other;

/// <summary>
///   Utility class that handles encryption
/// </summary>
internal class Encryption
{

  /// <summary>
  ///   Encrypts a string with aes256 standards.
  /// </summary>
  /// <param name="plainText"></param>
  /// <param name="key"></param>
  /// <returns></returns>
  protected static string Encrypt(string plainText, string key)
  {
    var iv = new byte[16];
    byte[] array;

    using (var aes = Aes.Create())
    {
      aes.Key = Encoding.UTF8.GetBytes(key);
      aes.IV = iv;

      ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

      using (var memoryStream = new MemoryStream())
      {
        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        {
          using (var streamWriter = new StreamWriter(cryptoStream))
          {
            streamWriter.Write(plainText);
          }

          array = memoryStream.ToArray();
        }
      }
    }

    return Convert.ToBase64String(array);
  }

  protected static string Decrypt(string cipherText, string key)
  {
    var iv = new byte[16];
    byte[] buffer = Convert.FromBase64String(cipherText);

    using (var aes = Aes.Create())
    {
      aes.Key = Encoding.UTF8.GetBytes(key);
      aes.IV = iv;
      ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

      using (var memoryStream = new MemoryStream(buffer))
      {
        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
        {
          using (var streamReader = new StreamReader(cryptoStream))
          {
            return streamReader.ReadToEnd();
          }
        }
      }
    }
  }

}