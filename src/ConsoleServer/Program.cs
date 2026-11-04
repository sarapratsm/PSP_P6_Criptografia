using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Utils.Cripto.Asimetric;
using Utils.Cripto.Simetric;
using Utils.Tcp;

namespace ConsoleServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            #region Selecció d'algorisme
            string opcions = string.Join(" / ", Enum.GetNames(typeof(AlgorismeSimetric)));
            AlgorismeSimetric algorisme;

            while (true)
            {
                //Accepta el nom de l'algoritme o la seva posició (començant per 0)
                Console.Write($"Selecciona l'algorisme de xifrat simètric que haurà de generar el client ({opcions}): ");
                string? entrada = Console.ReadLine();
                if (Enum.TryParse(entrada, ignoreCase: true, out algorisme)) break;
                Console.WriteLine("Opció no vàlida. Torna-ho a intentar.\n");
            }
            #endregion

            #region Generació de claus RSA
            (string clauPublicaRSA, string clauPrivadaRSA) = RSA_Utils.GenerarParellClaus();
            #endregion

            #region Inici del servidor TCP
            TcpListener listener = new TcpListener(IPAddress.Any, ServerInfo.Port); //Any = accepta connexions de qualsevol IP
            listener.Start();
            Console.WriteLine($"\n[OK] Servidor iniciat al port {ServerInfo.Port}. Algorisme: {algorisme}");
            Console.WriteLine("Esperant clients...\n");
            #endregion

            #region Loop d'acceptació de clients
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"[+] Nova connexió des de: {client.Client.RemoteEndPoint}");

                _ = Task.Run(() =>
                    new ClientHandler(client, algorisme, clauPublicaRSA, clauPrivadaRSA)
                        .HandleClientAsync()
                );
            }
            #endregion
        }
    }
}
