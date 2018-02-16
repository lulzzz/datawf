﻿using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections;
using DataWF.Data;
using Xwt;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace DataWF.Data.Gui
{
    public class CellEditorTable : CellEditorList
    {
        protected DBTable table;
        protected DBColumn column;
        protected string viewFilter = string.Empty;

        public CellEditorTable()
            : base()
        {
            dropDownExVisible = true;
            dropDownAutoHide = true;
        }

        public string ViewFilter
        {
            get { return viewFilter; }
            set { viewFilter = value; }
        }

        public DBColumn Column
        {
            get { return column; }
            set { column = value; }
        }

        public TableEditor TableEditor
        {
            get { return DropDown?.Target as TableEditor; }
        }

        public DBTable Table
        {
            get { return table; }
            set
            {
                table = value;
                if (View != null && View.Table != table)
                {
                    View.Dispose();
                    listSource = null;
                    //View = table.CreateView(viewFilter, DBViewInitMode.None, DBStatus.Current);
                    if (TableEditor != null)
                    {
                        TableEditor.Initialize(View, EditItem is DBItem ? (DBItem)EditItem : null, column, TableFormMode.Reference, false);
                    }
                }
            }
        }

        public IDBTableView View
        {
            get
            {
                if (listSource == null && table != null)
                    listSource = table.CreateItemsView(viewFilter, DBViewKeys.None, DBStatus.Current);
                return listSource as IDBTableView;
            }
            set
            {
                listSource = value;
                if (View != null && View.Table != table)
                    table = View.Table;
            }
        }

        public DBItem GetItem(object obj, object source)
        {
            if (!(obj is DBItem))
                if (source is DBItem && column != null)
                {
                    var row = (DBItem)source;
                    if (row[column].Equals(obj))
                    {
                        obj = row.GetReference(column, DBLoadParam.None);
                        if (obj == null && row.GetCache(column) == null)
                        {
                            getReferenceStack.Push(new PDBTableParam() { Row = row, Column = column });
                            if (getReferenceStack.Count == 1)
                            {
                                ThreadPool.QueueUserWorkItem(p => LoadReference());
                            }
                            obj = DBItem.EmptyItem;
                        }
                    }
                    else
                        obj = column.ReferenceTable.LoadItemById(obj);
                }
                else if (table != null)
                    obj = table.LoadItemById(obj);
            return obj as DBItem;
        }

        public struct PDBTableParam
        {
            public DBItem Row;
            public DBColumn Column;
        }

        private ConcurrentStack<PDBTableParam> getReferenceStack = new ConcurrentStack<PDBTableParam>();

        private void LoadReference()
        {
            using (var transaction = new DBTransaction(Table.Schema.Connection))
            {
                Debug.WriteLine("Get References {0}", getReferenceStack.Count);
                PDBTableParam item;
                while (getReferenceStack.TryPop(out item))
                {
                    item.Row.GetReference(item.Column, DBLoadParam.Load, transaction);
                    item.Row.OnPropertyChanged(item.Column.Name, item.Column);
                }
            }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value == null || value == DBNull.Value)
                return null;
            value = GetItem(value, dataSource);

            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value != null && (value.GetType() == valueType))
                return value;
            if (value is DBItem)
            {
                if (column != null || TypeHelper.IsBaseType(value.GetType(), valueType))
                    return value;
                return ((DBItem)value).PrimaryId;
            }
            if (value is string)
            {
                return table.LoadItemById(value, DBLoadParam.None);
            }
            return base.ParseValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            var tableEditor = editor.GetCacheControl<TableEditor>("TableEditor");

            tableEditor.ReadOnly = ReadOnly;
            tableEditor.KeyPressed -= TextBoxKeyPress;

            if (table != null)
            {
                editor.DropDownExClick += OnDropDownExClick;
                tableEditor.Initialize(View, EditItem is DBItem ? (DBItem)EditItem : null, column, TableFormMode.Reference, false);
                if (!ReadOnly)
                {
                    tableEditor.ItemSelect += OnTableControlRowSelect;
                }
                if (viewFilter != null && viewFilter.Length > 0)
                {
                    View.DefaultFilter = viewFilter;
                }
            }
            return tableEditor;
        }

        private void OnDropDownExClick(object sender, EventArgs e)
        {
            var row = GetItem(editor.Value, EditItem);
            if (editor != null && row != null)
            {
                using (var te = new TableExplorer())
                {
                    te.Initialize(row, TableFormMode.Item, false);
                    te.ShowDialog(editor);
                }
            }
        }

        protected override void ListReset()
        {
            View.Filter = string.Empty;
        }

        protected override IEnumerable ListFind(string filter)
        {
            IList list = null;

            if (Table.CodeKey != null)
            {
                DBItem item = Table.LoadItemByCode(filter.Trim(), Table.CodeKey, Table.IsSynchronized ? DBLoadParam.None : DBLoadParam.Load | DBLoadParam.Synchronize);
                if (item != null)
                    list = new object[] { item };
            }
            if (list == null || list.Count == 0)
            {
                var q = new QQuery(string.Empty, Table);
                q.SimpleFilter(EditorText);
                View.Filter = q.ToWhere();
                list = View;
            }
            return list;
        }

        protected override void ListSelect(IEnumerable flist)
        {
            if (flist != null)
            {
                //((TableEditor)tool.Target).List.SelectedValues._Clear();
                //((TableEditor)tool.Target).List.SelectedValues.AddRange(flist);
                //((TableEditor)tool.Target).List.VScrollToItem(flist[0]);
                foreach (var item in flist)
                {
                    TableEditor.List.SelectedItem = item;
                    break;
                }
            }
            else
            {
                //((TableEditor)tool.Target).List.SelectedValues.Clear();
            }
        }

        private void OnTableControlRowSelect(object sender, ListEditorEventArgs e)
        {
            if (TableEditor != null)
            {
                var item = (DBItem)e.Item;
                Value = ParseValue(item);
                ((TextEntry)editor.Widget).Changed -= OnControlValueChanged;
                ((TextEntry)editor.Widget).Text = item.ToString();
                ((TextEntry)editor.Widget).Changed += OnControlValueChanged;
                DropDown.Hide();
            }
        }

        public override void FreeEditor()
        {
            if (editor != null)
            {
                editor.DropDownExClick -= OnDropDownExClick;
            }
            if (TableEditor != null)
            {
                TableEditor.ItemSelect -= OnTableControlRowSelect;
            }
            base.FreeEditor();
        }

    }
}
