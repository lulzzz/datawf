﻿/*
 QColumn.cs
 
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
using System.Data;
using System.Linq;

namespace DataWF.Data
{
    public class QColumn : QItem
    {
        protected DBColumn columnn;
        private object temp;
        protected string columnName;
        private string prefix;

        public QColumn()
        {
        }

        public QColumn(string name)
            : base(name)
        {
            columnName = name;
        }

        public QColumn(DBColumn column)
            : base(column.Name)
        {
            Column = column;
        }

        public string ColumnName
        {
            get { return columnName; }
            set
            {
                if (columnName != value)
                {
                    columnName = value;
                    columnn = null;
                }
                OnPropertyChanged(nameof(ColumnName));
            }
        }

        public virtual DBColumn Column
        {
            get
            {
                if (columnn == null && columnName != null)
                    columnn = base.Table?.ParseColumn(columnName) ?? DBService.Schems.ParseColumn(columnName);
                return columnn;
            }
            set
            {
                if (Column != value)
                {
                    ColumnName = value?.FullName;
                    Text = value?.Name;
                    //prefix = value.Table.Code;
                    columnn = value;
                }
            }
        }

        public override DBTable Table
        {
            get { return Column?.Table ?? base.Table; }
            set { }
        }

        public QTable QTable
        {
            get { return Query.Tables.FirstOrDefault(p => p.Table == Table); }
        }

        public override void Dispose()
        {
            columnn = null;
            base.Dispose();
        }

        public string FullName
        {
            get { return $"{Table}.{Column}"; }
        }

        public override string Format(IDbCommand command = null)
        {
            if (Column == null)
                return text;
            else if (command != null
                && (Column.ColumnType == DBColumnTypes.Internal
                || Column.ColumnType == DBColumnTypes.Expression
                || Column.ColumnType == DBColumnTypes.Code))
                return string.Empty;
            else if (Column.ColumnType == DBColumnTypes.Query && Column.Table.Type != DBTableType.View)
                return $"({Column.Query}) as {text}";
            else
                return $"{(Prefix != null ? (Prefix + ".") : "")}{text}{(alias != null ? (" as " + alias) : "")}";
        }

        public override object GetValue(DBItem row)
        {
            return temp ?? Column.GetValue(row);
        }

        public string Prefix
        {
            get { return prefix ?? QTable?.Alias; }
            set
            {
                if (prefix != value)
                {
                    prefix = value;
                    OnPropertyChanged(nameof(Prefix));
                }
            }
        }
        public object Temp { get { return temp; } set { temp = value; } }

        public override string ToString()
        {
            return Column == null ? base.ToString() : Column.ToString();
        }
    }
}