using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.VSCode.EditorTests")]

namespace AntigravityEditor
{
    internal static class StringUtils
    {
        private const char WinSeparator = '\\';
        private const char UnixSeparator = '/';

        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (Path.DirectorySeparatorChar == WinSeparator)
                path = path.Replace(UnixSeparator, WinSeparator);
            else if (Path.DirectorySeparatorChar == UnixSeparator)
                path = path.Replace(WinSeparator, UnixSeparator);

            // Collapse any double-backslash sequences that may result from the replacements
            return path.Replace(@"\\", WinSeparator.ToString());
        }
    }
}
