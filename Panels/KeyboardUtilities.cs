using System.Windows.Input;

namespace Ranger2
{
    internal static class KeyboardUtilities
    {
        public static bool IsAlphaNumericKey(Key key) => (key >= Key.D0 && key <= Key.D9) || (key >= Key.A && key <= Key.Z);
        public static string AlphaNumericKeyKeyToString(Key key) => (key >= Key.D0 && key <= Key.D9) ? key.ToString().Substring(1) : key.ToString();

        public static bool IsAltDown => Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        public static bool IsControlDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        public static bool IsShiftDown => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
    }
}
