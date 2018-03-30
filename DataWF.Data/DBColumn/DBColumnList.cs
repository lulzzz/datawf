﻿/*
 DBColumnList.cs
 
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
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBColumnList : DBTableItemList<DBColumn>
    {
        public DBColumnList(DBTable table)
            : base(table)
        {
            Indexes.Add(new Invoker<DBColumn, string>(nameof(DBColumn.GroupName), item => item.GroupName));
            Indexes.Add(new Invoker<DBColumn, bool>(nameof(DBColumn.IsView), item => item.IsView));
            Indexes.Add(new Invoker<DBColumn, bool>(nameof(DBColumn.IsReference), item => item.IsReference));
            Indexes.Add(new Invoker<DBColumn, DBTable>(nameof(DBColumn.ReferenceTable), item => item.ReferenceTable));
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            if (Table != null && Table.Schema != null)
            {
                Table.ClearCache();
                if (newIndex >= 0)
                {
                    DBColumn column = this[newIndex];
                    if (type == ListChangedType.ItemChanged && property == nameof(DBColumn.Keys))
                    {
                        if (column.Index == null && (column.IsPrimaryKey || (column.Keys & DBColumnKeys.Indexing) == DBColumnKeys.Indexing))
                        {
                            column.Index = DBPullIndex.Fabric(column.DataType, Table, column);
                        }
                        else if (column.Index != null)
                        {
                            column.Index.Dispose();
                            column.Index = null;
                        }
                    }
                    else if (type == ListChangedType.ItemDeleted && column.Index != null)
                    {
                        column.Clear();
                        column.Index.Dispose();
                        column.Index = null;
                    }
                }
            }
        }

        public override DBColumn this[string name]
        {
            get
            {
                if (name == null)
                    return null;
                return base[name];
            }
            set
            {
                int i = GetIndexByName(name);
                this[i] = value;
            }
        }

        public new void Clear()
        {
            base.Clear();
        }

        public override int AddInternal(DBColumn item)
        {
            if (Contains(item))
            {
                return -1;
            }
            if (Contains(item.Name))
            {
                throw new InvalidOperationException($"Columns name duplication {item.Name}");
            }
            // if (col.Order == -1 || col.Order > this.Count)
            item.Order = this.Count;

            var index = base.AddInternal(item);

            if (item.Index == null && (item.IsPrimaryKey || (item.Keys & DBColumnKeys.Indexing) == DBColumnKeys.Indexing || (item.Keys & DBColumnKeys.Reference) == DBColumnKeys.Reference))
            {
                item.Index = DBPullIndex.Fabric(item.DataType, Table, item);
            }
            if (item.IsPrimaryKey)
            {
                DBConstraint primary = null;
                foreach (var constraint in Table.Constraints.GetByColumn(Table.PrimaryKey))
                {
                    if (constraint.Type == DBConstaintType.Primary)
                    {
                        primary = constraint;
                        break;
                    }
                }
                if (primary == null)
                {
                    primary = new DBConstraint() { Column = Table.PrimaryKey, Type = DBConstaintType.Primary };
                    primary.GenerateName();
                    Table.Constraints.Add(primary);
                }
            }
            return index;
        }

        public DBColumn Add(string name)
        {
            return Add(name, typeof(string), 0);
        }

        public DBColumn Add(string name, Type t)
        {
            return Add(name, t, 0);
        }

        public DBColumn Add(string name, DBTable reference)
        {
            DBColumn column = Add(name, reference.PrimaryKey.DataType, reference.PrimaryKey.Size);
            column.ReferenceTable = reference;
            return column;
        }

        public DBColumn Add(string name, Type t, int size)
        {
            if (Contains(name))
                return this[name];
            var column = new DBColumn(name, t, size) { Table = Table };
            Add(column);
            return column;
        }

        public IEnumerable<DBColumn> GetByGroup(DBColumnGroup group)
        {
            return GetByGroup(group.Name);
        }

        public IEnumerable<DBColumn> GetByGroup(string groupName)
        {
            return string.IsNullOrEmpty(groupName) ? null : Select(nameof(DBColumn.GroupName), CompareType.Equal, groupName);
        }

        public DBColumn GetByBase(string code)
        {
            return SelectOne(nameof(DBVirtualColumn.BaseName), CompareType.Equal, code);
        }

        public IEnumerable<DBColumn> GetByReference(DBTable table)
        {
            return Select(nameof(DBColumn.ReferenceTable), CompareType.Equal, table);
        }

        public IEnumerable<DBColumn> GetIsReference()
        {
            return Select(nameof(DBColumn.IsReference), CompareType.Equal, true);
        }

        public IEnumerable<DBColumn> GetIsView()
        {
            return Select(nameof(DBColumn.IsView), CompareType.Equal, true);
        }

        public DBColumn GetByKey(DBColumnKeys key)
        {
            foreach (DBColumn column in this)
            {
                if ((column.Keys & key) == key)
                {
                    return column;
                }
            }
            return null;
        }
    }
}
