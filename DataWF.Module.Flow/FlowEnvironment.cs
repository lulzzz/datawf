﻿/*
 FlowEnvir.cs
 
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
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Data;
using System.Diagnostics;

namespace DataWF.Module.Flow
{

    public sealed class FlowEnvironment : IDisposable
    {
        private static FlowEnvironment instance = new FlowEnvironment();
        private string schemaCode = "new";

        public DBSchemaList Schems
        {
            get { return DBService.Schems; }
        }

        public bool LogUpdate { get; set; } = true;

        public bool LogExecute { get; set; } = true;

        public bool LogProcedure { get; set; }

        public static void OnDBServiceExecute(DBExecuteEventArg arg)
        {
            if (Config.LogExecute || arg.Rezult is Exception || arg.Type == DBExecuteType.CreateConnection)
            {
                string message = string.Format("in {0} ms ({1})", arg.Time.TotalMilliseconds, arg.Rezult is IDataReader
                                               ? ((IDataReader)arg.Rezult).RecordsAffected + "*" + ((IDataReader)arg.Rezult).FieldCount : (arg.Rezult is Exception
                                               ? ((Exception)arg.Rezult).Message : arg.Rezult));
                Helper.Logs.Add(new StateInfo("Execute " + arg.Type, message, arg.Query, arg.Rezult is Exception ? StatusType.Warning : StatusType.Information));
            }
            if (arg.Rezult is Exception)
                Helper.OnException((Exception)arg.Rezult);
        }

        public static void OnDBRowChanged(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserLog.DBTable) //|| arg.Row.Table == FlowEnvir.Config.Document.Table)
                return;

            if (!(arg.Item.Table is IDBVirtualTable))
            {
                var cols = arg.Item.Table.Columns.GetByReference(Document.DBTable);

                foreach (DBColumn col in cols)
                {
                    var document = arg.Item.GetReference<Document>(col, DBLoadParam.None);
                    if (document != null)
                        document.OnReferenceChanged(arg.Item);
                }
            }
        }

        public static void LoadBooks()
        {
            Helper.Logs.Add(new StateInfo("Flow Synchronization", "Start", "", StatusType.Information));
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var transaction = new DBTransaction(Book.DBTable.Schema.Connection) { ReaderParam = DBLoadParam.Synchronize | DBLoadParam.CheckDeleted })
            {
                Book.DBTable.Load();
                //cache groups
                UserGroup.DBTable.Load();
                AccessValue.Groups = new DBTableView<UserGroup>("");
                Location.DBTable.Load();
                User.DBTable.Load();
                Template.DBTable.Load();
                TemplateData.DBTable.Load();
                Work.DBTable.Load();
                Stage.DBTable.Load();
                StageParam.DBTable.Load();
                GroupPermission.DBTable.Load();
                Scheduler.DBTable.Load();
            }
            watch.Stop();

            Helper.Logs.Add(new StateInfo("Flow Synchronization", "Complete", "in " + watch.ElapsedMilliseconds + " ms", StatusType.Information));

            DocumentWork.DBTable.DefaultComparer = new DBComparer(DocumentWork.DBTable.PrimaryKey) { Hash = true };
            //Logs.Add(new StateInfo("Flow Check", "Config Falil", "AccountInfo", StatusType.Warning));
        }



        public static void LoadConfig()
        {
            Helper.LogWorkingSet("DataBase Info");
            LoadEnvir();
            Helper.LogWorkingSet("Flow Config");
            LoadBooks();
            Helper.LogWorkingSet("Books");
            //FlowEnvironment.LoadDocuments();
            //Helper.LogWorkingSet("Documents");
            //FlowEnvironment.Compiler();
        }

        public static void SaveConfig()
        {
            FlowEnvironment.SaveEnvir();
        }

        public static void SaveEnvir()
        {
            instance.Save();
        }

        public void Save()
        {
            Save("flow.xml");
        }

        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public static void LoadEnvir()
        {
            instance.Load();
        }

        public void Load()
        {
            Load("flow.xml");
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public static FlowEnvironment Config
        {
            get { return instance; }
            set { instance = value; }
        }

        public void Initialize()
        {
            //DBService.Execute += FlowEnvironment.OnDBServiceExecute;

            FlowEnvironment.Config.LogUpdate = true;

            //DBService.RowStateEdited += FlowEnvironment.OnDBRowChanged;
            //DBService.RowAdded += FlowEnvir.OnDBRowChanged;
            //DBService.RowRemoved += FlowEnvir.OnDBRowChanged;
        }

        public void Dispose()
        {
            //DBService.Execute -= FlowEnvironment.OnDBServiceExecute;

            //DBService.RowStateEdited -= FlowEnvironment.OnDBRowChanged;
            //DBService.RowAdded -= FlowEnvir.OnDBRowChanged;
            //DBService.RowRemoved -= FlowEnvir.OnDBRowChanged;
        }

        public string SchemaCode
        {
            get { return schemaCode; }
            set { schemaCode = value; }
        }

        //public DBSchema Schema
        //{
        //    get { return DBService.Schems[schemaCode]; }
        //    set { schemaCode = value == null ? null : value.Name; }
        //}

        public static void CheckScheduler()
        {
            //throw new NotImplementedException();
        }

        //public static void OnGroupName(object sender, AccessItemEventArg e)
        //{
        //    Group group = Config.Group.View.GetById(e.Item.GroupId);
        //    e.String = group == null ? "empty" : group.Name;
        //}
    }
}
