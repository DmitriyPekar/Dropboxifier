using System;
using System.Runtime.InteropServices;

namespace Dropboxifier
{
    /// <summary>
    /// generic event args for arguments that only need 1 parameter
    /// </summary>
    public class EventArgs<T> : EventArgs
    {
        public T Data { get; set; }

        public EventArgs(T data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Basic utility methods
    /// </summary>
    public class Utils
    {
        public const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="lpSymlinkFileName">The source file or directory</param>
        /// <param name="lpTargetFileName">The target file or directory</param>
        /// <param name="dwFlags">Flags. Either default (0x0) for a file or SYMBOLIC_LINK_FLAG_DIRECTORY (0x1) for a directory</param>
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
        public static extern int CreateSymbolicLink([In] string lpSymlinkFileName, [In] string lpTargetFileName, int dwFlags = 0x0);
    }
}
