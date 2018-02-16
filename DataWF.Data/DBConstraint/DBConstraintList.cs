﻿using DataWF.Common;
/*
 DBConstraintList.cs
 
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
using System.Collections.Generic;

namespace DataWF.Data
{
    public class DBConstraintList<T> : DBSchemaItemList<T> where T : DBConstraint, new()
    {
        public DBConstraintList(DBSchema schema)
            : base(schema)
        {
            Indexes.Add(new Invoker<DBConstraint, string>(nameof(DBConstraint.TableName), (item) => item.TableName));
            Indexes.Add(new Invoker<DBConstraint, string>(nameof(DBConstraint.ColumnName), (item) => item.ColumnName));
        }

        public IEnumerable<T> GetByTable(DBTable table)
        {
            return Select(nameof(DBConstraint.TableName), CompareType.Equal, table.FullName);
        }

        public IEnumerable<T> GetByColumn(DBColumn column)
        {
            return Select(nameof(DBConstraint.ColumnName), CompareType.Equal, column.FullName);
        }

        public IEnumerable<T> GetByValue(string value)
        {
            return Select(nameof(DBConstraint.Value), CompareType.Equal, value);
        }
    }
}