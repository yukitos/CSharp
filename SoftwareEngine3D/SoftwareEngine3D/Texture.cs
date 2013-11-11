using SharpDX;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SoftwareEngine3D
{
    public class Texture
    {
        private byte[] internalBuffer;
        private int width;
        private int height;

        // Working with a fix sized texture (512x512, 1024x1024, etc.).
        public Texture(string filename, int width, int height)
        {
            this.width = width;
            this.height = height;
            Load(filename);
        }

        void Load(string filename)
        {
            try
            {
                using (FileStream sourceStream = new FileStream(filename, FileMode.Open))
                {
                    var decoder = BitmapDecoder.Create(sourceStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                    var bmp = new WriteableBitmap(decoder.Frames[0]);
                    bmp.Freeze();

                    internalBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
                    bmp.CopyPixels(internalBuffer, bmp.BackBufferStride, 0);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        // Takes the U & V coordinates exported by Blender
        // and return the corresponding pixel color in the texture
        public Color4 Map(float tu, float tv)
        {
            // Image is not loaded yet
            if (internalBuffer == null)
            {
                return Color4.White;
            }

            // using a % operator to cycle/repeat the texture if needed
            int u = Math.Abs((int)(tu * width) % width);
            int v = Math.Abs((int)(tv * height) % height);

            int pos = (u + v * width) * 4;
            byte b = internalBuffer[pos + 0];
            byte g = internalBuffer[pos + 1];
            byte r = internalBuffer[pos + 2];
            byte a = internalBuffer[pos + 3];

            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }
    }
}
