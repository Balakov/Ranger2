using System;
using System.Windows.Media;
using System.IO;
using ImageMagick;

namespace Ranger2
{
    internal class ImageCache_Affinity : ImageCache.IImageDecoderImplementation
    {
        public ImageSource LoadImage(string path)
        {
            return LoadImage(path, 0);
        }

        public ImageSource LoadImage(string path, int desiredWidth)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                byte[] header = { 137, 80, 78, 71, 13, 10, 26, 10 };

                int max = bytes.Length - 8;
                for (int i = 0; i < max; i++)
                {
                    if (bytes[i + 0] == header[0] &&
                        bytes[i + 1] == header[1] &&
                        bytes[i + 2] == header[2] &&
                        bytes[i + 3] == header[3] &&
                        bytes[i + 4] == header[4] &&
                        bytes[i + 5] == header[5] &&
                        bytes[i + 6] == header[6] &&
                        bytes[i + 7] == header[7])
                    {
                        int pngSize = bytes.Length - i;
                        byte[] pngBytes = new byte[pngSize];

                        Array.Copy(bytes, i, pngBytes, 0, pngSize);

                        using (var magickImage = new MagickImage(pngBytes, MagickFormat.Png))
                        {
                            if (desiredWidth > 0)
                            {
                                float aspect = (float)magickImage.Width / (float)magickImage.Height;
                                uint width = (uint)desiredWidth;
                                uint height = (uint)(desiredWidth / aspect);

                                magickImage.Thumbnail(width, height);
                            }

                            return magickImage.ToBitmapSource();
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public bool CanProvideBounds() => false;
        public (int Width, int Height) GetBounds(string path) => throw new NotImplementedException();
    }
}