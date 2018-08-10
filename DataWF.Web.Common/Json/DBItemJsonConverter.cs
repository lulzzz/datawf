﻿/*
 DBRow.cs
 
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
using System.Linq;
using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json;

namespace DataWF.Web.Common
{
    public class DBItemJsonConverter : JsonConverter
    {
        public bool IsSerializeableColumn(ColumnAttributeCache column)
        {
            return column.PropertyInvoker != null
                && (column.Attribute.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
                && (column.Attribute.Keys & DBColumnKeys.Password) != DBColumnKeys.Password
                && (column.Attribute.Keys & DBColumnKeys.File) != DBColumnKeys.File;
        }

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsBaseType(objectType, typeof(DBItem));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DBItem item)
            {
                writer.WriteStartObject();
                var table = item.Table;
                var valueType = value.GetType();
                var tableAttribute = table.TableAttribute;
                foreach (var column in tableAttribute.Columns.Where(p => TypeHelper.IsBaseType(valueType, p.PropertyInvoker.TargetType)))
                {
                    if (!IsSerializeableColumn(column))
                        continue;
                    writer.WritePropertyName(column.PropertyName);
                    if (column.GetDataType().IsEnum)
                    {
                        writer.WriteValue(column.PropertyInvoker.GetValue(item)?.ToString());
                    }
                    else
                    {
                        writer.WriteValue(column.PropertyInvoker.GetValue(item));
                    }
                }
                foreach (var refing in tableAttribute.Referencings)
                {
                    if (!TypeHelper.IsBaseType(valueType, refing.PropertyInvoker.TargetType))
                        continue;
                    var refs = refing.PropertyInvoker.GetValue(item) as IEnumerable;
                    if (refs != null)
                    {
                        writer.WritePropertyName(refing.Property.Name);
                        writer.WriteStartArray();
                        foreach (var refItem in refs)
                        {
                            serializer.Serialize(writer, refItem);
                        }
                        writer.WriteEndArray();
                    }
                }
                writer.WriteEndObject();
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var item = existingValue as DBItem;
            if (existingValue != null && item == null)
            {
                throw new JsonSerializationException($"Expect {nameof(DBItem)} but {nameof(existingValue)} is {existingValue?.GetType().Name ?? "null"}");
            }
            var tableAttribute = DBTable.GetTableAttributeInherit(objectType);
            if (tableAttribute == null)
            {
                throw new JsonSerializationException($"Can't find table of {objectType?.Name ?? "null"}");
            }
            var dictionary = new Dictionary<IInvoker, object>();
            var key = (IInvoker)null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = tableAttribute.ParseProperty((string)reader.Value);
                    if (key == null)
                    {
                        throw new InvalidOperationException($"Property {reader.Value} not found!");
                    }
                }
                else if (key != null)
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        dictionary[key] = serializer.Deserialize(reader, key.DataType);
                    }
                    else if (reader.TokenType == JsonToken.StartArray)
                    {
                        dictionary[key] = serializer.Deserialize(reader, key.DataType);
                    }
                    else
                    {
                        dictionary[key] = serializer.Deserialize(reader, key.DataType);
                    }
                }
            }

            if (tableAttribute.PrimaryKey != null && dictionary.TryGetValue(tableAttribute.PrimaryKey.PropertyInvoker, out var value) && value != null)
            {
                item = tableAttribute.Table.LoadItemById(value);
            }

            if (item == null)
            {
                if (tableAttribute.TypeKey != null && dictionary.TryGetValue(tableAttribute.TypeKey.PropertyInvoker, out var itemType) && itemType != null)
                    item = tableAttribute.Table.NewItem(DBUpdateState.Insert, true, (int)tableAttribute.TypeKey.Column.ParseValue(itemType));
                else
                    item = tableAttribute.Table.NewItem(DBUpdateState.Insert, true);
            }

            foreach (var entry in dictionary)
            {
                entry.Key.SetValue(item, entry.Value);
            }
            return item;
        }


    }
}