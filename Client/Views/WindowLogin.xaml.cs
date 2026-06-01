using Client.Dades;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using Utils.Cripto.Asimetric;
using Utils.Cripto.Simetric;
using Utils.Tcp;

namespace Client.Views
{
    public partial class Login : Window
    {
        #region Constructor i càrrega
        public Login()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUser.Text = "User" + new Random().Next(100000, 999999);
        }
        #endregion

        #region Connexió
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            try
            {
                string nick = txtUser.Text.Trim();
                if (string.IsNullOrEmpty(nick))
                {
                    SetStatus("Introdueix un nom d'usuari.");
                    return;
                }

                ConnexioClient.Instancia.Username = nick;
                ConnexioClient.Instancia.Connectar(ServerInfo.ServerIp, ServerInfo.Port);

                await EnviarPaquetAsync(new PaquetComunicacio
                {
                    Tipus = "CONNECT",
                    Nickname = nick
                });

                var negotiate = await LlegirPaquetAsync();
                if (negotiate == null || negotiate.Tipus != "NEGOTIATE")
                    throw new Exception("Resposta inesperada del servidor.");

                if (!negotiate.XifratTriat.HasValue ||
                    !Enum.IsDefined(typeof(AlgorismeSimetric), negotiate.XifratTriat.Value))
                {
                    await EnviarPaquetAsync(new PaquetComunicacio { Tipus = "KO" });
                    throw new Exception($"Algorisme desconegut: {negotiate.XifratTriat}");
                }

                ConnexioClient.Instancia.Algorisme = negotiate.XifratTriat.Value;

                int midaClau = negotiate.XifratTriat.Value == AlgorismeSimetric.DES ? 8 : 16;
                ConnexioClient.Instancia.ClauSimetrica = RandomNumberGenerator.GetBytes(midaClau);

                // hem de xifrar la clau simetrica amb la clau publica del servidor
                byte[] clauXifrada = RSA_Utils.Xifrar(
                    ConnexioClient.Instancia.ClauSimetrica,
                    negotiate.DadesClauCriptografica!
                );

                await EnviarPaquetAsync(new PaquetComunicacio
                {
                    Tipus = "OK",
                    DadesClauCriptografica = Convert.ToBase64String(clauXifrada)
                }); //recordatori: base64 sutilitza per transportar byte[] en text iq ue siguin caracters ASCII segurs.
                //Al receptor es fa el procés invers: Convert.FromBase64String(...)

                SetStatus("S'ha pogut connectar al servidor");

                await Task.Delay(1000); // espera 1 segon
                CanviaDePagina();

            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                ConnexioClient.Instancia.Tancar();
            }
            finally
            {
                btnConnect.IsEnabled = true;
            }
        }

        private void CanviaDePagina()
        {
            new MainWindow().Show();
            this.Close();
        }

        private void SetStatus(string status) => TxtStatus.Text = status;
        #endregion

        #region Transport
        private async Task EnviarPaquetAsync(PaquetComunicacio paquet)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(paquet);
            byte[] longitud = BitConverter.GetBytes(json.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(longitud);
            await ConnexioClient.Instancia.Stream!.WriteAsync(longitud);
            await ConnexioClient.Instancia.Stream!.WriteAsync(json);
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
                int llegit = await ConnexioClient.Instancia.Stream!.ReadAsync(buffer, total, nBytes - total);
                if (llegit == 0) return 0;
                total += llegit;
            }
            return total;
        }
        #endregion
    }
}
