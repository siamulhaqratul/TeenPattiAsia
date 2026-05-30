using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
// Alias needed because both UnityEditor and UnityEditor.PackageManager define a 'PackageInfo' type.
// Without this, the compiler raises CS0104 (ambiguous reference).
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AntigravityEditor
{
    public interface IAssemblyNameProvider
    {
        string[] ProjectSupportedExtensions { get; }
        ProjectGenerationFlag ProjectGenerationFlag { get; }
        string GetAssemblyNameFromScriptPath(string path);
        IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution);
        IEnumerable<string> GetAllAssetPaths();
        IEnumerable<string> GetRoslynAnalyzerPaths();
        PackageInfo FindForAssetPath(string assetPath);
        ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
        bool IsInternalizedPackagePath(string path);
        void ToggleProjectGeneration(ProjectGenerationFlag preference);
    }

    internal interface IPackageInfoCache
    {
        void ResetPackageInfoCache();
    }

    internal class AssemblyNameProvider : IAssemblyNameProvider, IPackageInfoCache
    {
        private readonly Dictionary<string, PackageInfo> m_PackageInfoCache = new();

        ProjectGenerationFlag m_ProjectGenerationFlag = (ProjectGenerationFlag)EditorPrefs.GetInt("unity_project_generation_flag", 0);

        public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationUserExtensions;

        public ProjectGenerationFlag ProjectGenerationFlag
        {
            get => m_ProjectGenerationFlag;
            private set
            {
                EditorPrefs.SetInt("unity_project_generation_flag", (int)value);
                m_ProjectGenerationFlag = value;
            }
        }

        public string GetAssemblyNameFromScriptPath(string path)
        {
            return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
        }

        public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
        {
            return CompilationPipeline.GetAssemblies()
                .Where(i => i.sourceFiles.Length > 0 && i.sourceFiles.Any(shouldFileBePartOfSolution));
        }

        public IEnumerable<string> GetAllAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths();
        }

        private static string ResolvePotentialParentPackageAssetPath(string assetPath)
        {
            const string packagesPrefix = "packages/";
            if (!assetPath.StartsWith(packagesPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var followupSeparator = assetPath.IndexOf('/', packagesPrefix.Length);
            return followupSeparator == -1
                ? assetPath.ToLowerInvariant()
                : assetPath.Substring(0, followupSeparator).ToLowerInvariant();
        }

        public void ResetPackageInfoCache()
        {
            m_PackageInfoCache.Clear();
        }

        public PackageInfo FindForAssetPath(string assetPath)
        {
            var parentPackageAssetPath = ResolvePotentialParentPackageAssetPath(assetPath);
            if (parentPackageAssetPath == null)
                return null;

            if (m_PackageInfoCache.TryGetValue(parentPackageAssetPath, out var cachedPackageInfo))
                return cachedPackageInfo;

            var result = PackageInfo.FindForAssetPath(parentPackageAssetPath);
            m_PackageInfoCache[parentPackageAssetPath] = result;
            return result;
        }

        public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return CompilationPipeline.ParseResponseFile(responseFilePath, projectDirectory, systemReferenceDirectories);
        }

        public bool IsInternalizedPackagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var packageInfo = FindForAssetPath(path);
            if (packageInfo == null)
                return false;

            return packageInfo.source switch
            {
                PackageSource.Embedded  => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Embedded),
                PackageSource.Registry  => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Registry),
                PackageSource.BuiltIn   => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.BuiltIn),
                PackageSource.Unknown   => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Unknown),
                PackageSource.Local     => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Local),
                PackageSource.Git       => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Git),
#if UNITY_2019_3_OR_NEWER
                PackageSource.LocalTarball => !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.LocalTarBall),
#endif
                _ => false
            };
        }

        public void ToggleProjectGeneration(ProjectGenerationFlag preference)
        {
            if (ProjectGenerationFlag.HasFlag(preference))
                ProjectGenerationFlag ^= preference;
            else
                ProjectGenerationFlag |= preference;
        }

        public IEnumerable<string> GetRoslynAnalyzerPaths()
        {
            // Use Any() instead of SingleOrDefault() to avoid InvalidOperationException on duplicate labels
            return PluginImporter.GetAllImporters()
                .Where(i => !i.isNativePlugin && AssetDatabase.GetLabels(i).Any(l => l == "RoslynAnalyzer"))
                .Select(i => i.assetPath);
        }
    }
}
