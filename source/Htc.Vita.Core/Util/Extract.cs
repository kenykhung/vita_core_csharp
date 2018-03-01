﻿using System.IO;
using Htc.Vita.Core.Log;
using Htc.Vita.Core.Runtime;

namespace Htc.Vita.Core.Util
{
    public static partial class Extract
    {
        private static readonly Logger Log = Logger.GetInstance(typeof(Extract));

        public static bool FromFileToIcon(FileInfo fromFile, FileInfo toIcon)
        {
            if (!Platform.IsWindows)
            {
                return false;
            }
            return Windows.FromFileToIconInPlatform(fromFile, toIcon);
        }
    }
}
