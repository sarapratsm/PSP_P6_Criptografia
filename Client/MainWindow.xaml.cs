using Client.Dades;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utils.Tcp;

namespace Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        #region Inicialització
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Inicialitzar capçalera
            TxtNickname.Text = ConnexioClient.Instancia.Username;
            TxtEstatConnexio.Text = $"Connectat · {ConnexioClient.Instancia.Algorisme}";
            TxtMetodeXifrat.Text = ConnexioClient.Instancia.Algorisme.ToString();
            BadgeXifratPanel.Visibility = Visibility.Visible;

            // Habilitar controls
            InputMissatge.IsEnabled = true;
            BtnEnviar.IsEnabled = true;
            InputMissatge.Focus();

            // Arrencar el loop de recepció en background
            _ = Task.Run(EscoltarMissatgesAsync);
        }
        #endregion

        #region Recepció de missatges
        private async Task EscoltarMissatgesAsync()
        {
            try
            {
                while (true)
                {
                    var paquet = await LlegirPaquetAsync();
                    if (paquet == null || paquet.Tipus == "BYE") break;

                    if (paquet.Tipus == "MESSAGE")
                    {
                        byte[] xifrat = Convert.FromBase64String(paquet.Contingut);
                        byte[] clar = ConnexioClient.Instancia.Xifrador!.Desxifrar(xifrat, ConnexioClient.Instancia.ClauSimetrica);
                        string text = Encoding.UTF8.GetString(clar);

                        // Dispatcher.Invoke pq estem en un fil i no podem tocar la UI directament
                        Dispatcher.Invoke(() => AfegirMissatge(text));
                    }
                }
            }
            catch { }
        }

        private void AfegirMissatge(string text)
        {
            var bloc = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4)
            };
            PanellMissatges.Children.Add(bloc);

            // Scroll automàtic al final
            ScrollMissatges.ScrollToBottom();
        }
        #endregion

        #region Enviament de missatges
        private async void BtnDesconnectar_Click(object sender, RoutedEventArgs e)
        {
            await DesconnectarAsync();
        }

        private async Task DesconnectarAsync()
        {
            try
            {
                // Avisem el servidor que ens anem
                await EnviarPaquetAsync(new PaquetComunicacio { Tipus = "BYE" });
            }
            catch { }
            finally
            {
                ConnexioClient.Instancia.Tancar();
                Environment.Exit(0);
            }
        }

        //Enviar amb el botó
        private async void BtnEnviar_Click(object sender, RoutedEventArgs e)
        {
            await EnviarMissatgeAsync();
        }

        //Enviar amb enter
        private async void InputMissatge_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await EnviarMissatgeAsync();
        }

        private async Task EnviarMissatgeAsync()
        {
            string message = InputMissatge.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            byte[] xifrat = ConnexioClient.Instancia.Xifrador!.Xifrar(bytes, ConnexioClient.Instancia.ClauSimetrica);

            await EnviarPaquetAsync(new PaquetComunicacio
            {
                Tipus = "MESSAGE",
                Nickname = ConnexioClient.Instancia.Username,
                Contingut = Convert.ToBase64String(xifrat)
            });

            InputMissatge.Clear();
        }
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
