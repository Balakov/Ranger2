using System;
using System.IO;

namespace Ranger2
{
    public static class ViewFilter
    {
        [Flags]
        public enum ViewMask
        {
            ShowHidden = 1 << 0,
            ShowSystem = 1 << 1,
            ShowDot = 1 << 2
        }

        public static bool FilterViewByAttributes(FileAttributes attributes, ViewMask viewMask, bool isDot, out bool greyedOut)
        {
            greyedOut = (attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System) || isDot);

            if (attributes.HasFlag(FileAttributes.Hidden) && !viewMask.HasFlag(ViewMask.ShowHidden))
            {
                return false;
            }

            if (attributes.HasFlag(FileAttributes.System) && !viewMask.HasFlag(ViewMask.ShowSystem))
            {
                return false;
            }

            if (isDot && !viewMask.HasFlag(ViewMask.ShowDot))
            {
                return false;
            }

            return true;
        }
    }
}
