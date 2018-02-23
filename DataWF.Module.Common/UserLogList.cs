﻿/*
 DocumentLog.cs

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
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class UserLogList : DBTableView<UserLog>
    {
        public UserLogList(string filter)
            : base(UserLog.DBTable, filter)
        {
        }

        public UserLogList()
            : this("")
        {
        }

        public UserLogList(DBItem document)
            : this(UserLog.DBTable.ParseProperty(nameof(UserLog.DocumentId)).Name + "='" + document.PrimaryId + "'")
        {
        }

        public void FilterByDocument(DBItem document)
        {
            Filter = UserLog.DBTable.ParseProperty(nameof(UserLog.DocumentId)).Name + "='" + document.PrimaryId + "'";
        }
    }
}