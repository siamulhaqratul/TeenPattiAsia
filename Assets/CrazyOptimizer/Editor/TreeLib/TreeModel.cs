using System;
using System.Collections.Generic;
using System.Linq;

namespace CrazyGames.TreeLib
{
    // The TreeModel is a utility class working on a list of serializable TreeElements where the order and the depth of each TreeElement define
    // the tree structure. Note that the TreeModel itself is not serializable (in Unity we are currently limited to serializing lists/arrays) but the
    // input list is.
    // The tree representation (parent and children references) are then built internally using TreeElementUtility.ListToTree (using depth
    // values of the elements).
    // The first element of the input list is required to have depth == -1 (the hidden root) and the rest to have
    // depth >= 0 (otherwise an exception will be thrown).

    public class TreeModel<T> where T : TreeElement
    {
        IList<T> m_Data;
        int m_MaxID;

        // Fast O(1) ID -> element lookup, kept in sync with m_Data
        Dictionary<int, T> m_IDLookup;

        public T root { get; set; }

        public event Action modelChanged;

        public int numberOfDataElements => m_Data.Count;

        public TreeModel(IList<T> data)
        {
            SetData(data);
        }

        public T Find(int id)
        {
            m_IDLookup.TryGetValue(id, out var element);
            return element;
        }

        public void SetData(IList<T> data)
        {
            Init(data);
        }

        void Init(IList<T> data)
        {
            m_Data = data ?? throw new ArgumentNullException(nameof(data), "Input data is null. Ensure input is a non-null list.");

            // Build fast lookup and track max ID in one pass
            m_IDLookup = new Dictionary<int, T>(m_Data.Count);
            m_MaxID = int.MinValue;
            foreach (var element in m_Data)
            {
                m_IDLookup[element.Id] = element;
                if (element.Id > m_MaxID)
                    m_MaxID = element.Id;
            }
            if (m_MaxID == int.MinValue)
                m_MaxID = 0;

            if (m_Data.Count > 0)
                root = TreeElementUtility.ListToTree(data);
        }

        public int GenerateUniqueID()
        {
            return ++m_MaxID;
        }

        public IList<int> GetAncestors(int id)
        {
            var parents = new List<int>();
            TreeElement element = Find(id);
            while (element?.parent != null)
            {
                parents.Add(element.parent.Id);
                element = element.parent;
            }
            return parents;
        }

        public IList<int> GetDescendantsThatHaveChildren(int id)
        {
            T searchFromThis = Find(id);
            if (searchFromThis != null)
                return GetParentsBelowStackBased(searchFromThis);

            return new List<int>();
        }

        IList<int> GetParentsBelowStackBased(TreeElement searchFromThis)
        {
            Stack<TreeElement> stack = new();
            stack.Push(searchFromThis);

            var parentsBelow = new List<int>();
            while (stack.Count > 0)
            {
                TreeElement current = stack.Pop();
                if (current.hasChildren)
                {
                    parentsBelow.Add(current.Id);
                    foreach (var child in current.children)
                        stack.Push(child);
                }
            }

            return parentsBelow;
        }

        public void RemoveElements(IList<int> elementIDs)
        {
            IList<T> elements = m_Data.Where(element => elementIDs.Contains(element.Id)).ToArray();
            RemoveElements(elements);
        }

        public void RemoveElements(IList<T> elements)
        {
            foreach (var element in elements)
            {
                if (element == root)
                    throw new ArgumentException("It is not allowed to remove the root element");
            }

            var commonAncestors = TreeElementUtility.FindCommonAncestorsWithinList(elements);

            foreach (var element in commonAncestors)
            {
                element.parent.children.Remove(element);
                element.parent = null;
            }

            TreeElementUtility.TreeToList(root, m_Data);
            RebuildLookup();

            Changed();
        }

        public void AddElements(IList<T> elements, TreeElement parent, int insertPosition)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements), "elements is null");
            if (elements.Count == 0)
                throw new ArgumentException("elements Count is 0: nothing to add", nameof(elements));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "parent is null");

            (parent.children ??= new List<TreeElement>()).InsertRange(insertPosition, elements.Cast<TreeElement>());
            foreach (var element in elements)
            {
                element.parent = parent;
                element.depth = parent.depth + 1;
                TreeElementUtility.UpdateDepthValues(element);
            }

            TreeElementUtility.TreeToList(root, m_Data);
            RebuildLookup();

            Changed();
        }

        public void AddRoot(T root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root), "root is null");

            if (m_Data == null)
                throw new InvalidOperationException("Internal Error: data list is null");

            if (m_Data.Count != 0)
                throw new InvalidOperationException("AddRoot is only allowed on empty data list");

            root.Id = GenerateUniqueID();
            root.depth = -1;
            m_Data.Add(root);
            m_IDLookup[root.Id] = root;
        }

        public void AddElement(T element, TreeElement parent, int insertPosition)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element), "element is null");
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "parent is null");

            (parent.children ??= new List<TreeElement>()).Insert(insertPosition, element);
            element.parent = parent;

            TreeElementUtility.UpdateDepthValues(parent);
            TreeElementUtility.TreeToList(root, m_Data);
            RebuildLookup();

            Changed();
        }

        public void MoveElements(TreeElement parentElement, int insertionIndex, List<TreeElement> elements)
        {
            if (insertionIndex < 0)
                throw new ArgumentException("Invalid input: insertionIndex is -1, client needs to decide what index elements should be reparented at");

            // Invalid reparenting input
            if (parentElement == null)
                return;

            // Adjust insertion index: items above the insertion point that are being moved will be removed first
            if (insertionIndex > 0)
                insertionIndex -= parentElement.children.GetRange(0, insertionIndex).Count(elements.Contains);

            // Remove dragged items from their old parents
            foreach (var draggedItem in elements)
            {
                draggedItem.parent.children.Remove(draggedItem);
                draggedItem.parent = parentElement;
            }

            (parentElement.children ??= new List<TreeElement>()).InsertRange(insertionIndex, elements);

            TreeElementUtility.UpdateDepthValues(root);
            TreeElementUtility.TreeToList(root, m_Data);
            // MoveElements doesn't add/remove IDs, so no lookup rebuild needed

            Changed();
        }

        // Rebuild the ID lookup from current m_Data (call after structural changes)
        void RebuildLookup()
        {
            m_IDLookup.Clear();
            foreach (var element in m_Data)
                m_IDLookup[element.Id] = element;
        }

        void Changed()
        {
            modelChanged?.Invoke();
        }
    }
}