using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace Ranger2
{
    public class FontPreviewCache
    {
        public interface IFontPreviewLoadedNotification
        {
            void FontSKPaintLoaded(SKPaint paint);
        }

        private class QueuedFontPreview
        {
            public readonly string FontPath;
            private IFontPreviewLoadedNotification m_owner;
            private System.Windows.Threading.Dispatcher m_dispatcher;

            public QueuedFontPreview(string path, IFontPreviewLoadedNotification owner, System.Windows.Threading.Dispatcher dispatcher)
            {
                FontPath = path;
                m_owner = owner;
                m_dispatcher = dispatcher;
            }
            
            public void SetSKPaint(SKPaint paint)
            {
                if (!m_dispatcher.CheckAccess())
                {
                    m_dispatcher.Invoke(() => { SetSKPaint(paint); });
                }
                else
                {
                    m_owner.FontSKPaintLoaded(paint);
                }
            }
        }

        private class CachedFontPreview
        {
            public readonly SKPaint Paint;

            public CachedFontPreview(SKPaint paint)
            {
                Paint = paint;
            }
        }

        private ConcurrentDictionary<string, CachedFontPreview> m_fontPreviewCache = new();

        private System.Windows.Threading.Dispatcher m_dispatcher;
        private Queue<QueuedFontPreview> m_fontPreviewQueue = new();
        private bool m_queueRunning = false;

        public FontPreviewCache(System.Windows.Threading.Dispatcher dispatcher)
        {
            m_dispatcher = dispatcher;
        }

        public void QueueFontPreviewLoad(string path, IFontPreviewLoadedNotification owner)
        {
            if (m_fontPreviewCache.TryGetValue(path, out var cachedItem))
            {
                owner.FontSKPaintLoaded(cachedItem.Paint);
                return;
            }

            lock (m_fontPreviewQueue)
            {
                m_fontPreviewQueue.Enqueue(new QueuedFontPreview(path, owner, m_dispatcher));

                if (!m_queueRunning)
                {
                    m_queueRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem((_) => ProcessQueue(), null);
                }
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                QueuedFontPreview fontPreviewToProcess = null;

                lock (m_fontPreviewQueue)
                {
                    if (!m_fontPreviewQueue.Any())
                    {
                        m_queueRunning = false;
                        break;
                    }

                    fontPreviewToProcess = m_fontPreviewQueue.Dequeue();
                }

                try
                {
                    if (fontPreviewToProcess != null)
                    {
                        var paint = new SKPaint
                        {
                            Color = SKColors.Black,
                            IsAntialias = true,
                            Style = SKPaintStyle.Fill,
                            Typeface = SKTypeface.FromFile(fontPreviewToProcess.FontPath),
                            TextSize = 36
                        };

                        fontPreviewToProcess.SetSKPaint(paint);

                        if (!m_fontPreviewCache.ContainsKey(fontPreviewToProcess.FontPath))
                        {
                            m_fontPreviewCache.TryAdd(fontPreviewToProcess.FontPath, new CachedFontPreview(paint));
                        }
                    }
                }
                catch
                {
                    //ThreadPool.UnsafeQueueUserWorkItem((_) => ProcessQueue(), null);
                    throw;
                }
            }
        }
    }
}
