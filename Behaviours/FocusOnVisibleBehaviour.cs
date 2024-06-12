using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ranger2.Behaviours
{
    // https://stackoverflow.com/questions/53860095/how-to-set-keyboard-focus-to-a-child-when-its-parent-is-made-visible

    public static class FocusOnVisibleBehavior
    {
        public static readonly DependencyProperty FocusProperty = DependencyProperty.RegisterAttached("Focus",
                                                                                                      typeof(bool),
                                                                                                      typeof(FocusOnVisibleBehavior),
                                                                                                      new PropertyMetadata(false, OnFocusChange));

        public static void SetFocus(DependencyObject source, bool value)
        {
            source.SetValue(FocusProperty, value);
        }

        public static bool GetFocus(DependencyObject source)
        {
            return (bool)source.GetValue(FocusProperty);
        }

        private static void OnFocusChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            DependencyPropertyChangedEventHandler handler = (sender, args) =>
            {
                if ((bool)args.NewValue)
                {
                    // see http://stackoverflow.com/questions/13955340/keyboard-focus-does-not-work-on-text-box-in-wpf
                    element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate ()
                    {
                        element.Focus();         // Set Logical Focus
                        Keyboard.Focus(element); // Set Keyboard Focus
                    }));

                }
            };

            if (e.NewValue != null)
            {
                if ((bool)e.NewValue)
                {
                    element.IsVisibleChanged += handler;
                    element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate ()
                    {
                        element.Focus();         // Set Logical Focus
                        Keyboard.Focus(element); // Set Keyboard Focus
                    }));
                }
                else
                {
                    element.IsVisibleChanged -= handler;
                }
            }
        }
    }
}