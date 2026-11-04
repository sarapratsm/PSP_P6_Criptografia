using System.Windows;
using Client.Views;

namespace Client
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Quan arrenca l'app, mostrem el Login en lloc de la MainWindow.
            // La MainWindow s'obrirà des del Login un cop connectat.
            new Login().Show();
        }
    }
}
