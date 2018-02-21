﻿/*
 DBSchema.cs
 
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBSchema : DBSchemaItem, IFileSerialize
    {
        protected string connectionName = "";
        protected string dataBase = "";
        protected string fileName = "";
        protected DBConnection connection;

        public DBSchema()
            : this(null)
        { }

        public DBSchema(string name)
            : base(name)
        {
            DataBase = name;
            Sequences = new DBSequenceList(this);
            Tables = new DBTableList(this);
            TableGroups = new DBTableGroupList(this);
            Procedures = new DBProcedureList(this);
        }

        public DBSchema(string name, string fileName)
            : this(name)
        {
            //Init();
            FileName = fileName;
            Serialization.Deserialize(fileName, this);
        }

        public void InitTables()
        {
            foreach (DBTable table in Tables)
            {
                foreach (DBColumn column in table.Columns)
                    if (column.Index == null && (column.IsPrimaryKey || (column.Keys & DBColumnKeys.Indexing) == DBColumnKeys.Indexing))
                        column.Index = DBPullIndex.Fabric(column.DataType, table, column);
            }
        }

        public DBTable InitTable(string name)
        {
            DBTable table = null;
            table = Tables[name];
            if (table == null)
            {
                table = new DBTable<DBItem>(name);
                Tables.Add(table);
            }
            return table;
        }

        public void GenerateTables(List<DBTableInfo> tables)
        {
            foreach (var tableInfo in tables)
            {
                var table = InitTable(tableInfo.Name);
                table.Type = tableInfo.View ? DBTableType.View : DBTableType.Table;
                table.GenerateColumns(tableInfo);
            }
        }

        #region IFileSerialize Members

        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public void Save()
        {
            Save(FileName);
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public void Load()
        {
            Load(FileName);
        }

        [Browsable(false)]
        public string FileName
        {
            get
            {
                if (name == null || name.Length == 0)
                    name = "schema";
                if (fileName == "")
                    fileName = "schems" + Path.DirectorySeparatorChar + name + ".xml";
                return fileName;
            }
            set { fileName = value; }
        }
        #endregion

        [XmlIgnore]
        public DBConnection Connection
        {
            get { return connection ?? (connection = DBService.Connections[connectionName]); }
            set
            {
                connection = value;
                connectionName = connection?.Name;
                if (value != null && !DBService.Connections.Contains((value)))
                {
                    DBService.Connections.Add(value);
                }
            }
        }

        public DBSystem System
        {
            get { return Connection?.System; }
        }

        public DBTableList Tables { get; private set; }

        public DBTableGroupList TableGroups { get; private set; }

        public DBProcedureList Procedures { get; private set; }

        public DBSequenceList Sequences { get; private set; }

        public override string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
                if (DataBase == null)
                    DataBase = value;
            }
        }

        public string DataBase
        {
            get { return dataBase; }
            set
            {
                if (dataBase != value)
                {
                    dataBase = value;
                    OnPropertyChanged(nameof(DataBase), false);
                }
            }
        }

        public string FormatSql()
        {
            var ddl = new StringBuilder();
            System?.Format(ddl, this);
            return ddl.ToString();
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            System?.Format(ddl, this, ddlType);
            return ddl.ToString();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public List<DBTableInfo> GetTablesInfo(string schemaName = null, string tableName = null)
        {
            return System.GetTablesInfo(Connection, schemaName, tableName);
        }

        public void CreateDatabase()
        {
            try { DBService.ExecuteQuery(Connection, FormatSql(DDLType.Drop), true, DBExecuteType.NoReader); }
            catch (Exception ex) { Debug.WriteLine(ex); }

            DBService.ExecuteGoQuery(Connection, FormatSql(DDLType.Create), true);

            if (Connection.Schema?.Length > 0)
            {
                if (Connection.System == DBSystem.Oracle)
                {
                    Connection.User = Name;
                }
                else if (Connection.System != DBSystem.SQLite)
                {
                    Connection.DataBase = Name;
                }
            }

            DBService.ExecuteGoQuery(Connection, FormatSql(), true);
        }

        internal IEnumerable<DBConstraint> GetConstraints()
        {
            foreach (var table in Tables)
            {
                foreach (var constraint in table.Constraints)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBForeignKey> GetForeigns()
        {
            foreach (var table in Tables)
            {
                foreach (var constraint in table.Foreigns)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBIndex> GetIndexes()
        {
            foreach (var table in Tables)
            {
                foreach (var index in table.Indexes)
                    yield return index;
            }
        }
    }
}
