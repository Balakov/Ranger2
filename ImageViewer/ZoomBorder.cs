﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public delegate void OnImageViewerZoomChangedDelegate(int zoomLevel);
    
    // https://stackoverflow.com/questions/741956/pan-zoom-image
    public class ZoomBorder : Border
    {
        public event OnImageViewerZoomChangedDelegate OnImageViewerZoomChanged;

        private UIElement child = null;
        private Point origin;
        private Point start;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // Reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // Reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;

                OnImageViewerZoomChanged?.Invoke((int)(st.ScaleX * 100));
            }
        }

        public void ScaleToFit(double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            double ratioX = viewWidth / imageWidth;
            double ratioY = viewHeight / imageHeight;
            // use whichever multiplier is smaller
            double zoom = ratioX < ratioY ? ratioX : ratioY;

            st.ScaleX = zoom;
            st.ScaleY = zoom;

            if (imageWidth > viewWidth)
            {
                double spareX = viewWidth - (imageWidth * zoom);
                tt.X = (spareX / 2);
            }
            else
            {
                double pixelSizeIncreaseX = (imageWidth * zoom) - imageWidth;
                tt.X = -(pixelSizeIncreaseX / 2);
            }

            if(imageHeight > viewHeight)
            {
                double spareY = viewHeight - (imageHeight * zoom);
                tt.Y = (spareY / 2);
            }
            else
            {
                double pixelSizeIncreaseY = (imageHeight * zoom) - imageHeight;
                tt.Y = -(pixelSizeIncreaseY / 2);
            }

            OnImageViewerZoomChanged?.Invoke((int)(st.ScaleX * 100));
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .1 || st.ScaleY < .1))
                    return;

                Point relative = e.GetPosition(child);

                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                double zoomCorrected = zoom * st.ScaleX; 
                st.ScaleX += zoomCorrected; 
                st.ScaleY += zoomCorrected;
                
                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;

                OnImageViewerZoomChanged?.Invoke((int)(st.ScaleX * 100));
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }
            }
        }

        #endregion
    }
}