using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Excel = Microsoft.Office.Interop.Excel;

namespace Clipboard2Word
{
    public class MainWindowModel : INotifyPropertyChanged, IDisposable
    {
        private ClipboardWatcher watcher;
        private Excel.Application excel;
        private Excel.Workbook workbook;

        public MainWindowModel()
        {
            excel = new Excel.Application();
            excel.Visible = true;
            MenuItem_FileOpen = new RelayCommand(_ => OnMenuItem_FileOpen());
        }

        public string FileName { get; private set; }

        public void InitializeClipboardWatcher(Window window)
        {
            watcher = new ClipboardWatcher(new WindowInteropHelper(window).Handle);
            watcher.DrawClipboard += OnDrawClipboard;
        }

        private void OnDrawClipboard(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var img = Clipboard.GetImage();
                ClipboardImage = img;

                if (workbook != null)
                {
                    try
                    {
                        workbook.ActiveSheet.PasteSpecial();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public RelayCommand MenuItem_FileOpen { get; private set; }
        public void OnMenuItem_FileOpen()
        {
            var dialog = new OpenFileDialog()
            {
                DefaultExt = ".xlsx",
                Filter = Properties.Resources.ExcelFileFilter
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                FileName = dialog.FileName;
                if (workbook != null)
                {
                    workbook.Close();
                }
                try
                {
                    workbook = excel.Workbooks.Open(FileName, Editable: true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private BitmapSource _clipboardImage;
        public BitmapSource ClipboardImage
        {
            get { return _clipboardImage; }
            set { SetProperty(ref _clipboardImage, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;

                var handler = Interlocked.CompareExchange(ref PropertyChanged, null, null);
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public void Dispose()
        {
            if (workbook != null)
            {
                try
                {
                    workbook.Close();
                }
                catch (Exception)
                {
                    // Do nothing here.
                }
            }
            if (watcher != null)
            {
                watcher.Dispose();
            }

            excel.Quit();
        }
    }
}
