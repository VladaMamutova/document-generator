using System.Windows;

namespace DocumentGenerator
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();
            
            Settings settings = Settings.GetSettings();
            bool success = false;
            if (!settings.CanRunOnTheCurrentProccessor())
            {
                MessageBox.Show("К сожалению, была обнаружена попытка " +
                                "несанкционированного доступа к программе. Возможно, " +
                                "она была нелегально скопирована. Программа не может быть запущена.",
                    MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                success = true;
            }

            if (success)
            {
                if (!settings.IsActivated && !settings.CanRunProgramWithoutActivation)
                {
                    ActivationWindow activationWindow =
                        new ActivationWindow(MainWindow.Title);
                    if (activationWindow.ShowDialog() == true)
                    {
                        MessageBox.Show("Программа успешно активирована!",
                            MainWindow.Title);
                    }
                    else
                    {
                        success = false;
                    }
                }
            }

            if (success)
            {
                MainWindow.Show();
            }
            else
            {
                MainWindow.Close();
            }
        }
    }
}
