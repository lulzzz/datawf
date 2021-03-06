﻿using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.Data.Gui
{
    public class TableExplorer : ListExplorer
    {
        private DBTable table;

        public TableExplorer() : base(new TableEditor())
        {
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
                foreach (TableExplorerNode table in Current.Nodes)
                {
                    if (table.Name == e.Relation.Name)
                    {
                        Current = table;
                        return;
                    }
                }
            }
        }

        public DBTable Table
        {
            get { return table; }
            set
            {
                if (Table == value)
                    return;
                Initialize(value, null, null, TableEditorMode.Table, false);
            }
        }

        public void Initialize(DBItem row, TableEditorMode openmode, bool readOnly)
        {
            Initialize(row.Table, row, null, openmode, readOnly);
        }

        public void Initialize(DBTable table, DBItem row, DBColumn ownColumn, TableEditorMode openmode, bool readOnly)
        {
            if (this.table == null)
            {
                this.table = table;
            }

            if (Name == "")
            {
                Name = table.Name + ownColumn?.Name;
            }

            TableExplorerNode node = null;
            if (openmode == TableEditorMode.Item)
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

        public TableExplorerNode InitToolTable(DBTable table, DBItem row, DBColumn ownColumn, TableEditorMode openmode, bool readOnly)
        {
            TableExplorerNode node = Find(table, ownColumn, row);
            if (node == null)
            {
                node = new TableExplorerNode()
                {
                    Info = new TableEditorInfo()
                    {
                        Table = table,
                        TableView = openmode == TableEditorMode.Item ? null
                        : table.CreateItemsView("", DBViewKeys.None, DBStatus.Actual | DBStatus.Edit | DBStatus.New | DBStatus.Error),
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

        protected override void OnNodeSelect(ListExplorerNode node)
        {
            base.OnNodeSelect(node);
            if (node is TableExplorerNode tableNode)
            {
                Text = $"{tableNode.Info.Table.DisplayName}{(tableNode.Info.Item == null ? string.Empty : " ")}{tableNode.Info.Item?.ToString()}";
            }
        }

        protected override void OnNodeCheck(ListExplorerNode node)
        {
            if (!(node is TableExplorerNode tableNode))
            {
                base.OnNodeCheck(node);
            }
            else if (tableNode.Info.Mode == TableEditorMode.Item)
            {
                int index = 0;
                foreach (DBForeignKey key in tableNode.Info.Item.Table.GetChildRelations())
                {
                    var refNode = InitToolTable(key.Table,
                                                      tableNode.Info.Item,
                                                      key.Column,
                                                      TableEditorMode.Referencing,
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
            var node = InitToolTable(table, row, column, TableEditorMode.Item, readOnly);
            if (node.ToolParent == null)
                node.ToolParent = owner;
            if (owner != null)
                node.Index = owner.Nodes.Count - 1;
            Current = node;
            return node;
        }

        #region Universal Form Events

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

        private async void OnToolCloseClick(object sender, EventArgs e)
        {
            if (Current == null)
                return;
            await CloseNode(Current as TableExplorerNode);
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
            foreach (var child in node.Nodes)
                DisposeNode(child as TableExplorerNode);
            node.Dispose();
        }

        public async Task<bool> CloseNode(TableExplorerNode node)
        {
            if (node == null)
                return false;
            for (int i = 0; i < node.Nodes.Count;)
            {
                var tnch = node.Nodes[i];
                if (!await CloseNode(tnch as TableExplorerNode))
                    return false;
                i++;
            }
            var info = node.Info;
            if ((info.TableView != null && info.TableView.IsEdited) || (info.Mode == TableEditorMode.Item && info.Item != null && info.Item.IsChanged))
            {
                var command = MessageDialog.AskQuestion("Closing", Locale.Get("TableEditor", "Data was changed! Save?"), Command.No, Command.Yes);
                if (command == Command.Yes)
                {
                    if (info.TableView != null)
                        await info.TableView.Save();
                    else if (info.Item != null && node.Info.Mode == TableEditorMode.Item)
                        await info.Item.Save(GuiEnvironment.User);
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
            //GuiService.Localize(this, "TableExplorer", "Table Explorer", GlyphType.Table);
        }

        public override void Serialize(ISerializeWriter writer)
        {
            writer.WriteAttribute("TableName", Table?.FullName);
        }

        public override void Deserialize(ISerializeReader reader)
        {
            var tableName = reader.ReadAttribute<string>("TableName");
            if (!string.IsNullOrEmpty(tableName))
            {
                Table = DBService.Schems.ParseTable(tableName);
            }
        }
    }
}
