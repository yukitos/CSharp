using System;
using System.Windows;
using System.Windows.Interop;

namespace Clipboard2Word
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowModel model;

        public MainWindow()
        {
            Resources.Add("resources", new Properties.Resources());
            InitializeComponent();
            model = new MainWindowModel();
            this.DataContext = model;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!model.Initialize(this))
            {
                Application.Current.Shutdown(1);
                return;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            model.Dispose();
        }
    }
}
