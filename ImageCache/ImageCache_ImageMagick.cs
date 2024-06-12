using System;
using System.Windows.Media;
using ImageMagick;

namespace Ranger2
{
    internal class ImageCache_ImageMagick : ImageCache.IImageDecoderImplementation
    {
        public ImageSource LoadImage(string path)
        {
            return LoadImage(path, 0);
        }

        public ImageSource LoadImage(string path, int desiredWidth)
        {
            try
            {
                using (var magickImage = new MagickImage(path))
                {
                    magickImage.AutoOrient();

                    if (desiredWidth > 0)
                    {
                        float aspect = (float)magickImage.Width / (float)magickImage.Height;
                        int width = desiredWidth;
                        int height = (int)(desiredWidth / aspect);

                        magickImage.Thumbnail(width, height);
                    }

                    return magickImage.ToBitmapSource();
                }
            }
            catch { }

            return null;
        }

        public bool CanProvideBounds() => false;
        public (int Width, int Height) GetBounds(string path) => throw new NotImplementedException();
    }
}