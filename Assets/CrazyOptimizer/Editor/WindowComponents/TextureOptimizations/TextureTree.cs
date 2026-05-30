using System;
using System.Collections.Generic;
using System.Linq;
using CrazyGames.TreeLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CrazyGames.WindowComponents.TextureOptimizations
{
    class TextureTree : TreeViewWithTreeModel<TextureTreeItem>
    {
        public TextureTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader,
            TreeModel<TextureTreeItem> model)
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

            // Cast once; the original pre-sort by TextureName was immediately overwritten — removed.
            // NOTE: original case 4 and 5 both sorted by CrunchCompressionQuality (bug);
            //       case 4 should sort by HasCrunchCompression (the crunch on/off column).
            IOrderedEnumerable<TreeViewItem<TextureTreeItem>> sorted = sortedColumnIndex switch
            {
                0 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.TextureName, ascending),
                1 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.TextureType, ascending),
                2 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.TextureMaxSize, ascending),
                3 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.TextureCompressionName, ascending),
                4 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.HasCrunchCompression, ascending),
                5 => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.CrunchCompressionQuality, ascending),
                _ => rootItem.children.Cast<TreeViewItem<TextureTreeItem>>().Order(i => i.data.TextureName, ascending)
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
            var item = (TreeViewItem<TextureTreeItem>)args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
        }

        void CellGUI(Rect cellRect, TreeViewItem<TextureTreeItem> item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    GUI.Label(cellRect, item.data.TextureName);
                    break;
                case 1:
                    GUI.Label(cellRect, item.data.TextureType.ToString());
                    break;
                case 2:
                    GUI.Label(cellRect, item.data.TextureMaxSize.ToString());
                    break;
                case 3:
                    GUI.Label(cellRect, item.data.TextureCompressionName);
                    break;
                case 4:
                    GUI.Label(cellRect, item.data.HasCrunchCompression ? "yes" : "no");
                    break;
                case 5:
                    GUI.Label(cellRect, item.data.CrunchCompressionQuality.ToString());
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

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.TexturePath);
        }
    }
}