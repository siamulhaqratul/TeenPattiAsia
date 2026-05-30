using CrazyGames.TreeLib;
using System.IO;
using UnityEditor;

namespace CrazyGames.WindowComponents.TextureOptimizations
{
    public class TextureTreeItem : TreeElement
    {
        public string TexturePath { get; }
        public string TextureName { get; }

        // All importer values captured once at construction to avoid repeated
        // cross-boundary calls on every sort pass and every rendered row.
        public TextureImporterType TextureType { get; }
        public TextureImporterCompression TextureCompression { get; }
        public TextureImporterFormat TextureFormat { get; }
        public int TextureMaxSize { get; }
        public bool HasCrunchCompression { get; }
        public int CrunchCompressionQuality { get; }
        public string TextureCompressionName { get; }

        public TextureTreeItem(string name, int depth, int id, string texturePath, TextureImporter textureImporter)
            : base(name, depth, id)
        {
            if (depth == -1)
                return;

            TexturePath = texturePath;
            TextureName = Path.GetFileName(texturePath);

            var platformSettings = textureImporter.GetPlatformTextureSettings("WebGL");

            TextureType             = textureImporter.textureType;
            TextureCompression      = textureImporter.textureCompression;
            TextureFormat           = platformSettings.format;
            TextureMaxSize          = platformSettings.maxTextureSize;
            HasCrunchCompression    = textureImporter.crunchedCompression;
            CrunchCompressionQuality = platformSettings.compressionQuality;

            TextureCompressionName = textureImporter.textureCompression switch
            {
                TextureImporterCompression.Uncompressed  => "Uncompressed",
                TextureImporterCompression.Compressed    => "Normal",
                TextureImporterCompression.CompressedHQ  => "High",
                TextureImporterCompression.CompressedLQ  => "Low",
                _                                        => textureImporter.textureCompression.ToString()
            };
        }
    }
}