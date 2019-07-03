using System;
using System.Windows;
using System.Windows.Controls;

namespace DocumentGenerator
{
    /// <summary>
    /// Логика взаимодействия для ActivatedWindow.xaml
    /// </summary>
    public partial class ActivationWindow : Window
    {
        private readonly string _programTitle;

        public ActivationWindow(string title)
        {
            InitializeComponent();
            _programTitle = title;
        }

        private void Activate_OnClick(object sender, RoutedEventArgs e)
        {
            Settings settings = Settings.GetSettings();
            if (settings.TryActivate(ActivationBox.Text))
            {
                settings.Save(Environment.CurrentDirectory);
                DialogResult = true;
            }
            else MessageBox.Show("Неверный код активации.", _programTitle);
        }

        private void ActivationBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ActivateButton.IsEnabled = ActivationBox.Text.Length == 10;
        }
    }
}
