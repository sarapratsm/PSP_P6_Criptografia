using System.Security.Cryptography;

namespace Utils.Cripto.Asimetric
{
    /// <summary>
    /// Classe d'utilitat per a xifrat assimètric RSA.
    /// S'usa únicament per intercanviar la clau simètrica de forma segura.
    /// Tots els mètodes són estàtics perquè no necessiten guardar estat.
    /// </summary>
    public class RSA_Utils
    {
        /// <summary>
        /// Genera un parell de claus RSA aleatories de 2048 bits en cada sessió.
        /// </summary>
        /// <returns>
        /// Tupla amb (clauPublica, clauPrivada) en format XML.
        /// La clau pública es pot enviar lliurement per la xarxa.
        /// La clau privada mai ha de sortir del servidor.
        /// </returns>
        public static (string clauPublica, string clauPrivada) GenerarParellClaus()
        {
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            string clauPublica = rsa.ToXmlString(false); // false = només pública
            string clauPrivada = rsa.ToXmlString(true);  // true = pública + privada
            return (clauPublica, clauPrivada);
        }

        /// <summary>
        /// Xifra dades amb la clau pública del receptor.
        /// Només el posseïdor de la clau privada corresponent podrà desxifrar-les.
        /// </summary>
        public static byte[] Xifrar(byte[] dades, string clauPublicaXml)
        {
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.FromXmlString(clauPublicaXml);
            // OaepSHA256 és el padding recomanat avui (el Pkcs1 antic té vulnerabilitats conegudes)
            return rsa.Encrypt(dades, RSAEncryptionPadding.OaepSHA256);
        }

        /// <summary>
        /// Desxifra dades xifrades amb la nostra clau pública, usant la clau privada.
        /// </summary>
        public static byte[] Desxifrar(byte[] dadesXifrades, string clauPrivadaXml)
        {
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.FromXmlString(clauPrivadaXml);
            return rsa.Decrypt(dadesXifrades, RSAEncryptionPadding.OaepSHA256);
        }
    }
}
