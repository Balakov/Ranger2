using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows.Media;

namespace Ranger2
{
    internal class ImageCache_SkiaSharp : ImageCache.IImageDecoderImplementation
    {
        public ImageSource LoadImage(string path)
        {
            return LoadImage(path, 0);
        }

        public ImageSource LoadImage(string path, int desiredWidth)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("LoadImage Failure: Empty path");
                return null;
            }
                
            using (SKImage image = SKImage.FromEncodedData(path))
            {
                if (image != null)
                {
                    using (SKBitmap bitmap = SKBitmap.FromImage(image))
                    {
                        if (desiredWidth > 0)
                        {
                            float aspect = (float)bitmap.Width / (float)bitmap.Height;

                            int width = desiredWidth;
                            int height = (int)(desiredWidth / aspect);

                            using (var resizedBitmap = bitmap.Resize(new SKSizeI(width, height), SKFilterQuality.High))
                            {
                                return resizedBitmap.ToWriteableBitmap();
                            }
                        }
                        else
                        {
                            return bitmap.ToWriteableBitmap();
                        }
                    }
                }
                else
                {
                    Debug.Log("LoadImage Failure: FromEncodedData returned null image");
                }
            }

            return null;
        }

        public bool CanProvideBounds() => true;

        public (int Width, int Height) GetBounds(string path)
        {
            SKImageInfo bounds = SKBitmap.DecodeBounds(path);
            return (bounds.Width, bounds.Height);
        }

    }
}
