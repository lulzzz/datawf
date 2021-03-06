﻿/*
 DBRowEventArgs.cs
 
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

using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public enum DBItemMethod
    {
        Accept,
        Attach,
        Change,
        Update
    }

    public class DBItemEventArgs : CancelEventArgs
    {
        public DBItemEventArgs(DBItem item, DBColumn column = null, string property = null, object value = null)
        {
            Item = item;
            State = item.UpdateState;
            Column = column;
            Value = value;
            Property = property ?? string.Empty;
        }

        public DBItemEventArgs(DBItem item, DBTransaction transaction)
            : this(item, transaction, transaction.Caller)
        { }

        public DBItemEventArgs(DBItem item, DBTransaction transaction, IUserIdentity user)
        {
            Item = item;
            State = item.UpdateState;
            Transaction = transaction;
            User = user;
        }

        public DBUpdateState State { get; set; }

        public DBColumn Column { get; }

        public string Property { get; }

        public object Value { get; set; }

        public DBItem Item { get; }

        public DBLogItem LogItem { get; set; }

        public List<DBColumn> Columns { get; set; }

        public DBTransaction Transaction { get; }

        public IUserIdentity User { get; }

        public bool StateAdded(DBUpdateState filter)
        {
            return (State & filter) != filter && (Item.UpdateState & filter) == filter;
        }

        public bool StateRemoved(DBUpdateState filter)
        {
            return (State & filter) == filter && (Item.UpdateState & filter) != filter; ;
        }
    }
}
