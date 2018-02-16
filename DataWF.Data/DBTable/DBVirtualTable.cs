﻿/*
 DBTable.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.ComponentModel;
using DataWF.Common;
using System.Xml.Serialization;
using System.Text;

namespace DataWF.Data
{
    public interface IDBVirtualTable
    {
        DBTable BaseTable { get; set; }
        QQuery FilterQuery { get; }
        void Refresh();
        void CheckItem(ListChangedType type, DBItem item, string property);
    }

    public class DBVirtualTable<T> : DBTable<T>, IDBVirtualTable where T : DBVirtualItem, new()
    {
        private DBTable cacheBaseTable;
        protected string baseTableName;
        private QQuery filterQuery;

        [XmlIgnore, Browsable(false)]
        public QQuery FilterQuery
        {
            get
            {
                if (BaseTable == null)
                    return null;
                return filterQuery ?? (filterQuery = new QQuery(query, BaseTable));
            }
        }

        [XmlAttribute, Browsable(false), Category("Database")]
        public string BaseTableName
        {
            get { return baseTableName; }
            set
            {
                if (baseTableName != value)
                {
                    baseTableName = value;
                    cacheBaseTable = null;
                    filterQuery = null;
                    OnPropertyChanged(nameof(BaseTableName), true);
                }
            }
        }

        [XmlIgnore, Category("Database")]
        public DBTable BaseTable
        {
            get
            {
                if (cacheBaseTable == null && baseTableName != null)
                    cacheBaseTable = Schema == null ? null : Schema.Tables[baseTableName];
                return cacheBaseTable;
            }
            set
            {
                if (BaseTable == value)
                    return;
                BaseTableName = value?.Name;
                sequenceName = value.SequenceName;
                cacheBaseTable = value;
            }
        }

        public void CheckItem(ListChangedType type, DBItem item, string property)
        {
            var view = item.GetVirtual(this);
            if (type == ListChangedType.ItemChanged)
            {
                if ((view == null || !view.Attached) && Query.Contains(property) && BaseTable.CheckItem(item, FilterQuery))
                    Add(view ?? New(item));
            }
            else if (type == ListChangedType.ItemAdded)
            {
                if ((view == null || !view.Attached) && BaseTable.CheckItem(item, FilterQuery))
                    Add(view ?? New(item));
            }
            else if (type == ListChangedType.ItemDeleted && view != null)
            {
                Remove(view);
            }
        }

        public void Refresh()
        {
            items.Clear();
            foreach (DBItem item in BaseTable.SelectItems(FilterQuery))
            {
                var newRow = item.GetVirtual(this);
                if (newRow == null)
                    newRow = New(item);
                Add((T)newRow);
            }
        }

        public void GenerateColumns()
        {
            columnGroups.Clear();
            foreach (DBColumnGroup @group in BaseTable.ColumnGroups)
            {
                var newGroup = (DBColumnGroup)@group.Clone();
                columnGroups.Add(newGroup);
            }
            columns.Clear();
            foreach (DBColumn col in BaseTable.Columns)
            {
                var newCol = new DBVirtualColumn(col);
                columns.Add(newCol);
                if (col.LocalizeInfo.Names.Count > 0)
                {
                    newCol.LocalizeInfo.Names.Add(col.LocalizeInfo.Names[0].Value, col.LocalizeInfo.Names[0].Culture);
                }
            }
        }

        public override void Add(T item)
        {
            base.Add(item);
            if (item.Main != null && !item.Main.Attached)
                BaseTable.Add(item.Main);
        }

        public override void OnListChanged(DBItem item, string property, ListChangedType type)
        {
            if (type == ListChangedType.ItemChanged && query.Contains(property))
            {
                var r = ((T)item).Main;
                if (r != null && !BaseTable.CheckItem(r, FilterQuery))
                    Remove((T)item);
                return;
            }
            base.OnListChanged(item, property, type);
        }

        public override void Dispose()
        {
            BaseTable.RemoveVirtual(this);
            if (filterQuery != null)
                filterQuery.Dispose();
            base.Dispose();
        }

        public override string SqlName
        {
            get { return BaseTableName; }
        }

        public override DBColumn CheckColumn(string name, Type t, ref bool newCol)
        {
            return BaseTable.CheckColumn(name, t, ref newCol);
        }

        public override bool SaveItem(DBItem row, DBTransaction transaction)
        {
            row = row is DBVirtualItem ? ((DBVirtualItem)row).Main : row;
            return BaseTable.SaveItem(row, transaction);
        }

        public override void Clear()
        {
            BaseTable.Clear();
            base.Clear();
        }

        public T New(DBItem main)
        {
            T row = new T();
            row.Main = main;
            row.Build(this, main.DBState, false);
            return row;
        }

        public DBColumn GetVColumn(int index)
        {
            DBTable table = BaseTable;
            DBColumn column = index >= 0 ? table.Columns[index] : null;
            if (column != null)
                column = columns.GetByBase(column.Name);
            return column;
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            ddl.AppendLine("create view " + Name + " as");
            ddl.Append("select ");
            foreach (DBVirtualColumn col in columns)
            {
                if (col.ColumnType == DBColumnTypes.Default)
                    ddl.Append(col.BaseName + " as " + col.Name);
                else if (col.ColumnType == DBColumnTypes.Query)
                    ddl.Append(col.Query + " as " + col.Name);
                else
                    continue;
                ddl.Append(", ");
            }
            ddl.Remove(ddl.Length - 2, 2);
            ddl.AppendLine();
            ddl.AppendLine("from {SqlName} where {Query};");

            return ddl.ToString();
        }
    }
}