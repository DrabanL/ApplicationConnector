using System.Security.Cryptography;
using System.Text;

namespace RabanSoft.ApplicationConnector.DataTransformers
{
    public abstract class TripleDESCryptoDataTransformer : CryptoDataTransformer
    {
        public virtual CipherMode CipherMode { get; set; } = CipherMode.ECB;

        public virtual PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;

        public override SymmetricAlgorithm GetCryptoProvider()
        {
            using (var hashProvider = new MD5CryptoServiceProvider())
                return new TripleDESCryptoServiceProvider()
                {
                    Mode = CipherMode,
                    Padding = PaddingMode,
                    Key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(SecretKey))
                };
        }
    }
}
