using System.Net.Sockets;
using Utils.Cripto.Simetric;

namespace Client.Dades
{
    public class ConnexioClient
    {
        #region Singleton
        public static ConnexioClient Instancia { get; } = new();
        private ConnexioClient() { }
        #endregion

        #region Estat de connexió
        public TcpClient? TcpClient { get; private set; }
        public NetworkStream? Stream { get; private set; }
        public bool EstaConnectat => TcpClient?.Connected ?? false;
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
