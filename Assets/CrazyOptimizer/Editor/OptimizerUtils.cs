using System.Collections.Generic;
using UnityEditor;

namespace CrazyGames
{
    public static class OptimizerUtils
    {
        /// <summary>
        /// Returns the paths of the scenes that will be included in the final build.
        /// </summary>
        public static IReadOnlyList<string> GetScenesInBuildPath()
        {
            var scenesInBuild = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenesInBuild.Add(scene.path);
            }
            return scenesInBuild;
        }
    }
}