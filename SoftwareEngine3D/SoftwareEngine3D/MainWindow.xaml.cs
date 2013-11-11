using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpDX;
using System.Threading;

namespace SoftwareEngine3D
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device;
        private Mesh[] meshes;
        private Camera camera = new Camera();
        private DateTime previousDate;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteableBitmap bmp = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            device = new Device(bmp);
            frontBuffer.Source = bmp;

            meshes = await device.LoadJSONFileAsync(System.IO.Path.GetFullPath("monkey.babylon"));

            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // FPS
            var now = DateTime.Now;
            var currentFps = 1000.0 / (now - previousDate).TotalMilliseconds;
            previousDate = now;

            fps.Text = string.Format("{0:0.00} fps", currentFps);

            // Rendering loop
            device.Clear(0, 0, 0, 255);

            foreach (var mesh in meshes)
            {
                // Rotating slightly the meshes during each frame rendered
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            }
            device.Render(camera, meshes);

            device.Present();
        }
    }
}
