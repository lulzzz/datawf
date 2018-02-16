﻿using DataWF.Data;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public enum ParamType
    {
        Column,
        Reference,
        Template,
        Procedure,
        Begin,
        End,
        Check,
        Relation,
    }

    public abstract class ParamBase : DBItem
    {
        [NonSerialized()]
        protected object _cache;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Column("typeid")]
        public ParamType? Type
        {
            get { return GetProperty<ParamType?>(nameof(Type)); }
            set { SetProperty(value, nameof(Type)); }
        }

        public abstract string ParamCode { get; set; }

        public object Param
        {
            get
            {
                if (_cache == null)
                {
                    if (!string.IsNullOrEmpty(ParamCode))
                    {
                        ParamType type = Type.GetValueOrDefault();
                        switch (type)
                        {
                            case ParamType.Column:
                                _cache = GetColumn();
                                break;
                            case ParamType.Reference:
                                _cache = GetReference();
                                break;
                            case ParamType.Begin:
                            case ParamType.End:
                            case ParamType.Procedure:
                            case ParamType.Check:
                                _cache = DBService.ParseProcedure(ParamCode);
                                break;
                            case ParamType.Relation:
                                _cache = Stage.DBTable.LoadItemById(ParamCode);
                                break;
                            case ParamType.Template:
                                _cache = Template.DBTable.LoadItemById(ParamCode);
                                break;
                        }
                    }
                }
                return _cache;
            }
            set
            {
                ParamCode = value == null ? null : value is DBItem ? ((DBItem)value).PrimaryId.ToString() : ((DBSchemaItem)value).FullName;
                _cache = null;
            }
        }

        public DBForeignKey GetReference()
        {
            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);

            DBColumn column = DBService.ParseColumn(code, FlowEnvironment.Config.Schema);
            //var result = new DBConstraintForeign() { Table = column.Table, Column = column, Value = column.Reference };
            //if (index >= 0) result.Value = ParamCode.Substring(index + 1);
            return column == null ? null : column.GetForeign();
        }

        public DBColumn GetColumn()
        {
            DBColumn result = null;

            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);
            DBColumn column = DBService.ParseColumn(code, FlowEnvironment.Config.Schema);
            if (column != null)
            {
                result = new DBVirtualColumn(column);
                result.Index = column.Index;
                result.Table = column.Table;
                result.Access = this.Access;
                //TODO reference if (index >= 0)
                //    result.Reference = ParamCode.Substring(index + 1);
                LocaleItem litem = (LocaleItem)column.LocalizeInfo.Clone();
                litem.Category = "DBDocument";
                foreach (DBColumn col in Table.Columns.GetByGroup("name"))
                {
                    string val = this[col].ToString();
                    if (val.Length != 0)
                        litem.Names.Add(val, col.Culture);
                }
                result.LocalizeInfo = litem;
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Type, Param);
        }

        public abstract DBItem Owner { get; }
    }
}