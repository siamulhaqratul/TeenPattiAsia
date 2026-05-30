using System.IO;
using System.Security;
using System.Text;

namespace AntigravityEditor
{
    public interface IFileIO
    {
        bool Exists(string fileName);
        string ReadAllText(string fileName);
        void WriteAllText(string fileName, string content);
        void CreateDirectory(string pathName);
        string EscapedRelativePathFor(string file, string projectDirectory);
    }

    class FileIOProvider : IFileIO
    {
        public bool Exists(string fileName) => File.Exists(fileName);

        public string ReadAllText(string fileName)
        {
            // Specify UTF-8 explicitly to avoid locale-dependent behaviour
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public void WriteAllText(string fileName, string content)
        {
            File.WriteAllText(fileName, content, Encoding.UTF8);
        }

        public void CreateDirectory(string pathName) => Directory.CreateDirectory(pathName);

        public string EscapedRelativePathFor(string file, string projectDirectory)
        {
            var projectDir = Path.GetFullPath(projectDirectory);

            // Normalize path separators — PackageManagerRemapper expects OS-specific separators
            var absolutePath = Path.GetFullPath(file.NormalizePath());
            var path = SkipPathPrefix(absolutePath, projectDir);

            return SecurityElement.Escape(path);
        }

        private static string SkipPathPrefix(string path, string prefix)
        {
            return path.StartsWith($@"{prefix}{Path.DirectorySeparatorChar}", System.StringComparison.Ordinal)
                ? path.Substring(prefix.Length + 1)
                : path;
        }
    }
}
