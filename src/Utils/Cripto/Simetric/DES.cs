using System.Security.Cryptography;

namespace Utils.Cripto.Simetric
{
    public class DES : IXifradorSimetric
    {
        #region IXifradorSimetric
        public byte[] Xifrar(byte[] raw_data, byte[] key)
        {
            ValidarClau(key);
            using var des = System.Security.Cryptography.DES.Create();
            des.Key = key;
            des.IV = key;
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(raw_data, 0, raw_data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public byte[] Desxifrar(byte[] encrypted_data, byte[] key)
        {
            ValidarClau(key);
            using var des = System.Security.Cryptography.DES.Create();
            des.Key = key;
            des.IV = key;
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(encrypted_data, 0, encrypted_data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
        #endregion

        #region Validació
        private void ValidarClau(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (key.Length != 8)
                throw new ArgumentException("DES requereix una clau de 8 bytes.", nameof(key));
        }
        #endregion
    }
}
