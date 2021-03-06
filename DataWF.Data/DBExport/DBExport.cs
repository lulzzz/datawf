﻿/*
 DBExport.cs
 
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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    [Flags]
    public enum ExportMode
    {
        Default = 0,
        Patch = 1,
        DropTable = 2,
        ClearData = 4,
        ExcludeAccess = 8,
        StringTrimm = 16,
        DateValidation = 32
    }

    public class DBExport : INotifyPropertyChanged
    {
        public static DBExport PatchRead(string fileName)
        {
            string dbfile = null;
            DBExport export = null;
            var path = Path.Combine(Helper.GetDirectory(), Path.GetFileNameWithoutExtension(fileName));
            if (Helper.ReadZip(fileName, path))
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    if (file.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                        export = Serialization.Deserialize(file) as DBExport;
                    if (file.EndsWith("sdb", StringComparison.OrdinalIgnoreCase))
                        dbfile = file;
                }
                export.Source.Connection.Host = dbfile;
            }
            return export;
        }

        public string GenetareCode()
        {
            List<DBTable> list = tables.Select(p => p.SourceTable).ToList();
            var builder = new StringBuilder("");
            builder.AppendLine("using DataWF.Common;");
            builder.AppendLine("using DataWF.Data;");
            builder.AppendLine("using DataWF.Module.Flow;");
            builder.AppendLine("using System;");
            builder.AppendLine("namespace CodeGenerator {");
            foreach (var table in list)
            {
                builder.AppendLine($"    public class {table.DisplayName.Replace(" ", "")}Generator {{");
                builder.AppendLine($"        public void Generate() {{");
                builder.AppendLine($"            using(var transaction = new DBTransaction({table.ItemType.Type.Name}.DBTable.Schema.Connection)){{");
                var enumer = table.SelectItems("");
                if (table.GroupKey != null)
                {
                    var groupList = enumer.Cast<DBGroupItem>().ToList();
                    groupList.Sort(GroupHelper.Compare);
                    enumer = groupList;
                }

                foreach (DBItem row in enumer)
                {
                    var type = row.GetType();
                    builder.AppendLine($"                new {type.Name}{{");
                    foreach (var column in table.Columns)
                    {
                        if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                            continue;
                        if (column.Property != null)
                        {
                            builder.AppendLine($"                    {column.Property} = {column.FormatCode(row.GetValue(column))},");
                        }
                    }
                    builder.AppendLine("                }.Save();");
                }
                builder.AppendLine($"                {table.ItemType.Type.Name}.DBTable.Save();");
                builder.AppendLine("                transaction.Commit();");
                builder.AppendLine("            }");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        public string GeneratePatch()
        {
            return GeneratePatch(tables.Select(p => p.SourceTable).ToList());
        }

        public string GeneratePatch(List<DBTable> list)
        {
            list.Sort(new Comparison<DBTable>(DBService.CompareDBTable));
            var builder = new StringBuilder();

            if ((Mode & ExportMode.DropTable) == ExportMode.DropTable)
            {
                builder.AppendLine("-- -=================== Drop Tables =====================");
                tables.Reverse();
                foreach (DBTable table in list)
                {
                    builder.AppendLine("if exists (select * from information_schema.tables where table_name = '" + table.Name + "')");
                    builder.AppendLine("\t" + table.FormatSql(DDLType.Drop));
                }
                builder.AppendLine("go");
                tables.Reverse();
            }
            if ((Mode & ExportMode.ClearData) == ExportMode.ClearData)
            {
                builder.AppendLine("-- -=================== Clear Tables ======================");
                tables.Reverse();
                foreach (DBTable table in list)
                {
                    builder.AppendLine("if exists (select * from information_schema.tables where table_name = '" + table.Name + "')");
                    builder.AppendLine("\tdelete from " + table.Name);
                }
                builder.AppendLine("go");
                tables.Reverse();
            }

            foreach (DBTable table in list)
            {
                builder.AppendLine();
                builder.AppendLine("-- -===================== " + table.Name + " ======================");
                if ((Mode & ExportMode.DropTable) == ExportMode.DropTable)
                {
                    builder.AppendLine(table.FormatSql(DDLType.Create));
                    builder.AppendLine("go");
                }
                if (table.Type == DBTableType.Table)//table.IsCaching && 
                {
                    if ((Mode & ExportMode.Patch) == ExportMode.Patch)
                    {
                        foreach (DBItem row in table.LoadItems())
                        {
                            if (row.Stamp != null && row.Stamp >= Stamp)//((DateTime)row.Stamp >= param.PatchDate)
                            {
                                builder.AppendLine(string.Format("if exists(select * from {0} where {1}={2})",
                                                            table.Name,
                                                            table.PrimaryKey.Name,
                                                            row.PrimaryId));
                                builder.AppendLine("    " + table.Schema.System.FormatCommand(table, DBCommandTypes.Update, row));
                                builder.AppendLine("else");
                                builder.AppendLine("    " + table.Schema.System.FormatCommand(table, DBCommandTypes.Insert, row));
                            }
                        }
                        builder.AppendLine("go");
                    }
                    else
                    {
                        builder.AppendLine(table.Schema.System.FormatInsert(table, true));
                        if (table.Count > 0)
                        {
                            builder.AppendLine("go");
                        }
                    }
                }
            }
            return builder.ToString();
        }

        protected string prefix = "";
        protected string source;
        protected string target;
        protected DBSchema dbsource;
        protected DBSchema dbtarget;
        protected DBETableList tables;
        protected ExportMode mode = ExportMode.Default;
        protected DateTime stamp;

        [NonSerialized]
        public static DBExportEnvironment Environment = new DBExportEnvironment();

        protected int bufferSize = 1000;

        public DBExport()
            : this(null)
        {
        }

        public event EventHandler<ExportProgressArgs> ExportProgress;

        public DBExport(string name)
        {
            this.tables = new DBETableList(this);
        }

        public override string ToString()
        {
            return string.Format("Source: {0}; Target: {1}", SourceName, TargetName);
        }

        public string Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        public ExportMode Mode
        {
            get { return mode; }
            set
            {
                if (mode != value)
                {
                    mode = value;
                    OnPropertyChange(nameof(Mode));
                }
            }
        }

        public DateTime Stamp
        {
            get { return stamp; }
            set
            {
                if (stamp != value)
                {
                    stamp = value;
                    PatchQuery();
                    OnPropertyChange(nameof(Stamp));
                }
            }
        }

        [Browsable(false)]
        public string SourceName
        {
            get { return this.source; }
            set
            {
                if (source != value)
                {
                    source = value;
                    OnPropertyChange(nameof(SourceName));
                }
            }
        }

        public DBSchema Source
        {
            get { return dbsource ?? DBService.Schems[source]; }
            set
            {
                if (Source != value)
                {
                    SourceName = value?.Name;
                    if (value == null || DBService.Schems.Contains(value))
                        dbsource = null;
                    else
                        dbsource = value;
                }
            }
        }

        [Browsable(false)]
        public string TargetName
        {
            get { return target; }
            set
            {
                if (target != value)
                {
                    target = value;
                    OnPropertyChange(nameof(TargetName));
                }
            }
        }

        public DBSchema Target
        {
            get { return dbtarget ?? DBService.Schems[target]; }
            set
            {
                if (Target != value)
                {
                    TargetName = value?.Name;
                    if (value == null || DBService.Schems.Contains(value))
                        dbtarget = null;
                    else
                        dbtarget = value;
                }
            }
        }

        [Browsable(false)]
        public bool IsInitialize
        {
            get { return (tables.Count != 0); }
        }

        [Browsable(false)]
        public DBETableList Tables
        {
            get { return tables; }
        }

        public void PatchQuery()
        {
            if ((mode & ExportMode.Patch) == ExportMode.Patch)
            {
                foreach (var table in tables)
                {
                    if (table.SourceTable != null && table.SourceTable.StampKey != null)
                    {
                        if (table.SourceTable.Schema.Connection.System == DBSystem.MSSql)
                            table.Query = string.Format("where {0} >= parse('{1:yyyy-MM-dd}' as datetime using 'ru-RU')", table.SourceTable.StampKey.Name, stamp);
                        else
                            table.Query = string.Format("where {0} >= '{1:yyyy-MM-dd}'", table.SourceTable.StampKey.Name, stamp);

                    }
                }
            }
        }

        protected void OnExportProgress(ExportProgressArgs ea)
        {
            ExportProgress?.Invoke(this, ea);
        }

        private void CheckParam()
        {
            if (Source == null)
                throw new Exception("Source Schema not specified!");
            if (Target == null)
                throw new Exception("Target Schema not specified!");
        }

        public void Initialize(ExportProgressArgs ea, IEnumerable<DBTable> list = null)
        {
            try
            {
                if (Source == null)
                    throw new Exception("Source Schema not specified!");

                if (ea != null)
                {
                    ea.Type = ExportProgressType.Initialize;
                    ea.Description = "Process start!";
                    ea.Count = Source.Tables.Count;
                    ea.Current = 0;
                    OnExportProgress(ea);
                }
                if (list == null)
                    list = Source.Tables;
                Tables.Clear();

                foreach (var table in list)
                {
                    if (ea.Cancel)
                        return;

                    var esetting = new DBETable(table.Name)
                    {
                        Target = prefix + table.Name.Trim().Replace("_", "")
                    };
                    Tables.Add(esetting);

                    foreach (var column in table.Columns)
                    {
                        esetting.Columns.Add(new DBEColumn(column.Name)
                        {
                            Target = column.Name.Trim().Replace(" ", "_"),
                            DataType = column.DBDataType,
                            Order = column.Order,
                            Size = column.Size,
                            Scale = column.Scale
                        });
                    }
                    if (ea != null)
                    {
                        ea.Current++;
                        ea.Table = esetting;
                        ea.Description = $"Table:{esetting} ({esetting.Columns.Count} columns)";
                    }
                }
                PatchQuery();
            }
            catch (Exception ex)
            {
                ea.Description = ex.Message;
                ea.Exception = ex;
                OnExportProgress(ea);
            }
        }

        public DBColumn GenerateColumn(DBEColumn column)
        {
            var newColumn = (DBColumn)column.SourceColumn.Clone();// new DBColumn(column.Destination);
            newColumn.Name = column.Target;
            newColumn.Order = column.Order;
            newColumn.DBDataType = column.DataType;
            newColumn.Size = column.Size;
            newColumn.Scale = column.Scale;
            return newColumn;
        }

        public DBTable GenerateTable(DBETable extable)
        {
            var newTable = extable.TargetTable;
            if (newTable == null)
                newTable = new DBTable<DBItem>(extable.Target) { Schema = Target };

            foreach (var column in extable.Columns)
                if (column.Check && !newTable.Columns.Contains(column.Target))
                    newTable.Columns.Add(GenerateColumn(column));

            return newTable;
        }

        public void ExportSchema(ExportProgressArgs arg)
        {
            try
            {
                CheckParam();
                arg.Type = ExportProgressType.Schema;
                arg.Count = Tables.Count;
                arg.Current = 0;
                arg.Table = null;
                arg.Description = "Export Schema Started!";
                OnExportProgress(arg);
                foreach (DBETable table in Tables)
                {
                    if (arg.Cancel)
                        return;
                    arg.Description = null;
                    arg.Current++;
                    arg.Table = table;
                    OnExportProgress(arg);
                    if (!table.Check)
                    {
                        continue;
                    }
                    DBTable newTable = GenerateTable(table);
                    if (!Target.Tables.Contains(newTable))
                    {
                        string ddl = newTable.FormatSql(DDLType.Create);
                        newTable.Schema.Connection.ExecuteQuery(ddl);
                        Target.Tables.Add(newTable);
                    }
                }
                arg.Description = "Export Schema Complete!";
                OnExportProgress(arg);

            }
            catch (Exception ex)
            {
                arg.Exception = ex;
                OnExportProgress(arg);
            }
        }

        public DBItem ExportRow(DBETable table, DBItem row)
        {
            DBItem newRow = null;
            if (table.TargetTable.PrimaryKey != null && table.SourceTable.PrimaryKey != null)
                newRow = table.TargetTable.LoadItemById(row.PrimaryId, DBLoadParam.None);
            if (newRow == null)
                newRow = table.TargetTable.NewItem(DBUpdateState.Insert, false);

            foreach (var column in table.Columns)
            {
                if (!column.Check || column.UserDefined || column.TargetColumn == null || column.SourceColumn == null)
                    continue;
                if ((mode & ExportMode.ExcludeAccess) == ExportMode.ExcludeAccess && (mode & ExportMode.Patch) == ExportMode.Patch &&
                    (column.TargetColumn.Keys & DBColumnKeys.Access) == DBColumnKeys.Access && newRow.Attached)
                    continue;
                object val = row[column.SourceColumn];
                if ((mode & ExportMode.DateValidation) == ExportMode.DateValidation && val is DateTime && ((DateTime)val).Year < 1900)
                    val = null;
                if ((mode & ExportMode.StringTrimm) == ExportMode.StringTrimm && val is string)
                    val = ((string)val).Trim();

                newRow[column.TargetColumn] = val;
            }
            return newRow;
        }

        public async Task ExportTable(ExportProgressArgs ea)
        {
            try
            {
                DBETable table = ea.Table;
                if ((mode & ExportMode.Patch) != ExportMode.Patch)
                {
                    table.SourceTable.Clear();
                    table.TargetTable.Clear();
                }
                using (var transacton = new DBTransaction(table.SourceTable.Connection))
                {
                    ea.Current = 0;
                    ea.Count = table.SourceTable.GetRowCount(transacton, table.Query);
                    ea.Description = null;
                    OnExportProgress(ea);

                    using (transacton.Reader = transacton.ExecuteQuery(table.SourceTable.CreateQuery(table.Query, null), DBExecuteType.Reader) as IDataReader)
                    {
                        table.SourceTable.CheckColumns(transacton);
                        while (transacton.Reader.Read())
                        {
                            if (ea.Cancel)
                            {
                                transacton.Cancel();
                                return;
                            }
                            var row = table.SourceTable.LoadItemFromReader(transacton);
                            var newRow = ExportRow(table, row);

                            await table.TargetTable.SaveItem(newRow, null);

                            ea.Current++;
                            ea.Row = newRow;

                            OnExportProgress(ea);

                            if ((mode & ExportMode.Patch) != ExportMode.Patch)
                            {
                                table.SourceTable.Clear();
                                table.TargetTable.Clear();
                            }
                        }
                        transacton.Reader.Close();
                    }
                    transacton.Commit();
                }
            }
            catch (Exception ex)
            {
                ea.Exception = ex;
                OnExportProgress(ea);
            }
        }

        public async void Export(ExportProgressArgs ea)
        {
            ExportSchema(ea);
            ea.Type = ExportProgressType.Data;
            ea.Table = null;
            ea.Count = 0;
            ea.Current = 0;
            ea.Description = "Export Data Started!";
            OnExportProgress(ea);

            foreach (DBETable table in Tables)
            {
                if (ea.Cancel)
                    return;
                if (!table.Check)
                    continue;
                ea.Table = table;
                await ExportTable(ea);
                ea.Description = string.Format("Export {0} rows of {1}!", ea.Count, table.Source);
                OnExportProgress(ea);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChange(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class DBExportList : SelectableList<DBExport>
    {
        public DBExportList() : base()
        { }
    }

    public class GenerateParam
    {
        public ExportMode Mode { get; set; }
        public DateTime PatchDate { get; set; }
    }

}
