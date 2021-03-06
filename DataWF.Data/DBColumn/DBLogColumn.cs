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
using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBLogColumn : DBColumn
    {
        public static string GetName(DBColumn column)
        {
            return column.Name + "_log";
        }

        private DBColumn baseColumn;

        public DBLogColumn()
        { }

        public DBLogColumn(DBColumn column)
        {
            BaseColumn = column;
        }

        public DBLogTable LogTable { get { return (DBLogTable)Table; } }

        [Browsable(false)]
        public string BaseName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn BaseColumn
        {
            get { return baseColumn ?? (baseColumn = LogTable?.BaseTable?.ParseColumn(BaseName)); }
            set
            {
                if (value == null)
                {
                    throw new Exception("BaseColumn value is empty!");
                }

                baseColumn = value;
                BaseName = value.Name;
                Name = GetName(value);
                DisplayName = value.DisplayName + " Log";
                DBDataType = value.DBDataType;
                DataType = value.DataType;
                ReferenceTable = value.ReferenceTable;
                Size = value.Size;
                Scale = value.Scale;
                if (value.IsFile)
                {
                    Keys |= DBColumnKeys.File;
                }
                if (value.IsFileName)
                {
                    Keys |= DBColumnKeys.FileName;
                }
                if (value.IsFileLOB)
                {
                    Keys |= DBColumnKeys.FileLOB;
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public override AccessValue Access
        {
            get { return BaseColumn?.Access; }
            set { base.Access = value; }
        }
    }
}
