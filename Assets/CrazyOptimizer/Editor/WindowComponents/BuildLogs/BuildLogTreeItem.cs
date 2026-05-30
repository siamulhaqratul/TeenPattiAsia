using CrazyGames.TreeLib;

namespace CrazyOptimizer.Editor.WindowComponents.BuildLogs
{
    public class BuildLogTreeItem : TreeElement
    {
        public readonly float size;
        public readonly string sizeUnit;
        public readonly float sizePercentage;
        public readonly string filePath;

        // Computed once at construction; avoids switch overhead on every sort/render pass
        public readonly float sizeInBytes;

        public BuildLogTreeItem(string name, int depth, int id, float size, string sizeUnit, float sizePercentage,
            string filePath) : base(name, depth, id)
        {
            if (depth == -1)
                return;

            this.size = size;
            this.sizeUnit = sizeUnit;
            this.sizePercentage = sizePercentage;
            this.filePath = filePath;

            sizeInBytes = sizeUnit switch
            {
                "kb" => size * 1024f,
                "mb" => size * 1024f * 1024f,
                _ => throw new System.ArgumentOutOfRangeException(nameof(sizeUnit), $"Unknown size unit: '{sizeUnit}'")
            };
        }
    }
}