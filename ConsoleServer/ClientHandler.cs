using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Utils.Cripto.Asimetric;
using Utils.Cripto.Simetric;
using Utils.Tcp;

namespace ConsoleServer
{
    public class ClientHandler
    {
        #region Estat compartit (static)
        private static readonly List<ClientHandler> _clientsActius = new();
        private static readonly List<string> _historial = new();
        private static readonly object _lock = new();
        private const int MAX_HISTORIAL = 20;
        #endregion

        #region Estat individual del client
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly IXifradorSimetric _xifrador;
        private readonly string _clauPublicaRSA;
        private readonly string _clauPrivadaRSA;
        private readonly AlgorismeSimetric _algorisme;
        private byte[] _clauSimetrica = Array.Empty<byte>();
        private string _nick = string.Empty;
        #endregion

        #region Constructor
        public ClientHandler(TcpClient client, AlgorismeSimetric algorisme, string clauPublicaRSA, string clauPrivadaRSA)
        {
            _tcpClient = client;
            _stream = client.GetStream();
            _algorisme = algorisme;
            _clauPublicaRSA = clauPublicaRSA;
            _clauPrivadaRSA = clauPrivadaRSA;

            _xifrador = algorisme switch
            {
                AlgorismeSimetric.DES => new DES(),
                AlgorismeSimetric.RC4 => new Rc4(),
                _ => throw new NotSupportedException($"Algorisme {algorisme} no suportat.")
            };
        }
        #endregion

        #region Flux principal
        public async Task HandleClientAsync()
        {
            try
            {
                if (!await NegociarAsync())
                {
                    Console.WriteLine($"[-] Negociació fallida. Tancant connexió.");
                    return;
                }

                lock (_lock) { _clientsActius.Add(this); }
                await EnviarHistorialAsync();
                Console.WriteLine($"[✓] {_nick} al xat. Clients: {_clientsActius.Count}");

                await LlegirMissatgesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {_nick}: {ex.Message}");
            }
            finally
            {
                DesconnectarClient();
            }
        }
        #endregion

        #region Negociació
        private async Task<bool> NegociarAsync()
        {
            var connect = await LlegirPaquetAsync();
            if (connect?.Tipus != "CONNECT") return false;
            _nick = connect.Nickname;
            Console.WriteLine($"    Nick: {_nick}");

            int nClients;
            lock (_lock) { nClients = _clientsActius.Count; }

            await EnviarPaquetAsync(new PaquetComunicacio
            {
                Tipus = "NEGOTIATE",
                XifratTriat = _algorisme,
                Contingut = nClients.ToString(),
                DadesClauCriptografica = _clauPublicaRSA
            });

            var resposta = await LlegirPaquetAsync();
            if (resposta?.Tipus != "OK") return false;

            if (string.IsNullOrEmpty(resposta.DadesClauCriptografica))
            {
                Console.WriteLine($"[!] {_nick} no ha enviat la clau simètrica.");
                return false;
            }

            _clauSimetrica = RSA_Utils.Desxifrar(
                Convert.FromBase64String(resposta.DadesClauCriptografica),
                _clauPrivadaRSA
            );
            Console.WriteLine($"    Clau simètrica rebuda ({_clauSimetrica.Length} bytes).");

            return true;
        }
        #endregion

        #region Loop de missatges
        private async Task LlegirMissatgesAsync()
        {
            while (true)
            {
                var paquet = await LlegirPaquetAsync();
                if (paquet == null || paquet.Tipus == "BYE") break;

                if (paquet.Tipus == "MESSAGE")
                {
                    byte[] xifrat = Convert.FromBase64String(paquet.Contingut);
                    byte[] clar = _xifrador.Desxifrar(xifrat, _clauSimetrica);
                    string text = Encoding.UTF8.GetString(clar);

                    string missatgeFormatat = $"[{DateTime.Now:HH:mm:ss}] {_nick}: {text}";
                    Console.WriteLine($"    {missatgeFormatat}");

                    lock (_lock)
                    {
                        _historial.Add(missatgeFormatat);
                        if (_historial.Count > MAX_HISTORIAL)
                            _historial.RemoveAt(0);
                    }

                    await BroadcastAsync(missatgeFormatat);
                }
            }
        }
        #endregion

        #region Helpers
        private async Task EnviarHistorialAsync()
        {
            List<string> copia;
            lock (_lock) { copia = new List<string>(_historial); }
            foreach (string m in copia)
                await EnviarMissatgeXifratAsync(m);
        }

        private async Task BroadcastAsync(string missatgeClar)
        {
            List<ClientHandler> copia;
            lock (_lock) { copia = new List<ClientHandler>(_clientsActius); }
            foreach (var client in copia)
            {
                try { await client.EnviarMissatgeXifratAsync(missatgeClar); }
                catch { }
            }
        }

        public async Task EnviarMissatgeXifratAsync(string missatgeClar)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(missatgeClar);
            byte[] xifrat = _xifrador.Xifrar(bytes, _clauSimetrica);
            await EnviarPaquetAsync(new PaquetComunicacio
            {
                Tipus = "MESSAGE",
                Contingut = Convert.ToBase64String(xifrat)
            });
        }

        private void DesconnectarClient()
        {
            lock (_lock) { _clientsActius.Remove(this); }
            _tcpClient.Close();
            Console.WriteLine($"[-] {_nick} desconnectat. Clients: {_clientsActius.Count}");
        }
        #endregion

        #region Transport (protocol: 4 bytes longitud + JSON)
        private async Task EnviarPaquetAsync(PaquetComunicacio paquet)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(paquet);
            byte[] longitud = BitConverter.GetBytes(json.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(longitud);
            await _stream.WriteAsync(longitud);
            await _stream.WriteAsync(json);
        }

        private async Task<PaquetComunicacio?> LlegirPaquetAsync()
        {
            byte[] bufLong = new byte[4];
            if (await LlegirExactamentAsync(bufLong, 4) == 0) return null;
            if (BitConverter.IsLittleEndian) Array.Reverse(bufLong);
            int longitud = BitConverter.ToInt32(bufLong, 0);

            byte[] bufJson = new byte[longitud];
            if (await LlegirExactamentAsync(bufJson, longitud) == 0) return null;
            return JsonSerializer.Deserialize<PaquetComunicacio>(bufJson);
        }

        private async Task<int> LlegirExactamentAsync(byte[] buffer, int nBytes)
        {
            int total = 0;
            while (total < nBytes)
            {
                int llegit = await _stream.ReadAsync(buffer, total, nBytes - total);
                if (llegit == 0) return 0;
                total += llegit;
            }
            return total;
        }
        #endregion
    }
}
