using CrazyGames.TreeLib;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrazyGames.WindowComponents.AudioOptimizations
{
    public class AudioTreeItem : TreeElement
    {
        public string AudioPath { get; }
        public string AudioName { get; }

        // Resolved at construction time; no need to store the importer reference afterwards
        public string LoadType { get; }
        public int Quality { get; }

        public AudioTreeItem(string name, int depth, int id, string audioPath, AudioImporter audioImporter)
            : base(name, depth, id)
        {
            if (depth == -1)
                return;

            AudioPath = audioPath;
            AudioName = Path.GetFileName(audioPath);

            var platformSettings = audioImporter.GetOverrideSampleSettings("WebGL");

            LoadType = platformSettings.loadType switch
            {
                AudioClipLoadType.DecompressOnLoad => "Decompress on load",
                AudioClipLoadType.CompressedInMemory => "Compressed in memory",
                AudioClipLoadType.Streaming => "Streaming",
                _ => platformSettings.loadType.ToString()
            };

            Quality = Mathf.RoundToInt(platformSettings.quality * 100);
        }
    }
}