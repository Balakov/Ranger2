using System.Collections.Generic;
using System.Windows.Media;
using System.Threading;
using System.Collections.Concurrent;
using System;
using System.IO;

namespace Ranger2
{
    public class ImageCache
    {
        private const int c_defaultWidth = 200;
        private const int c_defaultHeight = c_defaultWidth;

        public interface IImageLoadedNotification
        {
            void ImageLoaded(ImageSource image);
        }

        public interface IImageDecoderImplementation
        {
            ImageSource LoadImage(string path, int desiredWith);
            ImageSource LoadImage(string path);
            bool CanProvideBounds();
            (int Width, int Height) GetBounds(string path);
        }

        private class QueuedThumbnail : QueuedImage
        {
            public readonly DateTime DateModified;
            public readonly int DesiredWidth;

            public QueuedThumbnail(string path,
                                   int desiredWidth,
                                   DateTime dateModified,
                                   IImageLoadedNotification owner,
                                   System.Windows.Threading.Dispatcher dispatcher) : base(path, owner, dispatcher)
            {
                DateModified = dateModified;
                DesiredWidth = desiredWidth;
            }
        }

        private class QueuedImage
        {
            public readonly string ImagePath;
            private IImageLoadedNotification m_owner;
            private System.Windows.Threading.Dispatcher m_dispatcher;
            
            public QueuedImage(string path,
                               IImageLoadedNotification owner,
                               System.Windows.Threading.Dispatcher dispatcher)
            {
                ImagePath = path;
                m_owner = owner;
                m_dispatcher = dispatcher;
            }

            public void SetImage(ImageSource image)
            {
                image?.Freeze();

                if (!m_dispatcher.CheckAccess())
                {
                    m_dispatcher.Invoke(() => { SetImage(image); });
                }
                else
                {
                    m_owner.ImageLoaded(image);
                }
            }
        }

        private class CachedImage
        {
            public readonly ImageSource Image;
            public readonly DateTime DateModified;
            public readonly int Width;

            public CachedImage(ImageSource image, int width, DateTime dateModified)
            {
                Image = image;
                Width = width;
                DateModified = dateModified;
            }
        }

        private class RegisteredExtension
        {
            public readonly IImageDecoderImplementation Decoder;
            public readonly bool UseInternalViewerForFileClick;

            public RegisteredExtension(IImageDecoderImplementation decoder, bool useInternalViewerForFileClick)
            {
                Decoder = decoder;
                UseInternalViewerForFileClick = useInternalViewerForFileClick;
            }
        }

        private ConcurrentDictionary<string, CachedImage> m_imageCache = new();
        private Dictionary<string, RegisteredExtension> m_imageDecoders = new();
        
        private System.Windows.Threading.Dispatcher m_dispatcher;

        public ImageCache(System.Windows.Threading.Dispatcher dispatcher)
        {
            m_dispatcher = dispatcher;

            var skiaDecoder = new ImageCache_SkiaSharp();
            var affinityDecoder = new ImageCache_Affinity();
            var imageMagickDecoder = new ImageCache_ImageMagick();

            m_imageDecoders.Add(".jpg", new RegisteredExtension(skiaDecoder, true));
            m_imageDecoders.Add(".jpeg", new RegisteredExtension(skiaDecoder, true));
            m_imageDecoders.Add(".png", new RegisteredExtension(skiaDecoder, true));
            m_imageDecoders.Add(".webp", new RegisteredExtension(skiaDecoder, true));

            m_imageDecoders.Add(".afdesign", new RegisteredExtension(affinityDecoder, false));
            m_imageDecoders.Add(".afpub", new RegisteredExtension(affinityDecoder, false));
            m_imageDecoders.Add(".afphoto", new RegisteredExtension(affinityDecoder, false));

            m_imageDecoders.Add(".bmp", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".dds", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".emf", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".gif", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".ico", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".jfif", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".pcx", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".psd", new RegisteredExtension(imageMagickDecoder, false));
            m_imageDecoders.Add(".svg", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".tiff", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".tif", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".tga", new RegisteredExtension(imageMagickDecoder, true));
            m_imageDecoders.Add(".heic", new RegisteredExtension(imageMagickDecoder, true));
        }

        public bool CanDecode(string path) => m_imageDecoders.ContainsKey(Path.GetExtension(path).ToLower());

        public bool ShouldDecodeOnFileClick(string path) => m_imageDecoders.TryGetValue(Path.GetExtension(path).ToLower(), 
                                                                                        out var registeredFileExtension) && 
                                                            registeredFileExtension.UseInternalViewerForFileClick;
        
        public (int Width, int Height) GetBounds(string path)
        {
            if (m_imageDecoders.TryGetValue(Path.GetExtension(path).ToLower(), out RegisteredExtension registeredFileExtension))
            {
                if (registeredFileExtension.Decoder.CanProvideBounds())
                {
                    var (w, h) = registeredFileExtension.Decoder.GetBounds(path);

                    if (w > 0 && h > 0)
                    {
                        return (w, h);
                    }
                }
            }
                
            return (c_defaultWidth, c_defaultHeight);
        }

        public void QueueImageLoad(string path, IImageLoadedNotification owner)
        {
            QueueImageInternal(new QueuedImage(path,owner, m_dispatcher), null);
        }

        public void QueueThumbnailLoad(string path, int desiredWidth, DateTime dateModified, IImageLoadedNotification owner, TimeSpan? delay)
        {
            lock (m_imageCache)
            {
                if (m_imageCache.TryGetValue(path, out var cachedItem))
                {
                    FileInfo fi = new FileInfo(path);
                    if (fi.LastWriteTime != cachedItem.DateModified || desiredWidth != cachedItem.Width)
                    {
                        m_imageCache.TryRemove(new KeyValuePair<string, CachedImage>(path, cachedItem));
                    }
                    else
                    {
                        owner.ImageLoaded(cachedItem.Image);
                        return;
                    }
                }
            }

            QueueImageInternal(new QueuedThumbnail(path, desiredWidth, dateModified, owner, m_dispatcher), delay);
        }

        private void QueueImageInternal(QueuedImage imageToProcess, TimeSpan? delay)
        {
            ThreadPool.QueueUserWorkItem(o => 
            {
                if (delay.HasValue)
                {
                    // Don't try to access the file immediately if requested as it might need time for locks to be released.
                    Thread.Sleep(delay.Value);
                }

                string fileExtension = Path.GetExtension(imageToProcess.ImagePath).ToLower();
                if (m_imageDecoders.TryGetValue(fileExtension, out var registeredFileExtension))
                {
                    if (imageToProcess is QueuedThumbnail thumbnail)
                    {
                        var imageSource = registeredFileExtension.Decoder.LoadImage(thumbnail.ImagePath, thumbnail.DesiredWidth);

                        if (imageSource != null)
                        {
                            lock (m_imageCache)
                            {
                                if (!m_imageCache.ContainsKey(thumbnail.ImagePath))
                                {
                                    m_imageCache.TryAdd(thumbnail.ImagePath, new CachedImage(imageSource, (int)imageSource.Width, thumbnail.DateModified));
                                }
                            }
                        }
                        else
                        {
                            Debug.Log($"LoadImage Failure: Image source was null from decoder for {thumbnail.ImagePath}");
                        }

                        thumbnail.SetImage(imageSource);
                    }
                    else
                    {
                        imageToProcess.SetImage(registeredFileExtension.Decoder.LoadImage(imageToProcess.ImagePath));
                    }
                }
            });
        }
    }
}
