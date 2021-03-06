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
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

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
        Stop = 1,
        Start = 2,
        System = 4,
        Return = 8,
        AutoComplete = 16
    }

    [DataContract, Table("rstage", "Template", BlockSize = 100)]
    public class Stage : DBItem, IDisposable
    {
        private static DBTable<Stage> dbTable;
        private static DBColumn exportCodeKey = DBColumn.EmptyKey;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn workKey = DBColumn.EmptyKey;
        private static DBColumn keysKey = DBColumn.EmptyKey;
        private static DBColumn timeLimitKey = DBColumn.EmptyKey;

        public static DBTable<Stage> DBTable => dbTable ?? (dbTable = GetTable<Stage>());
        public static DBColumn ExportCodeKey => DBTable.ParseProperty(nameof(ExportCode), ref exportCodeKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn WorkKey => DBTable.ParseProperty(nameof(WorkId), ref workKey);
        public static DBColumn KeysKey => DBTable.ParseProperty(nameof(Keys), ref keysKey);
        public static DBColumn TimeLimitKey => DBTable.ParseProperty(nameof(TimeLimit), ref timeLimitKey);

        private Work work;

        public Stage()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(table.PrimaryKey); }
            set { SetValue(value, table.PrimaryKey); }
        }

        [DataMember, Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(table.CodeKey); }
            set { SetValue(value, table.CodeKey); }
        }

        [DataMember, Column("export_code", 512)]
        public string ExportCode
        {
            get { return GetValue<string>(ExportCodeKey); }
            set { SetValue(value, ExportCodeKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [Browsable(false)]
        [DataMember, Column("work_id")]
        public int? WorkId
        {
            get { return GetValue<int?>(WorkKey); }
            set { SetValue(value, WorkKey); }
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get { return GetReference(WorkKey, ref work); }
            set { work = SetReference(value, WorkKey); }
        }

        [DataMember, Column("keys")]
        public StageKey? Keys
        {
            get { return GetValue<StageKey?>(KeysKey); }
            set { SetValue(value, KeysKey); }
        }

        [DataMember, Column("time_limit")]
        public TimeSpan? TimeLimit
        {
            get { return GetValue<TimeSpan?>(TimeLimitKey); }
            set { SetValue(value, TimeLimitKey); }
        }

        public IEnumerable<T> GetParams<T>() where T : StageParam
        {
            return GetReferencing<StageParam>(nameof(StageParam.StageId), DBLoadParam.None).OfType<T>();
        }

        [ControllerMethod]
        public IEnumerable<StageParam> GetParams()
        {
            return GetReferencing<StageParam>(nameof(StageParam.StageId), DBLoadParam.None);
        }

        [ControllerMethod]
        public IEnumerable<StageReference> GetReferences()
        {
            return GetParams<StageReference>();
        }

        [ControllerMethod]
        public StageReference GetNextReference()
        {
            return GetParams<StageReference>().FirstOrDefault(p => p.Next ?? false);
        }

        [ControllerMethod]
        public IEnumerable<StageProcedure> GetProcedures()
        {
            return GetParams<StageProcedure>();
        }

        [ControllerMethod]
        public IEnumerable<StageProcedure> GetProceduresByType(StageParamProcudureType type)
        {
            return GetParams<StageProcedure>().Where(p => p.ProcedureType == type);
        }


    }
}
