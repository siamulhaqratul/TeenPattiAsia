using CrazyGames.TreeLib;
using System.IO;
using UnityEditor;

namespace CrazyGames.WindowComponents.ModelOptimizations
{
    public class ModelTreeItem : TreeElement
    {
        public string ModelPath { get; }
        public string ModelName { get; }

        // All importer values captured once at construction.
        // Avoids repeated cross-boundary property calls on every sort/render pass.
        public bool IsReadWriteEnabled { get; }
        public bool ArePolygonsOptimized { get; }
        public bool AreVerticesOptimized { get; }
        public ModelImporterMeshCompression MeshCompression { get; }
        public ModelImporterAnimationCompression AnimationCompression { get; }

        // Enum.ToString() already produces the correct display strings for both enums
        public string MeshCompressionName { get; }
        public string AnimationCompressionName { get; }

        public ModelTreeItem(string name, int depth, int id, string modelPath, ModelImporter modelImporter)
            : base(name, depth, id)
        {
            if (depth == -1)
                return;

            ModelPath = modelPath;
            ModelName = Path.GetFileName(modelPath);

            IsReadWriteEnabled    = modelImporter.isReadable;
            ArePolygonsOptimized  = modelImporter.optimizeMeshPolygons;
            AreVerticesOptimized  = modelImporter.optimizeMeshVertices;
            MeshCompression       = modelImporter.meshCompression;
            AnimationCompression  = modelImporter.animationCompression;

            // These enums have display-ready names; ToString() avoids a verbose switch
            MeshCompressionName      = modelImporter.meshCompression.ToString();
            AnimationCompressionName = modelImporter.animationCompression.ToString();
        }
    }
}