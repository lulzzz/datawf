﻿/*
 DBColumnGroupList.cs
 
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

namespace DataWF.Data
{
    public class DBColumnGroupList : DBTableItemList<DBColumnGroup>
    {
        public DBColumnGroupList(DBTable table) : base(table)
        {
        }

        public override int AddInternal(DBColumnGroup item)
        {
            item.Order = this.Count;
            if (Contains(item.Name))
                throw new InvalidOperationException($"Column group Name duplicaation {item.Name}!");
            return base.AddInternal(item);
        }
    }
}
