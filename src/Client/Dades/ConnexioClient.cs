using System.Net.Sockets;
using Utils.Cripto.Simetric;

namespace Client.Dades
{
    public class ConnexioClient
    {
        #region Singleton
        public static ConnexioClient Instancia { get; } = new(); // Patró Singleton: només hi pot haver una instància de ConnexioClient a l'aplicació (1 unic socket connectat a la vegada).
        private ConnexioClient() { } // Constructor privat per evitar que es creïn més instàncies
        #endregion

        #region Estat de connexió
        public TcpClient? TcpClient { get; private set; } // Socket TCP que representa la connexió amb el servidor. Només es pot llegir i escriure mentre estigui connectat.
        public NetworkStream? Stream { get; private set; } // Stream de dades del socket TCP. Només es pot llegir i escriure mentre TcpClient estigui connectat.
        public bool EstaConnectat => TcpClient?.Connected ?? false; // Retorna true si TcpClient no és null i està connectat, false en cas contrari.
        #endregion

        #region Dades del client
        public string Username { get; set; } = string.Empty;
        public byte[] ClauSimetrica { get; set; } = Array.Empty<byte>();
        public IXifradorSimetric? Xifrador { get; private set; }

        private AlgorismeSimetric _algorisme;
        public AlgorismeSimetric Algorisme
        {
            get => _algorisme;
            set
            {
                /*definim el setter de manera que cada vegada que
                 * s'assigni un algorisme, s'inicialitzi 
                 * automaticament el xifrador amb la classe que 
                 * correspon, de manera que no hem de delegar al 
                 * programador(jo) enrecordar-se de fer-ho 
                 * (quina mandra)*/
                _algorisme = value;
                Xifrador = value switch
                {
                    AlgorismeSimetric.DES => new DES(),
                    AlgorismeSimetric.RC4 => new Rc4(),
                    _ => throw new NotSupportedException($"Algorisme {value} no suportat.")
                };
            }
        }
        #endregion

        #region Mètodes
        public void Connectar(string ip, int port)
        {
            TcpClient = new TcpClient(ip, port);
            Stream = TcpClient.GetStream();
        }

        public void Tancar()
        {
            Stream?.Close();
            TcpClient?.Close();
        }
        #endregion
    }
}
