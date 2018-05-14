﻿/*
 Stage.cs
 
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
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;
using System.Runtime.Serialization;
using System.Collections;

namespace DataWF.Module.Flow
{
    public class StageList : DBTableView<Stage>
    {
        public StageList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer(Stage.DBTable.CodeKey, ListSortDirection.Ascending));
        }

        public StageList()
            : this("")
        {
        }

        public StageList(Work flow)
            : this(Stage.DBTable.ParseColumn(nameof(Stage.Work)).Name + "=" + flow.PrimaryId)
        {
        }
    }

    [Flags]
    public enum StageKey
    {
        None = 0,
        IsStop = 1,
        IsStart = 2,
        IsSystem = 4,
        IsReturn = 8,
        IsAutoComplete = 16
    }

    [DataContract, Table("rstage", "Template", BlockSize = 100)]
    public class Stage : DBItem, IDisposable
    {
        public static DBTable<Stage> DBTable
        {
            get { return DBService.GetTable<Stage>(); }
        }

        public Stage()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        [Browsable(false)]
        [DataMember, Column("work_id")]
        public int? WorkId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get { return GetPropertyReference<Work>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("keys")]
        public StageKey? Keys
        {
            get { return GetProperty<StageKey?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("time_limit")]
        public TimeSpan? TimeLimit
        {
            get { return GetProperty<TimeSpan?>(); }
            set { SetProperty(value); }
        }
        public IEnumerable<StageProcedure> GetProceduresByType(ParamProcudureType type)
        {
            return GetParams().Where(p => p is StageProcedure && ((StageProcedure)p).ProcedureType == type).Cast<StageProcedure>();
        }

        public IEnumerable<StageParam> GetParams()
        {
            var list = GetReferencing<StageParam>(nameof(StageParam.StageId), DBLoadParam.None).ToList();
            list.Sort(new DBComparer(StageParam.DBTable.PrimaryKey, ListSortDirection.Ascending));
            return list;
        }

        public IEnumerable<User> GetUsers()
        {
            foreach (var access in Access.Items)
            {
                if (access.Create)
                {
                    foreach (User user in User.DBTable)
                    {
                        if (user.Access.Get(access.Group).Create)
                            yield return user;
                    }
                }
            }
        }

        public IEnumerable<Position> GetPositions()
        {
            foreach (var access in Access.Items)
            {
                if (access.Create)
                {
                    foreach (Position position in Position.DBTable)
                    {
                        if (position.Access.Get(access.Group).Create)
                            yield return position;
                    }
                }
            }
        }

        public IEnumerable<Department> GetDepartment()
        {
            foreach (var access in Access.Items)
            {
                if (access.Create)
                {
                    foreach (Department department in Department.DBTable)
                    {
                        if (department.Access.Get(access.Group).Create)
                            yield return department;
                    }
                }
            }
        }
    }
}
