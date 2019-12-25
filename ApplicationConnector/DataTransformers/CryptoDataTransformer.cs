using System;
using System.Security.Cryptography;

namespace RabanSoft.ApplicationConnector.DataTransformers
{
    public abstract class CryptoDataTransformer : IDataTransformer, IDisposable
    {
        private readonly ICryptoTransform _decryptor;
        private readonly ICryptoTransform _encryptor;

        public abstract SymmetricAlgorithm GetCryptoProvider();

        public abstract string SecretKey { get; set; }

        public CryptoDataTransformer()
        {
            using (var cryptoProvider = GetCryptoProvider())
            {
                _decryptor = cryptoProvider.CreateDecryptor();
                _encryptor = cryptoProvider.CreateEncryptor();
            }
        }

        byte[] IDataTransformer.TransformIn(byte[] data, int offset, int count) => _decryptor.TransformFinalBlock(data, offset, count);

        byte[] IDataTransformer.TransformOut(byte[] data, int offset, int count) => _encryptor.TransformFinalBlock(data, offset, count);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    _decryptor?.Dispose();
                    _encryptor?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TripleDESDataTransformer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
