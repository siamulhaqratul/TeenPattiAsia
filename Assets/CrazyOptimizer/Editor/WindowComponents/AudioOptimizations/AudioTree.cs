using System;
using System.Collections.Generic;
using System.Linq;
using CrazyGames.TreeLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CrazyGames.WindowComponents.AudioOptimizations
{
    class AudioTree : TreeViewWithTreeModel<AudioTreeItem>
    {
        public AudioTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader, TreeModel<AudioTreeItem> model)
            : base(treeViewState, multiColumnHeader, model)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            var sortedColumnIndex = sortedColumns[0];
            var ascending = multiColumnHeader.IsSortedAscending(sortedColumnIndex);

            // Cast once; the initial OrderBy on line 38 in the original was wasted work
            // because the switch always overwrote it — now we sort directly.
            IOrderedEnumerable<TreeViewItem<AudioTreeItem>> sorted = sortedColumnIndex switch
            {
                0 => rootItem.children.Cast<TreeViewItem<AudioTreeItem>>().Order(i => i.data.AudioName, ascending),
                1 => rootItem.children.Cast<TreeViewItem<AudioTreeItem>>().Order(i => i.data.LoadType, ascending),
                2 => rootItem.children.Cast<TreeViewItem<AudioTreeItem>>().Order(i => i.data.Quality, ascending),
                _ => rootItem.children.Cast<TreeViewItem<AudioTreeItem>>().Order(i => i.data.AudioName, ascending)
            };

            rootItem.children = sorted.Cast<TreeViewItem>().ToList();
            TreeToList(root, rows);
            Repaint();
        }

        public static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            result.Clear();

            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                        stack.Push(current.children[i]);
                }
            }
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<AudioTreeItem>)args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
        }

        void CellGUI(Rect cellRect, TreeViewItem<AudioTreeItem> item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    GUI.Label(cellRect, item.data.AudioName);
                    break;
                case 1:
                    GUI.Label(cellRect, item.data.LoadType);
                    break;
                case 2:
                    // Quality is now an int property; convert once here
                    GUI.Label(cellRect, item.data.Quality.ToString());
                    break;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 0)
                return;

            var item = treeModel.Find(selectedIds[0]);
            if (item == null)
                return;

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.AudioPath);
        }
    }
}