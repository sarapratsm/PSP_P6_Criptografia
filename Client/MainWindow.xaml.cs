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


            InputMissatge.Focus();

            InicialitzarEmojis();
            InicialitzarControls();

            //loop escolta
            _ = Task.Run(EscoltarMissatgesAsync);
        }

        private void InicialitzarControls()
        {
            //metode super inutil pero bona practica
            InputMissatge.IsEnabled = true;
            BtnEnviar.IsEnabled = true;
            BtnEmoji.IsEnabled = true;
            BtnFitxer.IsEnabled = true;
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
                        byte[] raw = ConnexioClient.Instancia.Xifrador!.Desxifrar(xifrat, ConnexioClient.Instancia.ClauSimetrica);
                        string text = Encoding.UTF8.GetString(raw);
                        Dispatcher.Invoke(() => AfegirMissatge(text));
                    }

                    if (paquet.Tipus == "FILE" && !string.IsNullOrEmpty(paquet.NomFitxer))
                    {
                        byte[] xifrat = Convert.FromBase64String(paquet.Contingut);
                        byte[] raw = ConnexioClient.Instancia.Xifrador!.Desxifrar(xifrat, ConnexioClient.Instancia.ClauSimetrica);
                        string nomFitxer = paquet.NomFitxer;
                        string nick = paquet.Nickname;
                        Dispatcher.Invoke(() => RebreFitxer(nick, nomFitxer, raw));
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

            // scroll automàtic al final
            ScrollMissatges.ScrollToBottom();
        }
        #endregion

        #region Fitxers
        private const long MAX_MIDA_FITXER = 10 * 1024 * 1024; // 10 MB

        private async void BtnFitxer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecciona un fitxer per enviar"
            };
            if (dialog.ShowDialog() != true) return;

            var info = new System.IO.FileInfo(dialog.FileName);
            if (info.Length > MAX_MIDA_FITXER)
            {
                MessageBox.Show($"El fitxer és massa gran. Màxim: 10 MB.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] bytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
            byte[] xifrat = ConnexioClient.Instancia.Xifrador!.Xifrar(bytes, ConnexioClient.Instancia.ClauSimetrica);

            await EnviarPaquetAsync(new PaquetComunicacio
            {
                Tipus = "FILE",
                Nickname = ConnexioClient.Instancia.Username,
                NomFitxer = info.Name,
                Contingut = Convert.ToBase64String(xifrat)
            });

            AfegirMissatge($"📎 Has enviat el fitxer: {info.Name}");
        }

        private void RebreFitxer(string nick, string nomFitxer, byte[] bytes)
        {
            // Creem una targeta al xat amb botó de desar
            // El closure captura 'bytes' i 'nomFitxer' per quan l'usuari decideixi desar
            var targeta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };

            var lblFitxer = new TextBlock
            {
                Text = $"📎 {nick} ha enviat: {nomFitxer}",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var btnDesar = new Button
            {
                Content = "💾 Desar",
                Padding = new Thickness(8, 4, 8, 4)
            };
            btnDesar.Click += (_, _) =>
            {
                /*els bytes del document viuen en memoria mentre el client esta obert.*/
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Guardar fitxer rebut",
                    FileName = nomFitxer
                };
                if (dialog.ShowDialog() != true) return;

                System.IO.File.WriteAllBytes(dialog.FileName, bytes);
                //lblFitxer.Text = $"✅ Desat: {nomFitxer}";
                //btnDesar.IsEnabled = false; //crec que prefereixo poder descarregar-lo de nou...
            };

            targeta.Children.Add(lblFitxer);
            targeta.Children.Add(btnDesar);
            PanellMissatges.Children.Add(targeta);
            ScrollMissatges.ScrollToBottom();
        }
        #endregion

        #region Emojis
        private static readonly string[] Emojis =
        {
            "😀","😂","😍","🥰","😎","😢","😡","🤔","👍","👎",
            "❤️","🔥","✅","⭐","🎉","🙏","💪","🤣","😊","🥳",
            "😅","😱","🤩","😴","🤗","💀","🐱","🐶","🍕","☕"
        };

        private void InicialitzarEmojis()
        {
            foreach (string emoji in Emojis)
            {
                var btn = new Button
                {
                    Content = emoji,
                    FontSize = 24,
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(2),
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btn.Click += (_, _) =>
                {
                    InputMissatge.Text += emoji;
                    InputMissatge.CaretIndex = InputMissatge.Text.Length;
                    PopupEmoji.IsOpen = false;
                    InputMissatge.Focus();
                };
                PanellEmojis.Children.Add(btn);
            }
        }

        private void BtnEmoji_Click(object sender, RoutedEventArgs e)
        {
            PopupEmoji.IsOpen = !PopupEmoji.IsOpen;
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
                // avisem el servidor que ens anem
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
