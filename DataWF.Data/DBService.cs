﻿/*
 DBService.cs
 
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DataWF.Common;

namespace DataWF.Data
{
    public delegate object ExecuteDelegate(ExecuteEventArgs arg);

    public delegate void DBExecuteDelegate(DBExecuteEventArg arg);

    public delegate void DBItemEditEventHandler(DBItemEventArgs arg);

    public class ExecuteEventArgs : CancelEventArgs
    {
        public ExecuteEventArgs(DBSchema schema, DBTable table, string query)
        {
            Schema = schema;
            Table = table;
            Query = query;
        }

        public DBSchema Schema { get; set; }

        public DBTable Table { get; set; }

        public string Query { get; set; }
    }

    public class DBExecuteEventArg : EventArgs
    {
        public DBExecuteEventArg()
        {
        }

        public object Rezult { get; set; }

        public TimeSpan Time { get; set; }

        public string Query { get; set; }

        public DBExecuteType Type { get; set; }
    }

    /// <summary>
    /// Service for connection
    /// </summary>
    public static class DBService
    {
        //private static object[] fabricparam = new object[1];
        //private static Type[] param = new Type[] { typeof(DBItem) };
        //private static Type[] param2 = new Type[] { typeof(DBRow) };
        public static char[] DotSplit = { '.' };
        private static DBSchema defaultSchema;
        private static DBConnectionList connections = new DBConnectionList();
        private static DBSchemaList items = new DBSchemaList();

        public static void SaveCache()
        {
            foreach (DBSchema schema in items)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.Count > 0 && table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.SaveFile();
                    }
                }
            }
        }

        public static void LoadCache()
        {
            foreach (DBSchema schema in items)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.LoadFile();
                    }
                }
            }
        }

        public static event EventHandler<DBSchemaChangedArgs> DBSchemaChanged;

        public static void OnDBSchemaChanged(DBSchemaItem item, DDLType type)
        {
            if (item is DBTable && !DBService.Schems.Contains(((DBTable)item).Schema))
                return;

            DBSchemaChanged?.Invoke(item, new DBSchemaChangedArgs { Item = item, Type = type });
        }

        public static event DBItemEditEventHandler RowAdded;

        internal static void RaiseRowAdded(DBItem e)
        {
            var args = new DBItemEventArgs(e) { State = DBUpdateState.Insert };
            RaiseRowStateEdited(args);
            RowAdded?.Invoke(args);
        }

        public static event DBItemEditEventHandler RowRemoved;

        internal static void RaiseRowRemoved(DBItem e)
        {
            var args = new DBItemEventArgs(e) { State = DBUpdateState.Delete };
            RaiseRowStateEdited(args);
            RowRemoved?.Invoke(args);
        }

        public static event DBItemEditEventHandler RowEditing;

        internal static void RaiseRowEditing(DBItemEventArgs e)
        {
            if (RowEditing != null)
                RowEditing(e);
        }

        public static event DBItemEditEventHandler RowEdited;

        internal static void RaiseRowEdited(DBItemEventArgs e)
        {
            if (RowEdited != null)
                RowEdited(e);
        }

        public static event DBItemEditEventHandler RowStateEdited;

        internal static void RaiseRowStateEdited(DBItemEventArgs e)
        {
            if (RowStateEdited != null)
                RowStateEdited(e);
        }

        public static event DBItemEditEventHandler RowUpdating;

        internal static void RaiseRowUpdating(DBItemEventArgs e)
        {
            if (RowUpdating != null)
                RowUpdating(e);
        }

        public static event DBItemEditEventHandler RowUpdated;

        internal static void RaiseRowUpdated(DBItemEventArgs e)
        {
            if (RowUpdated != null)
                RowUpdated(e);
        }

        public static event DBItemEditEventHandler RowAccept;

        internal static void RaiseRowAccept(DBItem item)
        {
            if (RowAccept != null)
                RowAccept(new DBItemEventArgs(item));
        }

        public static event DBItemEditEventHandler RowReject;

        internal static void RaiseRowReject(DBItem item)
        {
            if (RowReject != null)
                RowReject(new DBItemEventArgs(item));
        }

        public static void Save()
        {
            Save("data.xml");
        }

        public static void Save(string file)
        {
            Serialization.Serialize(connections, "connections.xml");
            Serialization.Serialize(items, file);
        }

        public static void Load()
        {
            Load("data.xml");
        }

        public static void Load(string file)
        {
            Serialization.Deserialize("connections.xml", connections);
            Serialization.Deserialize(file, items);
        }

        public static DBConnectionList Connections
        {
            get { return connections; }
            set { connections = value; }
        }

        public static DBSchemaList Schems
        {
            get { return items; }
        }

        public static DBSchema DefaultSchema
        {
            get
            {
                if (defaultSchema == null && items.Count > 0)
                    defaultSchema = items[0];
                return defaultSchema;
            }
            set
            {
                defaultSchema = value;
                if (!items.Contains(defaultSchema))
                    items.Add(defaultSchema);
            }
        }

        public static DBColumn ParseColumn(string name, DBSchema s = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            DBColumn column = null;
            DBTable table = ParseTable(name, s);

            int index = name.LastIndexOf('.');
            name = index < 0 ? name : name.Substring(index + 1);
            if (s == null)
                s = DBService.DefaultSchema;


            if (table != null)
            {
                column = table.ParseColumn(name);
            }
            else if (s != null)
            {
                foreach (var t in s.Tables)
                {
                    column = t.Columns[name];
                    if (column != null)
                        break;
                }
            }
            return column;
        }

        public static DBTable ParseTable(string code, DBSchema s = null)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DBTable table = null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index >= 0)
            {
                schema = DBService.Schems[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ? code.Substring(index) : code.Substring(index, sindex - index);
            }
            if (schema == null)
                schema = s;
            if (schema != null)
            {
                table = schema.Tables[code];
            }
            else
            {
                foreach (var sch in DBService.Schems)
                {
                    table = sch.Tables[code];
                    if (table != null)
                        break;
                }
            }
            return table;
        }

        public static DBTableGroup ParseTableGroup(string code, DBSchema s = null)
        {
            if (code == null)
                return null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index < 0)
                schema = s;
            else
            {
                schema = DBService.Schems[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ?
                    code.Substring(index) :
                    code.Substring(index, sindex - index);
            }
            return schema.TableGroups[code];
        }

        public static bool GetBool(DBItem row, string ColumnCode)
        {
            return GetBool(row, row.Table.Columns[ColumnCode]);
        }

        public static bool GetBool(DBItem row, DBColumn Column)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return false;

            return row[Column].ToString() == Column.BoolTrue;
        }

        public static void SetBool(DBItem row, string ColumnCode, bool Value)
        {
            SetBool(row, row.Table.Columns[ColumnCode], Value);
        }

        public static void SetBool(DBItem row, DBColumn Column, bool Value)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return;
            row[Column] = Value ? Column.BoolTrue : Column.BoolFalse;
        }

        public static DateTime GetDateVal(object val)
        {
            if (val == null)
                return DateTime.MinValue;
            if (val is DateTime)
                return (DateTime)val;
            return DateTime.Parse(val.ToString());
        }

        public static DateTime GetDate(DBItem row, DBColumn Column)
        {
            return GetDateVal(row[Column]);
        }

        public static DateTime GetDate(DBItem row, string Column)
        {
            return GetDateVal(row[Column]);
        }

        public static void SetDate(DBItem row, DBColumn Column, DateTime value)
        {
            row[Column] = value;
        }

        public static TimeSpan GetTimeSpan(DBItem row, DBColumn Column)
        {
            object val = row[Column];
            if (val == null)
                return new TimeSpan();
            if (val is TimeSpan)
                return (TimeSpan)val;
            return TimeSpan.Parse(val.ToString());
        }

        public static void SetTimeSpan(DBItem row, DBColumn Column, TimeSpan value)
        {
            row[Column] = value;
        }

        public static byte[] GetZip(DBItem row, DBColumn column)
        {
            var data = row.GetValue<byte[]>(column);
            if (data != null && Helper.IsGZip(data))
                data = Helper.ReadGZip(data);
            return data;
        }

        public static byte[] SetZip(DBItem row, DBColumn column, byte[] data)
        {
            byte[] temp = data != null && data.Length > 500 ? Helper.WriteGZip(data) : data;
            row.SetValue(temp, column);
            return temp;
        }

        public static int GetIntValue(object value)
        {
            if (value == null)
                return 0;
            if (value is int)
                return (int)value;
            if (value is int?)
                return ((int?)value).Value;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        public static DBColumnGroup InitColumnGroup(DBTable table, string code)
        {
            DBColumnGroup cs = null;
            cs = table.ColumnGroups[code];
            if (cs == null)
            {
                cs = new DBColumnGroup(code);
                table.ColumnGroups.Add(cs);
            }
            return cs;
        }

        public static DBColumn InitColumn(DBTable table, string code)
        {
            DBColumn cs = null;
            cs = table.Columns[code];
            if (cs == null)
            {
                cs = new DBColumn(code);
                table.Columns.Add(cs);
            }
            return cs;
        }

        public static DBTable<T> ExecuteTable<T>(string tableName, string query) where T : DBItem, new()
        {
            var table = new DBTable<T>(tableName);
            table.Schema = DefaultSchema;
            table.Load(query);
            return table;
        }

        public static QResult ExecuteQResult(DBConnection connection, string query, bool noTransaction = true)
        {
            using (var transaction = new DBTransaction(connection, query, noTransaction))
            {
                return ExecuteQResult(transaction);
            }
        }

        public static QResult ExecuteQResult(DBTransaction transaction)
        {
            var list = new QResult();
            ExecuteQResult(transaction, list);
            return list;
        }

        public static void ExecuteQResult(DBTransaction transaction, QResult list)
        {
            list.Values.Clear();
            list.Columns.Clear();
            using (var reader = ExecuteQuery(transaction, DBExecuteType.Reader) as IDataReader)
            {
                int fCount = reader.FieldCount;
                for (int i = 0; i < fCount; i++)
                {
                    var name = reader.GetName(i);
                    list.Columns.Add(name, new QField { Index = i, Name = name, DataType = reader.GetFieldType(i) });
                }
                list.OnColumnsLoaded();
                while (reader.Read())
                {
                    var objects = new object[fCount];
                    reader.GetValues(objects);
                    list.Values.Add(objects);
                }
                reader.Close();
                list.OnLoaded();
            }
        }

        internal static List<List<KeyValuePair<string, object>>> ExecuteListPair(DBConnection cs, string query)
        {
            List<List<KeyValuePair<string, object>>> list = null;

            using (var transaction = new DBTransaction(cs, query))
            {
                list = new List<List<KeyValuePair<string, object>>>();
                using (var reader = ExecuteQuery(transaction, DBExecuteType.Reader) as IDataReader)
                {
                    int fCount = reader.FieldCount;
                    while (reader.Read())
                    {
                        var objects = new List<KeyValuePair<string, object>>(fCount);
                        for (int i = 0; i < fCount; i++)
                            objects.Add(new KeyValuePair<string, object>(reader.GetName(i), reader.GetValue(i)));
                        list.Add(objects);
                    }
                    reader.Close();
                }
            }
            return list;
        }

        public static List<Dictionary<string, object>> ExecuteListDictionary(DBConnection cs, string query)
        {
            using (var transaction = new DBTransaction(cs, query))
                return ExecuteListDictionary(transaction);
        }

        public static List<Dictionary<string, object>> ExecuteListDictionary(DBTransaction transaction)
        {
            var list = new List<Dictionary<string, object>>();
            using (var reader = ExecuteQuery(transaction, DBExecuteType.Reader) as IDataReader)
            {
                int fCount = reader.FieldCount;
                while (reader.Read())
                {
                    var objects = new Dictionary<string, object>(fCount, StringComparer.InvariantCultureIgnoreCase);
                    for (int i = 0; i < fCount; i++)
                        objects.Add(reader.GetName(i), reader.GetValue(i));
                    list.Add(objects);
                }
                reader.Close();
            }
            return list;
        }

        public static object ExecuteQuery(DBConnection connection, string query, bool noTransaction = false, DBExecuteType type = DBExecuteType.Scalar)
        {
            if (string.IsNullOrEmpty(query))
                return null;
            using (var transaction = new DBTransaction(connection, query, noTransaction))
            {
                var result = ExecuteQuery(transaction, type);
                transaction.Commit();
                return result;
            }
        }

        public static List<object> ExecuteGoQuery(DBConnection config, string query, bool noTransaction = true, DBExecuteType type = DBExecuteType.Scalar)
        {
            var regex = new Regex(@"\s*go\s*(\n|$)", RegexOptions.IgnoreCase);
            var split = regex.Split(query);
            var result = new List<object>(split.Length);
            foreach (var go in split)
            {
                if (go.Trim().Length == 0)
                {
                    continue;
                }
                result.Add(ExecuteQuery(config, go, noTransaction, type));
            }
            return result;

        }

        //public static FormatCommand(IDbCommand command)
        //{
        //if (command.Parameters.Count > 0)
        //{
        //    text += Environment.NewLine;
        //    foreach (IDataParameter param in command.Parameters)
        //    {
        //        string ap = param.Value is string ? "'" : string.Empty;
        //        string tex = param.Value == null || param.Value == DBNull.Value ? "null" : ap + param.Value.ToString() + ap;
        //        text = text.Replace(param.ParameterName, tex);
        //    }
        //}
        //}
        public static object ExecuteQuery(DBTransaction transaction, DBExecuteType type = DBExecuteType.Scalar)
        {
            return ExecuteQuery(transaction, transaction.Command, type);
        }

        public static object ExecuteQuery(DBTransaction transaction, IDbCommand command, DBExecuteType type = DBExecuteType.Scalar)
        {
            object buf = null;
            var watch = new Stopwatch();
            try
            {
                Debug.WriteLine(command.CommandText);
                watch.Start();
                switch (type)
                {
                    case DBExecuteType.Scalar:
                        buf = command.ExecuteScalar();
                        break;
                    case DBExecuteType.Reader:
                        buf = command.ExecuteReader();
                        break;
                    case DBExecuteType.NoReader:
                        buf = command.ExecuteNonQuery();
                        break;
                }

                watch.Stop();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ex.HelpLink = Environment.StackTrace;
                buf = ex;
            }
            finally
            {
                OnExecute(type, command.CommandText, watch.Elapsed, buf);
                if (buf is Exception)
                    throw (Exception)buf;
            }
            return buf;
        }

        public static void RefreshToString()
        {
            foreach (DBSchema s in Schems)
                foreach (DBTable table in s.Tables)
                    foreach (DBItem row in table)
                        row.cacheToString = string.Empty;
        }

        public static string FormatToSqlText(object value)
        {
            if (value is DBItem)
                value = ((DBItem)value).PrimaryId;

            if (value == null)
                return "null";
            else if (value is string)
                return "'" + ((string)value).Replace("'", "''") + "'";
            else if (value is DateTime)
                if (((DateTime)value).TimeOfDay == TimeSpan.Zero)
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd") + "'";
                else
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            else if (value is byte[])
            {
                var sBuilder = new StringBuilder();
                var data = (byte[])value;
                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                sBuilder.Append("0x");
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
            else
                return value.ToString().Replace(",", ".");
        }

        public static event DBExecuteDelegate Execute;

        internal static void OnExecute(DBExecuteType type, string text, TimeSpan ms, object rez)
        {           
            Execute?.Invoke(new DBExecuteEventArg { Time = ms, Query = text, Type = type, Rezult = rez });
        }

        public static int CompareDBTable(DBTable x, DBTable y)
        {
            if (x == y)
                return 0;
            if (x.Type != y.Type)
            {
                if (x.Type == DBTableType.Table)
                    return -1;
                else
                    return 1;
            }
            var xpars = new List<DBTable>();
            x.GetAllParentTables(xpars);
            var ypars = new List<DBTable>();
            y.GetAllParentTables(ypars);
            var xchil = new List<DBTable>();
            x.GetAllChildTables(xchil);
            var ychil = new List<DBTable>();
            y.GetAllChildTables(ychil);

            if (xpars.Contains(y))
                return 1;
            else if (ypars.Contains(x))
                return -1;
            else
            {
                List<DBTable> merge = (List<DBTable>)ListHelper.AND(xpars, ypars, null);
                if (merge.Count > 0)
                {
                    int r = xpars.Count.CompareTo(ypars.Count);
                    if (r != 0)
                        return r;
                }
                // foreach(DBTable xp in xpars)
                //     if(xp.GetChildTables())
            }

            if (xchil.Contains(y))
                return -1;
            else if (ychil.Contains(x))
                return 1;
            else
            {
                List<DBTable> merge = (List<DBTable>)ListHelper.AND(xchil, ychil, null);
                if (merge.Count > 0)
                {
                    int r = xchil.Count.CompareTo(ychil.Count);
                    if (r != 0)
                        return r;
                }
                // foreach(DBTable xp in xpars)
                //     if(xp.GetChildTables())
            }
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        //public static T Fabric<T>(DBItem row) where T : DBItem, new()
        //{
        //    if (row == null)
        //        return null;
        //    T view = row as T;
        //    if (view == null)//&& view.Table == table
        //    {
        //        view = new T();
        //        view.Table = row.Table;
        //        row.Table.Rows.Replace(row, view);
        //    }
        //    return view;
        //}

        public static DBItem FabricRow(DBItem row, Type t)
        {
            if (row == null)
                return null;
            DBItem view = row;
            if (view != null && TypeHelper.IsBaseType(view.GetType(), t))
                return view;

            var pa = EmitInvoker.Initialize(t, Type.EmptyTypes, true);
            if (pa == null)
                throw new InvalidOperationException(string.Format("Type {0} must have constructor with DBRow parameters", t));
            var rowview = (DBItem)pa.Create(new object[] { row });
            rowview.Table = row.Table;
            //rowview.Initialize(row);
            return rowview;
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row column data in text.
        /// </returns>
        /// <param name='table'>
        /// Table of schema
        /// </param>
        /// <param name='id'>
        /// Identifier(by primary column)
        /// </param>
        public static string GetRowText(DBTable table, object id)
        {
            return GetRowText(table, id, table.Columns.GetViewColumns());
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='table'>
        /// Table of schema
        /// </param>
        /// <param name='id'>
        /// Identifier (by primary column)
        /// </param>
        /// <param name='parameters'>
        /// Parameters (collection of DBColumn)
        /// </param>
        public static string GetRowText(DBTable table, object id, IEnumerable<DBColumn> parameters)
        {
            return GetRowText(table, id, parameters, false, " - ");
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='table'>
        /// Table.
        /// </param>
        /// <param name='id'>
        /// Identifier (by primary column)
        /// </param>
        /// <param name='parametrs'>
        /// Parametrs (collection of DBColumn)
        /// </param>
        /// <param name='showColumn'>
        /// Show column in result
        /// </param>
        /// <param name='separator'>
        /// Separator(between pair of column-value)
        /// </param>
        public static string GetRowText(DBTable table, object id, IEnumerable<DBColumn> parametrs, bool showColumn, string separator)
        {
            return GetRowText(table.LoadItemById(id), parametrs, showColumn, separator);
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='table'>
        /// Table.
        /// </param>
        /// <param name='id'>
        /// Identifier.
        /// </param>
        /// <param name='allColumns'>
        /// All columns.
        /// </param>
        /// <param name='showColumn'>
        /// Show column.
        /// </param>
        /// <param name='separator'>
        /// Separator.
        /// </param>
        public static string GetRowText(DBTable table, object id, bool allColumns, bool showColumn, string separator)
        {
            return GetRowText(table.LoadItemById(id), (allColumns ? (IEnumerable<DBColumn>)table.Columns : table.Columns.GetViewColumns()), showColumn, separator);
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='row'>
        /// Row.
        /// </param>
        /// <param name='allColumns'>
        /// All columns.
        /// </param>
        /// <param name='showColumn'>
        /// Show column.
        /// </param>
        /// <param name='separator'>
        /// Separator.
        /// </param>
        public static string GetRowText(DBItem row, bool allColumns, bool showColumn, string separator)
        {
            return GetRowText(row, (allColumns ? (IEnumerable<DBColumn>)row.Table.Columns : row.Table.Columns.GetViewColumns()), showColumn, separator);
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='row'>
        /// Row.
        /// </param>
        public static string GetRowText(DBItem row)
        {
            if (row == null)
                return "<null>";
            //DBTable table = row.VirtualTable != null ? row.VirtualTable : row.Table;
            return GetRowText(row, row.Table.Columns.GetViewColumns(), false, " - ");
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='row'>
        /// Row.
        /// </param>
        /// <param name='parameters'>
        /// Parameters.
        /// </param>
        public static string GetRowText(DBItem row, ICollection<DBColumn> parameters)
        {
            if (row == null)
                return "<null>";
            return GetRowText(row, parameters, false, " - ");
        }

        /// <summary>
        /// Gets the row text.
        /// </summary>
        /// <returns>
        /// The row text.
        /// </returns>
        /// <param name='row'>
        /// Row.
        /// </param>
        /// <param name='parameters'>
        /// Parameters.
        /// </param>
        /// <param name='showColumn'>
        /// Show column.
        /// </param>
        /// <param name='separator'>
        /// Separator.
        /// </param>
        public static string GetRowText(DBItem row, IEnumerable<DBColumn> parameters, bool showColumn, string separator)
        {
            if (row == null)
                return "<null>";
            else if (!row.Access.View)
                return "********";
            string bufRez = "";
            if (parameters == null)
                parameters = row.Table.Columns;
            //if (!parameters.Any())
            //{
            //    if (row.Table.CodeKey != null)
            //        parameters.Add(row.Table.CodeKey);
            //    else if (row.Table.PrimaryKey != null)
            //        parameters.Add(row.Table.PrimaryKey);
            //}
            string c = string.Empty;
            foreach (DBColumn column in parameters)
            {
                if (!column.Access.View)
                {
                    //bufRez += temprez;
                    continue;
                }
                string header = "";
                if (showColumn)
                    header = $"{column}: ";
                string value = FormatValue(column, row[column]);
                string temprez = header + value + separator;
                if (column.IsCulture)
                {
                    if (column.Culture.TwoLetterISOLanguageName == Locale.Data.Culture.TwoLetterISOLanguageName)
                    {
                        bufRez += temprez;
                        if (value.Length != 0)
                            c = null;
                    }
                    else if (c != null && value.Length != 0)
                        c += value;
                }
                else
                    bufRez += temprez;
            }
            if (c != null)
                bufRez += " " + c;
            bufRez = bufRez.Trim((separator + " ").ToCharArray());

            return bufRez;
        }

        public static string FormatValue(DBColumn column, object val)
        {
            //if value passed to format is null
            if (val == null)
                return "";
            if (column == null)
                return val.ToString();
            if ((column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean)
            {
                if (val.ToString().Equals(column.BoolTrue))
                    return "Check";
                else
                    return "Uncheck";
            }
            if (column.IsReference)
            {
                DBItem temp = column.ReferenceTable.LoadItemById(val);
                return temp == null ? "<new or empty>" : temp.ToString();
            }

            if (column.DataType == typeof(string))
                return val.ToString();

            if (column.DataType == typeof(byte[]))
            {
                if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                {
                    AccessValue cash = new AccessValue();
                    cash.Read((byte[])val);
                    string rez = string.Empty;
                    foreach (var item in cash.Items)
                    {
                        rez += string.Format("{0}{1}", rez.Length > 0 ? "; " : string.Empty, item);
                    }
                    return rez;
                }
                else
                    return Helper.LengthFormat(((byte[])val).LongLength);
            }
            if (column.Format != null)
            {
                MethodInfo mi = val.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                if (column.Format.ToLower() == "p")
                    if (val is decimal)
                        return ((decimal)val * 100).ToString("N") + "%";
                    else if (val is double)
                        return ((double)val * 100).ToString("N") + "%";
                    else if (val is float)
                        return ((float)val * 100).ToString("N") + "%";
                    else return (decimal.Parse(val.ToString()) * 100).ToString("N") + "%";
                if (column.Format.ToLower() == "b" && column.DataType == typeof(string) && column.Size == 1)
                    if (val.ToString() == "RowSetting")
                        return "V";
                    else
                        return "X";
                else if (mi != null)
                    return (string)mi.Invoke(val, new object[] { column.Format });
            }

            if (val is DateTime)
                return val.Equals(((DateTime)val).Date) ? ((DateTime)val).ToString("yyyy.MM.dd") : val.ToString();
            return val.ToString();
        }

        /// <summary>
        /// Parces the value.
        /// </summary>
        /// <returns>
        /// The value.
        /// </returns>
        /// <param name='column'>
        /// Cs.
        /// </param>
        /// <param name='value'>
        /// Value.
        /// </param>
        public static object ParseValue(DBColumn column, object value)
        {
            object buf = null;
            if (column == null)
                return value;
            if (value is bool && (column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean && column.DataType != typeof(bool))
                value = (bool)value ? column.BoolTrue : column.BoolFalse;
            if (value == null || value == DBNull.Value)
                buf = null;
            else if (column.DataType == value.GetType())
                buf = value;
            else if (value is DBItem)
                buf = ((DBItem)value).PrimaryId;
            //else if (column.Pull.ItemType.IsG )
            //buf = ((DBItem)value).PrimaryId;
            else
                buf = ParseValue(column, value.ToString());

            if (buf is DateTime && buf.Equals(DateTime.MinValue))
                buf = null;
            return buf;
        }

        public static string FormatStatusFilter(DBTable table, DBStatus filter)
        {
            string rez = string.Empty;
            if (table.StatusKey != null && filter != 0 && filter != DBStatus.Empty)
            {
                var qlist = new QEnum();
                if ((filter & DBStatus.Actual) == DBStatus.Actual)
                    qlist.Items.Add(new QValue((int)DBStatus.Actual));
                if ((filter & DBStatus.New) == DBStatus.New)
                    qlist.Items.Add(new QValue((int)DBStatus.New));
                if ((filter & DBStatus.Edit) == DBStatus.Edit)
                    qlist.Items.Add(new QValue((int)DBStatus.Edit));
                if ((filter & DBStatus.Delete) == DBStatus.Delete)
                    qlist.Items.Add(new QValue((int)DBStatus.Delete));
                if ((filter & DBStatus.Archive) == DBStatus.Archive)
                    qlist.Items.Add(new QValue((int)DBStatus.Archive));
                if ((filter & DBStatus.Error) == DBStatus.Error)
                    qlist.Items.Add(new QValue((int)DBStatus.Error));
                var param = new QParam()
                {
                    ValueLeft = new QColumn(table.StatusKey),
                    Comparer = CompareType.In,
                    ValueRight = qlist
                };

                rez = param.Format();
            }
            return rez;
        }

        /// <summary>
        /// Parses the value.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="column">Column.</param>
        /// <param name="value">Value.</param>
        public static object ParseValue(DBColumn column, string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            object val = null;
            var type = column.DataType;
            if (type == typeof(decimal))
            {
                if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var d))
                    val = d;
            }
            else if (type == typeof(DateTime))
            {
                var index = value.IndexOf('|');
                if (index >= 0)
                    value = value.Substring(0, index);
                DateTime date;
                if (value.Equals("getdate()", StringComparison.OrdinalIgnoreCase) || value.Equals("current_timestamp", StringComparison.OrdinalIgnoreCase))
                    val = DateTime.Now;
                if (DateTime.TryParse(value, out date))
                    val = date;
                else if (DateTime.TryParseExact(value, new string[] { "yyyyMMdd", "yyyyMM" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                    val = date;
            }
            else if (type == typeof(string))
                val = value;
            else if (type == typeof(int) || type.IsEnum)
            {
                if (int.TryParse(value, out int i))
                    val = i;
            }
            else if (type == typeof(byte))
            {
                if (byte.TryParse(value, out byte i))
                    val = i;
            }
            else if (type == typeof(TimeSpan))
                val = TimeSpan.Parse(value);
            else if (type == typeof(double))
            {
                if (double.TryParse(value, out double d))
                    val = d;
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float f))
                    val = f;
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(value, out long l))
                    val = l;
            }

            return val;
        }

        public static bool Equal(object field, object value)
        {
            if (field == null)
                return value == null;

            bool equal = field.Equals(value);
            if (!equal && field is byte[] && value is byte[])
                equal = Helper.CompareByte((byte[])field, (byte[])value);
            return equal;
        }

        public static List<DBItem> GetChilds(DBItem row, int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            var rows = new List<DBItem>();
            recurs--;
            var relations = row.Table.GetChildRelations();
            foreach (DBForeignKey relation in relations)
            {
                if (relation.Table.Name.IndexOf("drlog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    relation.Table.Type != DBTableType.Table ||
                    relation.Column.ColumnType != DBColumnTypes.Default)
                    continue;
                if (recurs >= 0 || relation.Table == row.Table)
                {
                    var list = row.GetReferencing(relation, param);
                    foreach (DBItem item in list)
                    {
                        if (item != row)
                        {
                            var childs = GetChilds(item, recurs, param);
                            foreach (var child in childs)
                                if (!rows.Contains(child))
                                    rows.Add(child);
                            if (!rows.Contains(item))
                                rows.Add(item);
                        }
                    }
                }
            }
            return rows;
        }

        public static void Delete(DBItem row, int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            try
            {
                recurs--;
                var relations = row.Table.GetChildRelations();
                foreach (DBForeignKey relation in relations)
                {
                    if (relation.Table.Name.IndexOf("drlog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        relation.Table.Type != DBTableType.Table ||
                    relation.Column.ColumnType != DBColumnTypes.Default)
                        continue;
                    if (recurs >= 0 || relation.Table == row.Table)
                    {
                        var list = row.GetReferencing(relation, param);
                        foreach (DBItem item in list)
                        {
                            if (item.Attached)
                                Delete(item, recurs, param);
                        }
                    }
                }
                if ((row.DBState & DBUpdateState.Insert) == DBUpdateState.Insert)
                    row.Table.Remove(row);
                else
                {
                    row.Delete();
                    row.Save();
                }
            }
            catch (Exception ex)//TODO If Timeout Expired
            {
                Helper.OnException(ex);
            }
        }

        public static void Merge(IEnumerable list, DBItem main)
        {
            var relations = main.Table.GetChildRelations().ToList();
            var rows = new List<DBItem>();
            rows.Add(main);
            foreach (DBItem item in list)
                if (item != main)
                {
                    rows.Add(item);

                    item.DBState |= DBUpdateState.Delete;
                    foreach (DBColumn column in item.Table.Columns)
                        if (main[column] == DBNull.Value && item[column] != DBNull.Value)
                            main[column] = item[column];

                    foreach (DBForeignKey relation in relations)
                        if (relation.Table.Type == DBTableType.Table)
                        {
                            var refings = item.GetReferencing<DBItem>(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
                            if (refings.Count > 0)
                            {
                                foreach (DBItem refing in refings)
                                    refing[relation.Column] = main.PrimaryId;

                                relation.Table.Save(refings);
                            }
                        }
                }
            main.Table.Save(rows);

        }

        public static IDbCommand CreateCommand(IDbConnection connection, string text = null, IDbTransaction transaction = null)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandTimeout = connection.ConnectionTimeout;
            command.CommandText = text;
            if (transaction != null)
                command.Transaction = transaction;

            return command;
        }

        public static object GetItem(List<KeyValuePair<string, object>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Compare(list[i].Key, key, true) == 0)
                    return list[i].Value;
            return DBNull.Value;
        }

        public static T FabricRow<T>()
        {
            throw new NotImplementedException();
        }

        public static ColumnAttribute GetColumnAttribute(PropertyInfo property)
        {
            var config = (ColumnAttribute)Attribute.GetCustomAttribute(property, typeof(ColumnAttribute));
            if (config == null)
            {
                config = (ColumnAttribute)Attribute.GetCustomAttribute(property, typeof(VirtualColumnAttribute));
            }
            return config;
        }

        private static Dictionary<Type, TableAttribute> cacheTables = new Dictionary<Type, TableAttribute>();

        public static TableAttribute GetTableAttribute(Type type)
        {
            if (!cacheTables.TryGetValue(type, out TableAttribute table))
            {
                table = (TableAttribute)Attribute.GetCustomAttribute(type, typeof(TableAttribute));
                if (table == null)
                {
                    table = (TableAttribute)Attribute.GetCustomAttribute(type, typeof(VirtualTableAttribute));
                }
                if (table != null)
                {
                    table.Initialize(type);
                    cacheTables[type] = table;
                }
            }
            return table;
        }

        public static DBTable<T> GetTable<T>(bool generate = false) where T : DBItem, new()
        {
            return (DBTable<T>)GetTable(typeof(T), generate);
        }

        public static DBTable GetTable(Type type, bool generate = false)
        {
            var config = GetTableAttribute(type);
            if (config != null)
            {
                if (config.Table == null && generate)
                    config.Generate(type);
                return config.Table;
            }
            return null;
        }

        public static List<DBSchema> Generate(Assembly assembly)
        {
            var schems = new List<DBSchema>();
            foreach (var type in assembly.GetTypes())
            {
                var table = GetTable(type, true);
                if (table != null && !schems.Contains(table.Schema))
                    schems.Add(table.Schema);
            }
            return schems;
        }

        public static DBProcedure ParseProcedure(string name)
        {
            foreach (var schema in Schems)
            {
                var procedure = schema.Procedures[name];
                if (procedure != null)
                    return procedure;
            }
            return null;
        }

        public static void ClearChache()
        {
            cacheTables.Clear();
        }

        private static List<int> accessGroups = new List<int>();

        public static List<int> AccessGroups { get { return accessGroups; } }

    }

}