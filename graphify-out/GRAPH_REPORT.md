# Graph Report - Assets  (2026-05-31)

## Corpus Check
- 60 files · ~81,230 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 456 nodes · 715 edges · 30 communities (28 shown, 2 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Project Generator Interfaces|Project Generator Interfaces]]
- [[_COMMUNITY_Assembly Name Provider|Assembly Name Provider]]
- [[_COMMUNITY_Tree View UI|Tree View UI]]
- [[_COMMUNITY_Game Loading & UI|Game Loading & UI]]
- [[_COMMUNITY_Audio Optimization|Audio Optimization]]
- [[_COMMUNITY_Tree Model Data|Tree Model Data]]
- [[_COMMUNITY_Audio Tree View|Audio Tree View]]
- [[_COMMUNITY_Build Log Tree View|Build Log Tree View]]
- [[_COMMUNITY_Model Tree View|Model Tree View]]
- [[_COMMUNITY_Texture Tree View|Texture Tree View]]
- [[_COMMUNITY_Optimization Tree Items|Optimization Tree Items]]
- [[_COMMUNITY_Model Optimization|Model Optimization]]
- [[_COMMUNITY_File IO Provider|File IO Provider]]
- [[_COMMUNITY_Texture Optimization|Texture Optimization]]
- [[_COMMUNITY_Build Log Analysis|Build Log Analysis]]
- [[_COMMUNITY_Antigravity Discovery|Antigravity Discovery]]
- [[_COMMUNITY_Project File Generation|Project File Generation]]
- [[_COMMUNITY_Sprite Emoji Frames|Sprite Emoji Frames]]
- [[_COMMUNITY_Tree Element Utilities|Tree Element Utilities]]
- [[_COMMUNITY_Antigravity Script Editor|Antigravity Script Editor]]
- [[_COMMUNITY_Tree Extension Methods|Tree Extension Methods]]
- [[_COMMUNITY_MCP Unity Package Installer|MCP Unity Package Installer]]
- [[_COMMUNITY_Export Optimizations UI|Export Optimizations UI]]
- [[_COMMUNITY_GUID Generator|GUID Generator]]
- [[_COMMUNITY_Game Setup Editor|Game Setup Editor]]
- [[_COMMUNITY_Tree Element Data|Tree Element Data]]
- [[_COMMUNITY_Optimizer Utilities|Optimizer Utilities]]
- [[_COMMUNITY_String Utilities|String Utilities]]
- [[_COMMUNITY_About Window|About Window]]
- [[_COMMUNITY_Project Generation Flags|Project Generation Flags]]

## God Nodes (most connected - your core abstractions)
1. `ProjectGeneration` - 58 edges
2. `TreeViewWithTreeModel` - 24 edges
3. `TreeModel` - 18 edges
4. `AssemblyNameProvider` - 15 edges
5. `ModelOptimization` - 14 edges
6. `Assembly` - 14 edges
7. `Loading` - 13 edges
8. `AudioOptimization` - 12 edges
9. `TextureOptimization` - 12 edges
10. `DynamicViewport` - 11 edges

## Surprising Connections (you probably didn't know these)
- `AudioTree` --inherits--> `TreeViewWithTreeModel`  [EXTRACTED]
  CrazyOptimizer/Editor/WindowComponents/AudioOptimizations/AudioTree.cs → CrazyOptimizer/Editor/TreeLib/TreeViewWithTreeModel.cs
- `BuildLogTree` --inherits--> `TreeViewWithTreeModel`  [EXTRACTED]
  CrazyOptimizer/Editor/WindowComponents/BuildLogs/BuildLogTree.cs → CrazyOptimizer/Editor/TreeLib/TreeViewWithTreeModel.cs
- `ModelTree` --inherits--> `TreeViewWithTreeModel`  [EXTRACTED]
  CrazyOptimizer/Editor/WindowComponents/ModelOptimizations/ModelTree.cs → CrazyOptimizer/Editor/TreeLib/TreeViewWithTreeModel.cs
- `TextureTree` --inherits--> `TreeViewWithTreeModel`  [EXTRACTED]
  CrazyOptimizer/Editor/WindowComponents/TextureOptimizations/TextureTree.cs → CrazyOptimizer/Editor/TreeLib/TreeViewWithTreeModel.cs

## Import Cycles
- None detected.

## Communities (30 total, 2 thin omitted)

### Community 0 - "Project Generator Interfaces"
Cohesion: 0.08
Nodes (14): IAssemblyNameProvider, IFileIO, IGUIDGenerator, ILookup, MethodInfo, ProjectGeneration, ScriptingLanguage, StringBuilder (+6 more)

### Community 1 - "Assembly Name Provider"
Cohesion: 0.10
Nodes (11): PackageInfo, AntigravityEditor, AssemblyNameProvider, IAssemblyNameProvider, IPackageInfoCache, Assembly, Dictionary, Func (+3 more)

### Community 2 - "Tree View UI"
Cohesion: 0.13
Nodes (14): CanStartDragArgs, IList, List, string, T, DragAndDropArgs, DragAndDropVisualMode, Object (+6 more)

### Community 3 - "Game Loading & UI"
Cohesion: 0.11
Nodes (13): GameObject, IEnumerator, Image, LoadingDisplayMode, MonoBehaviour, RectTransform, float, DynamicViewport (+5 more)

### Community 4 - "Audio Optimization"
Cohesion: 0.11
Nodes (13): AudioOptimization, CrazyGames.WindowComponents.AudioOptimizations, AudioTree, int, MenuItem, string, bool, HashSet (+5 more)

### Community 5 - "Tree Model Data"
Cohesion: 0.20
Nodes (8): Dictionary, IList, int, List, T, TreeElement, CrazyGames.TreeLib, TreeModel

### Community 6 - "Audio Tree View"
Cohesion: 0.23
Nodes (8): AudioTree, CrazyGames.WindowComponents.AudioOptimizations, AudioTreeItem, IList, MultiColumnHeader, Rect, RowGUIArgs, TreeViewItem

### Community 7 - "Build Log Tree View"
Cohesion: 0.23
Nodes (8): BuildLogTree, CrazyOptimizer.Editor.WindowComponents.BuildLogs, BuildLogTreeItem, IList, MultiColumnHeader, Rect, RowGUIArgs, TreeViewItem

### Community 8 - "Model Tree View"
Cohesion: 0.23
Nodes (8): IList, MultiColumnHeader, Rect, RowGUIArgs, TreeViewItem, CrazyGames.WindowComponents.ModelOptimizations, ModelTree, ModelTreeItem

### Community 9 - "Texture Tree View"
Cohesion: 0.23
Nodes (8): IList, MultiColumnHeader, Rect, RowGUIArgs, TreeViewItem, CrazyGames.WindowComponents.TextureOptimizations, TextureTree, TextureTreeItem

### Community 10 - "Optimization Tree Items"
Cohesion: 0.13
Nodes (11): AudioTreeItem, CrazyGames.WindowComponents.AudioOptimizations, BuildLogTreeItem, CrazyOptimizer.Editor.WindowComponents.BuildLogs, float, string, CrazyGames.WindowComponents.ModelOptimizations, ModelTreeItem (+3 more)

### Community 11 - "Model Optimization"
Cohesion: 0.23
Nodes (7): bool, HashSet, MultiColumnHeaderState, Regex, CrazyGames.WindowComponents.ModelOptimizations, ModelOptimization, ModelTree

### Community 12 - "File IO Provider"
Cohesion: 0.15
Nodes (3): AntigravityEditor, FileIOProvider, IFileIO

### Community 13 - "Texture Optimization"
Cohesion: 0.22
Nodes (7): bool, HashSet, MultiColumnHeaderState, Regex, CrazyGames.WindowComponents.TextureOptimizations, TextureOptimization, TextureTree

### Community 14 - "Build Log Analysis"
Cohesion: 0.23
Nodes (7): BuildLogs, CrazyOptimizer.Editor.WindowComponents.BuildLogs, BuildLogTree, bool, GUIStyle, MultiColumnHeaderState, string

### Community 15 - "Antigravity Discovery"
Cohesion: 0.24
Nodes (7): AntigravityDiscovery, AntigravityEditor, IDiscovery, Name, Path, Installation, List

### Community 16 - "Project File Generation"
Cohesion: 0.18
Nodes (3): AntigravityEditor, IGenerator, SolutionGuidGenerator

### Community 17 - "Sprite Emoji Frames"
Cohesion: 0.17
Nodes (11): frames, meta, app, format, image, scale, size, smartupdate (+3 more)

### Community 18 - "Tree Element Utilities"
Cohesion: 0.32
Nodes (5): HashSet, IList, T, CrazyGames.TreeLib, TreeElementUtility

### Community 19 - "Antigravity Script Editor"
Cohesion: 0.24
Nodes (7): GetInstallationName(), OnGUI(), RegenerateProjectFiles(), SettingsButton(), TryGetInstallationForPath(), Installation, ProjectGenerationFlag

### Community 20 - "Tree Extension Methods"
Cohesion: 0.22
Nodes (7): Func, IEnumerable, T, IOrderedEnumerable, TKey, CrazyGames.TreeLib, TreeExtensionMethods

### Community 21 - "MCP Unity Package Installer"
Cohesion: 0.28
Nodes (4): AddRequest, AddMCPForUnityPackage, MenuItem, string

### Community 22 - "Export Optimizations UI"
Cohesion: 0.28
Nodes (5): Action, Color, GUIStyle, CrazyGames.WindowComponents, ExportOptimizations

### Community 23 - "GUID Generator"
Cohesion: 0.29
Nodes (3): AntigravityEditor, GUIDProvider, IGUIDGenerator

### Community 24 - "Game Setup Editor"
Cohesion: 0.33
Nodes (4): GameSetup, TeenPattiAsia.Editor, Obsolete, MenuItem

### Community 25 - "Tree Element Data"
Cohesion: 0.40
Nodes (5): int, List, string, CrazyGames.TreeLib, TreeElement

### Community 26 - "Optimizer Utilities"
Cohesion: 0.40
Nodes (3): CrazyGames, OptimizerUtils, IReadOnlyList

### Community 27 - "String Utilities"
Cohesion: 0.40
Nodes (3): char, AntigravityEditor, StringUtils

## Knowledge Gaps
- **117 isolated node(s):** `CrazyGames`, `IReadOnlyList`, `CrazyGames`, `int`, `string` (+112 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `TreeViewWithTreeModel` connect `Tree View UI` to `Model Tree View`, `Texture Tree View`, `Audio Tree View`, `Build Log Tree View`?**
  _High betweenness centrality (0.035) - this node is a cross-community bridge._
- **Why does `ProjectGeneration` connect `Project Generator Interfaces` to `Project File Generation`?**
  _High betweenness centrality (0.023) - this node is a cross-community bridge._
- **Why does `BuildLogTree` connect `Build Log Tree View` to `Tree View UI`?**
  _High betweenness centrality (0.012) - this node is a cross-community bridge._
- **What connects `CrazyGames`, `IReadOnlyList`, `CrazyGames` to the rest of the system?**
  _117 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Project Generator Interfaces` be split into smaller, more focused modules?**
  _Cohesion score 0.07740384615384616 - nodes in this community are weakly interconnected._
- **Should `Assembly Name Provider` be split into smaller, more focused modules?**
  _Cohesion score 0.0989247311827957 - nodes in this community are weakly interconnected._
- **Should `Tree View UI` be split into smaller, more focused modules?**
  _Cohesion score 0.12807881773399016 - nodes in this community are weakly interconnected._