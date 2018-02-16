﻿using System;
using System.Collections.Generic;
using Xwt.Drawing;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Data;
using Xwt;

namespace DataWF.Data.Gui
{
    public class TableExplorer : ListExplorer
    {
        public TableExplorer() : base(new TableEditor())
        {
            TableEditor.ShowDetails = true;
            TableEditor.Updating += TableUpdating;
            TableEditor.Updated += TableUpdated;

            Name = "TableExplorer";
        }

        public TableEditor TableEditor
        {
            get { return (TableEditor)Editor; }
        }

        public List<DBItem> Rezult
        {
            get { return TableEditor.SelectedRows; }
        }

        public bool ShowList { get; set; }

        private void TableEditOnReferenceClick(object sender, TableEditorReferenceEventArgs e)
        {
            if (Current != null)
            {
                foreach (TableExplorerNode table in Current.Childs)
                {
                    if (table.Name == e.Relation.Name)
                    {
                        Current = table;
                        return;
                    }
                }
            }
        }

        public void Initialize(DBItem row, TableFormMode openmode, bool readOnly)
        {
            Initialize(row.Table, row, null, openmode, readOnly);
        }

        public void Initialize(DBTable table, DBItem row, DBColumn ownColumn, TableFormMode openmode, bool readOnly)
        {
            if (Name == "")
                Name = table.Name + ownColumn?.Name;

            TableExplorerNode node = null;
            if (openmode == TableFormMode.Item)
            {
                node = SelectRow(null, table, row, ownColumn, readOnly);
            }
            else
            {
                node = InitToolTable(table, row, ownColumn, openmode, readOnly);
                Tree.Nodes.Add(node);
            }
            Current = node;
        }

        public TableExplorerNode InitToolTable(DBTable table, DBItem row, DBColumn ownColumn, TableFormMode openmode, bool readOnly)
        {
            TableExplorerNode node = Find(table, ownColumn, row);
            if (node == null)
            {
                node = new TableExplorerNode()
                {
                    Info = new TableEditorInfo()
                    {
                        Table = table,
                        TableView = openmode == TableFormMode.Item ? null : table.CreateItemsView("", DBViewKeys.None, DBStatus.Current),
                        Item = row,
                        Column = ownColumn,
                        Mode = openmode,
                        ReadOnly = readOnly
                    }
                };
            }
            return node;
        }

        public TableExplorerNode Find(DBTable table, DBColumn column, DBItem row)
        {
            if (table == null)
                return null;
            string findParam = table.Name + column?.Name + row?.PrimaryId;
            return Tree.Nodes[findParam] as TableExplorerNode;
        }

        protected override void OnNodeCheck(ListExplorerNode node)
        {
            var tableNode = node as TableExplorerNode;
            if (tableNode == null)
            {
                base.OnNodeCheck(node);
            }
            else if (tableNode.Info.Mode == TableFormMode.Item)
            {
                int index = 0;
                foreach (DBForeignKey key in tableNode.Info.Item.Table.GetChildRelations())
                {
                    var refNode = InitToolTable(key.Table,
                                                      tableNode.Info.Item,
                                                      key.Column,
                                                      TableFormMode.Referencing,
                                                      TableEditor.ReadOnly);
                    refNode.ToolParent = tableNode;
                    refNode.Index = index++;
                    Tree.Nodes.Add(refNode);
                }
            }
            node.Check = true;
        }

        public TableExplorerNode SelectRow(TableExplorerNode owner, DBTable table, DBItem row, DBColumn column, bool readOnly)
        {
            var node = InitToolTable(table, row, column, TableFormMode.Item, readOnly);
            if (node.ToolParent == null)
                node.ToolParent = owner;
            if (owner != null)
                node.Index = owner.Childs.Count - 1;
            Current = node;
            return node;
        }

        #region Universal Form Events

        private void TableUpdated(object sender, EventArgs e)
        {
        }

        private void TableUpdating(object sender, EventArgs e)
        {
        }

        protected override void OnEditorItemSelect(object sender, ListEditorEventArgs e)
        {
            if (Current == null || e.Item == null)
                return;
            var item = (DBItem)e.Item;
            if (item.PrimaryId == null)
                return;
            //TableControl sndr = sender as TableControl;
            SelectRow(Current as TableExplorerNode, item.Table, item, null, TableEditor.ReadOnly);
        }

        #endregion

        private void toolClose_Click(object sender, EventArgs e)
        {
            if (Current == null)
                return;
            CloseNode(Current as TableExplorerNode);
        }

        protected override void Dispose(bool disposing)
        {
            for (int i = 0; i < Tree.Nodes.Count; i++)
            {
                var tn = Tree.Nodes[i];
                DisposeNode(tn as TableExplorerNode);
            }

            base.Dispose(disposing);
        }

        public void DisposeNode(TableExplorerNode node)
        {
            if (node == null)
                return;
            foreach (var child in node.Childs)
                DisposeNode(child as TableExplorerNode);
            node.Dispose();
        }

        public bool CloseNode(TableExplorerNode node)
        {
            if (node == null)
                return false;
            for (int i = 0; i < node.Childs.Count;)
            {
                var tnch = node.Childs[i];
                if (!CloseNode(tnch as TableExplorerNode))
                    return false;
                i++;
            }
            var info = node.Info;
            if ((info.TableView != null && info.TableView.IsEdited) || (info.Mode == TableFormMode.Item && info.Item != null && info.Item.IsChanged))
            {
                var command = MessageDialog.AskQuestion("Closing", Locale.Get("TableEditor", "Data was changed! Save?"), Command.No, Command.Yes);
                if (command == Command.Yes)
                {
                    if (info.TableView != null)
                        info.TableView.Save();
                    else if (info.Item != null && node.Info.Mode == TableFormMode.Item)
                        info.Item.Save();
                }
                else if (command != Command.No)
                {
                    return false;
                }
            }
            node.Close();
            node.Dispose();
            return true;
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "TableExplorer", "Table Explorer", GlyphType.Table);
        }
    }
}