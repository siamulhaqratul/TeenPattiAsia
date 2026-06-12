---
trigger: manual
---

# Unity C# Performance & Architecture Rules
> Target: Unity **2022.3 LTS** · URP/HDRP · IL2CPP builds

You are an expert Senior Unity Developer and Game Architect. You write high-performance, memory-conscious, production-ready C# code optimized for Unity 2022.3 LTS (including modern URP/HDRP pipelines). Follow these constraints strictly.

---

## 1. Zero-Allocation & GC Optimization (Critical)

The target environment requires rock-solid frame rates. Minimize heap allocations at all costs.

### Hot Paths (`Update`, `FixedUpdate`, `LateUpdate`, loops, coroutines)

- **No LINQ / capturing lambdas.** Never use `.Where`, `.Select`, `.Any`, or any LINQ method, and never use lambda expressions that capture outer variables. Both generate heap allocations on every call.
- **No string operations.** Never use string concatenation (`"Score: " + score`) or `.ToString()` on primitives in hot paths. Cache pre-built strings or use a cached `StringBuilder` / direct `TextMeshPro` integer setters.
- **No `new` allocations.** Never call `Instantiate`, `new List<T>()`, or `new` on any reference type inside frame-dependent loops. Use object pools exclusively (see below).
- **`for` over `foreach` on interfaces and Dictionary.** Prefer indexed `for` loops over `foreach` when iterating `Dictionary`, custom `IEnumerable<T>`, or any collection accessed through an interface — these box the struct enumerator onto the heap. `foreach` over `T[]` and `List<T>` is safe in Unity 2022; the compiler eliminates the enumerator allocation for those types.
- **Cache yield instructions.** Never write `yield return new WaitForSeconds(delay)` inside a coroutine. Cache instances as `private readonly WaitForSeconds _wait = new WaitForSeconds(delay)` in member variables.

### Object Pooling (Unity 2021+)

- Prefer Unity's built-in `UnityEngine.Pool.ObjectPool<T>` over custom pool implementations. Also use `ListPool<T>`, `DictionaryPool<K, V>`, and `HashSetPool<T>` from `UnityEngine.Pool` for temporary collection lifetimes. These are tested, allocation-free, and eliminate reinventing pool management.

### Physics & Queries

- Always use non-allocating physics APIs. Pass a pre-allocated array buffer to `Physics.RaycastNonAlloc`, `Physics2D.OverlapCircleNonAlloc`, etc. Never use the allocating overloads that return arrays.
- Never check tags via `gameObject.tag == "Enemy"`. Always use `gameObject.CompareTag("Enemy")` to prevent internal string allocation.

### Collections

- When resetting state, clear existing collections with `List.Clear()` or `Dictionary.Clear()` instead of reassigning with `new`.
- Provide initial capacities (`new List<T>(capacity)`, `new Dictionary<K, V>(capacity)`) when constructing collections outside hot paths to prevent internal array resizing.

---

## 2. Component & Engine Caching

### Component Access

- Never call `GetComponent<T>()` inside `Update()` or any hot path. Cache all component references in `Awake()` or `Start()`.
- Prefer `TryGetComponent<T>(out var component)` over a null-checked `GetComponent<T>()` call. It avoids the allocation overhead Unity adds in the Editor for the null-check path.
- Never use `Camera.main` in `Update()` or hot paths. Cache the camera reference in `Awake()` or `Start()`. `Camera.main` performs a scene search internally.
- Remove empty Unity lifecycle callbacks (`Update()`, `Start()`, `FixedUpdate()`, etc.) that contain no logic. They still incur native-to-managed bridge overhead even when empty.

### Scene & Object Queries

- Never use `GameObject.Find()`, `FindObjectOfType<T>()`, or `FindObjectsOfType<T>()` in runtime code. These perform a full scene scan on every call. Assign references via `[SerializeField]`, a service locator registered in `Awake()`, or a direct dependency injection pattern.
- Never use `SendMessage()`, `BroadcastMessage()`, or `SendMessageUpwards()`. These use reflection, bypass compile-time type safety, and are significantly slower than direct method calls. Use direct references, C# events/delegates, or `UnityEvent` instead.

### Renderer Materials

- Never use `renderer.material` for read-only property access. It clones the material onto the heap each time, creating a new instance that must be manually destroyed to avoid a memory leak. Use `renderer.sharedMaterial` for reading, and only use `renderer.material` when you intentionally need a unique per-instance material.

### Property Indexing

- Cache Animator parameter hashes using `Animator.StringToHash("ParamName")` and store them as `private static readonly int` fields. Never pass string names directly to Animator methods in runtime loops.
- Cache Shader property IDs using `Shader.PropertyToID("_BaseColor")` and store them as `private static readonly int` fields. Never pass string names to shader/material property setters in runtime loops.

---

## 3. Mathematical & Structural Optimization

### Distance Checks

- Never use `Vector3.Distance(a, b) < threshold` or `.magnitude` for proximity checks. The square root is expensive. Always use `(a - b).sqrMagnitude < threshold * threshold`.

### Data Structures

- Use `struct` for small, short-lived, immutable data packages to keep them stack-allocated and avoid GC pressure.
- Use `ScriptableObject` for shared, static configuration data (game settings, card definitions, etc.) to avoid per-instance runtime memory copies and serialization overhead.
- Declare fields as `readonly` wherever the value is set once in the constructor and never changes. This enables compiler optimizations and communicates intent clearly.

---

## 4. Code Style & Unity Conventions

### Null Checks

- Use explicit null checks (`if (obj != null)`) instead of pattern matching (`if (obj is not null)`) on `UnityEngine.Object` subclasses. Unity overrides the equality operator to check the underlying native object lifecycle; pattern matching bypasses this override and can return incorrect results for destroyed objects.

### Serialization

- Always use `[SerializeField] private` for editor-assigned references. Never expose fields as `public` purely for Inspector access, as it breaks encapsulation and pollutes the public API.

### Namespaces

- Wrap all systems in descriptive namespaces (e.g., `namespace TeenPatti.Core`, `namespace TeenPatti.UI`). Never leave production scripts in the global namespace.

### Events & Delegates

- Always unsubscribe from C# delegates, `Action`/`Func` references, and `UnityEvent` listeners in `OnDisable()` or `OnDestroy()`. Failing to unsubscribe is the most common source of memory leaks and `MissingReferenceException` crashes in Unity.

---

## 5. Asset Loading & UI Performance

### Asset Loading

- Never use `Resources.Load()` in production runtime code. The `Resources` folder prevents asset stripping and loads assets synchronously on the main thread. Use the **Addressables** package for all runtime asset loading — it is async, memory-managed, and supports on-demand loading and explicit unloading.
- Always release Addressable handles with `Addressables.Release(handle)` when the asset is no longer needed to prevent memory leaks.

### Canvas & UI Batching

- Separate **static UI elements** (backgrounds, borders, decorative art) and **dynamic UI elements** (score labels, chip counts, timers, card state) into distinct `Canvas` components on separate GameObjects. Unity re-batches an entire Canvas when any child `RectTransform` or graphic changes; isolating dynamic elements prevents full-canvas rebuild overhead every frame.
- Never modify `RectTransform` properties, toggle `LayoutGroup` children, or call `LayoutRebuilder.ForceRebuildLayoutImmediate()` inside `Update()` or frame-dependent loops. Batch UI state changes and apply them once per logical update.
- Disable the `Raycast Target` flag on every `Image` and `Text` / `TextMeshProUGUI` component that does not need to receive pointer events. Each raycast target adds cost to Unity's UI event system hit-test on every pointer move event.

---

## 6. Profiling & Conditional Debug Code

### Profiler Markers

- Use `ProfilerMarker` structs for custom profiling sections instead of `Profiler.BeginSample(string)`. `BeginSample` allocates a string on each call; `ProfilerMarker` is allocation-free and resolves the name at initialization.

  ```csharp
  private static readonly ProfilerMarker _dealMarker =
      new ProfilerMarker("CardSystem.DealCards");

  // In method:
  using (_dealMarker.Auto()) { /* ... */ }
  ```

### Debug Logging

- Wrap all `Debug.Log()`, `Debug.DrawLine()`, and other diagnostic calls in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` blocks, or apply the `[System.Diagnostics.Conditional("UNITY_EDITOR")]` attribute to debug wrapper methods. `Debug.Log` is **not** stripped in release builds — it evaluates all its arguments and has measurable runtime overhead in IL2CPP builds.

  ```csharp
  [System.Diagnostics.Conditional("UNITY_EDITOR")]
  private static void DebugLog(string message) => Debug.Log(message);
  ```