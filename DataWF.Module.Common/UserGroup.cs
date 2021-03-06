﻿/*
 Group.cs
 
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
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("rgroup", "User", BlockSize = 10)]
    public class UserGroup : DBItem, IDisposable, IAccessGroup
    {
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn companyKey = DBColumn.EmptyKey;
        private static DBTable<UserGroup> dbTable;
        private Company company;

        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn CompanyKey => DBTable.ParseProperty(nameof(Company), ref companyKey);
        public static DBTable<UserGroup> DBTable => dbTable ?? (dbTable = GetTable<UserGroup>());

        internal static void SetCurrent()
        {
            AccessValue.Groups = UserGroup.DBTable;
        }

        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        public UserGroup()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        int IAccessGroup.Id { get { return Id ?? -1; } }

        [DataMember, Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get { return GetValue<int?>(CompanyKey); }
            set { SetValue(value, CompanyKey); }
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get { return GetReference(CompanyKey, ref company); }
            set { company = SetReference(value, CompanyKey); }
        }

        [DataMember, Column("group_number", 512, Keys = DBColumnKeys.Code), Index("rgroup_group_number")]
        public string Number
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameEN
        {
            get { return GetValue<string>(NameENKey); }
            set { SetValue(value, NameENKey); }
        }

        public string NameRU
        {
            get { return GetValue<string>(NameRUKey); }
            set { SetValue(value, NameRUKey); }
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers(DBTransaction transaction)
        {
            foreach (User user in User.DBTable)
            {
                if (user.Access.Get(this).Create && user.Access.GetFlag(AccessType.Read, transaction.Caller))
                {
                    yield return user;
                }
            }
        }

        public bool IsCurrentUser(IUserIdentity user)
        {
            return user is User currentUser && ((currentUser?.Super.Value ?? false)//currentUser.Status == DBStatus.Actual ||
                    || (currentUser?.Access.Get(this).Create ?? false));
        }

        public void AddUser(User user)
        {
            user.Access.SetFlag(this, AccessType.Create);
        }

        public override void Dispose()
        {
            base.Dispose();
        }


    }
}
