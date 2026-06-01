namespace Utils.Cripto.Simetric
{
    public class Rc4 : IXifradorSimetric
    {
        #region IXifradorSimetric
        public byte[] Xifrar(byte[] raw_data, byte[] key)
        {
            ValidarClau(key);
            byte[] estat = CrearVectorEstat();
            DesordenarEstat(estat, key);
            return ProcessarBytes(raw_data, estat);
        }

        public byte[] Desxifrar(byte[] encrypted_data, byte[] key)
            => Xifrar(encrypted_data, key); // RC4 és simètric: xifrar = desxifrar
        #endregion

        #region Algoritme intern RC4
        private byte[] CrearVectorEstat()
        {
            byte[] estat = new byte[256];
            for (int i = 0; i < 256; i++) estat[i] = (byte)i;
            return estat;
        }

        private void DesordenarEstat(byte[] estat, byte[] key)
        {
            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + estat[i] + key[i % key.Length]) % 256;
                (estat[i], estat[j]) = (estat[j], estat[i]);
            }
        }

        private byte[] ProcessarBytes(byte[] dades, byte[] estat)
        {
            byte[] resultat = new byte[dades.Length];
            int x = 0, y = 0;
            for (int i = 0; i < dades.Length; i++)
            {
                x = (x + 1) % 256;
                y = (y + estat[x]) % 256;
                (estat[x], estat[y]) = (estat[y], estat[x]);
                resultat[i] = (byte)(dades[i] ^ estat[(estat[x] + estat[y]) % 256]);
            }
            return resultat;
        }
        #endregion

        #region Validació
        private void ValidarClau(byte[] key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("La clau RC4 no pot estar buida.");
        }
        #endregion
    }
}
