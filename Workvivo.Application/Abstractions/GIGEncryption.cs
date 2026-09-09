namespace Workvivo.Application.Abstractions;
public static class GIGEncryption
{
    private static string _saltValueinitial = "G!g@2016"; // private key (Salt) 
    private static string initVectorintial = "G@l@xyShj20!7@94"; // private key  
    private static string passwordIterationsinitial = "2"; // how many encrupts will occure at one time
    public static string GetEnumDescription(Enum value)
    {
        DescriptionAttribute[] descriptionAttributeArray = (DescriptionAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (descriptionAttributeArray != null && descriptionAttributeArray.Length > 0)
            return descriptionAttributeArray[0].Description;
        return value.ToString();
    }
    public static string Encrypt(string plainText, string encryptionKey)
    {
        try
        {
            string saltValue = _saltValueinitial;
            string passPhrase = encryptionKey;
            hashAlgorithmEnum hashAlgorithm = hashAlgorithmEnum.SHA256;
            int passwordIterations = int.Parse(passwordIterationsinitial);
            string initVectorin = initVectorintial;
            keySizeEnum keySize = keySizeEnum.High;
            initVectorCls initVectorCls = new initVectorCls(initVectorin);
            initsaltValueCls initsaltValueCls = new initsaltValueCls(saltValue);
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVectorCls.InitVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(initsaltValueCls.InitsaltValue);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] password = new PasswordDeriveBytes(passPhrase, saltValueBytes, GetEnumDescription(hashAlgorithm), passwordIterations).GetBytes((int)keySize / 8);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(password, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            var x = BitConverter.ToString(cipherTextBytes).Replace("-", "");
            return x;
        }
        catch
        {
            throw new ArgumentException("Unknown Error!");
        }
    }
    public static string Decrypt(string cipherText, string encryptionKey)
    {
        try
        {

            string saltValue = _saltValueinitial;
            string passPhrase = encryptionKey;
            hashAlgorithmEnum hashAlgorithm = hashAlgorithmEnum.SHA256;
            int passwordIterations = int.Parse(passwordIterationsinitial);
            string initVectorin = initVectorintial;
            keySizeEnum keySize = keySizeEnum.High;
            initVectorCls initVectorCls = new initVectorCls(initVectorin);
            initsaltValueCls initsaltValueCls = new initsaltValueCls(saltValue);
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVectorCls.InitVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(initsaltValueCls.InitsaltValue);
            byte[] cipherTextBytes = StringToByteArray(cipherText);


            byte[] password = new PasswordDeriveBytes(passPhrase, saltValueBytes, GetEnumDescription(hashAlgorithm), passwordIterations).GetBytes((int)keySize / 8);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Mode = CipherMode.CBC;
            // rijndaelManaged.Padding = PaddingMode.None;


            using (var input = new MemoryStream(cipherTextBytes))
            using (var output = new MemoryStream())
            {
                var Newdecryptor = rijndaelManaged.CreateDecryptor(password, initVectorBytes);
                using (var cryptStream = new CryptoStream(input, Newdecryptor, CryptoStreamMode.Read))
                {
                    var buffer = new byte[cipherTextBytes.Length];
                    var read = cryptStream.Read(buffer, 0, buffer.Length);
                    var plainText = Encoding.UTF8.GetString(buffer);
                    while (read > 0)
                    {
                        output.Write(buffer, 0, read);
                        read = cryptStream.Read(buffer, 0, buffer.Length);
                    }
                    cryptStream.Flush();
                    var result = Encoding.UTF8.GetString(output.ToArray());
                    return result;

                }
            }

        }

        catch
        {
            throw new ArgumentException("Incorrect Cipher Text or Incorrect Password");
        }
    }
    enum keySizeEnum
    {
        Low = 128,
        Medium = 192,
        High = 256,
    }

    enum hashAlgorithmEnum
    {
        [Description("MD5")]
        MD5,
        [Description("SHA1")]
        SHA1,
        [Description("SHA256")]
        SHA256,
    }
    static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }

}

public class initVectorCls
{
    private string initVector;

    public string InitVector
    {
        get
        {
            return initVector;
        }
    }

    public initVectorCls(string initVector)
    {
        if (initVector.Length != 16)
            throw new ArgumentException("Parameter Must Be String With Exactly 16 ASCII Characters Long", "initVector");
        this.initVector = initVector;
    }
}

class initsaltValueCls
{
    private string saltValue;

    public string InitsaltValue
    {
        get
        {
            return saltValue;
        }
    }

    public initsaltValueCls(string saltValue)
    {
        if (saltValue.Length < 8)
            throw new ArgumentException("Parameter Must Be String With minimum of 8 ASCII Characters Long", "saltValue");
        this.saltValue = saltValue;
    }
}
