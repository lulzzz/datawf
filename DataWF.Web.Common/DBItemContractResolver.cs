﻿using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace DataWF.Web.Common
{
    public class DBItemContractResolver : DefaultContractResolver
    {
        private JsonConverter dbConverter = new DBItemJsonConverter();
        public override JsonContract ResolveContract(Type type)
        {
            var contract = base.ResolveContract(type);
            return contract;
        }


        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (TypeHelper.IsBaseType(objectType, typeof(DBItem)))
            {
                var table = DBTable.GetTableAttributeInherit(objectType);
                if (table != null)
                {
                    var result = new JsonObjectContract(objectType)
                    {
                        Converter = dbConverter
                    };

                    foreach (var column in table.Columns.Where(p => TypeHelper.IsBaseType(p.Property.DeclaringType, objectType)))
                    {
                        var jsonProperty = base.CreateProperty(column.Property, MemberSerialization.OptIn);
                        jsonProperty.ValueProvider = EmitInvoker.Initialize(column.Property);
                        jsonProperty.Ignored = column.ColumnType != DBColumnTypes.Default || (column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access;
                        result.Properties.Add(jsonProperty);

                        if (column.ReferenceProperty != null)
                        {
                            jsonProperty = base.CreateProperty(column.ReferenceProperty, MemberSerialization.OptIn);
                            jsonProperty.ValueProvider = EmitInvoker.Initialize(column.ReferenceProperty);
                            jsonProperty.NullValueHandling = NullValueHandling.Ignore;
                            result.Properties.Add(jsonProperty);
                        }
                    }

                    return result;
                }
            }
            return base.CreateObjectContract(objectType);
        }
    }
}