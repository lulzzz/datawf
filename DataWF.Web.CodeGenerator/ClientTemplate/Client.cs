﻿using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Client
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient where T : class
    {
        public Client(string idProperty)
        {
            IdProperty = idProperty;
            if (IdProperty != null)
            {
                Items.Indexes.Add(EmitInvoker.Initialize<T>(IdProperty));
            }
        }

        public readonly string IdProperty;

        public SelectableList<T> Items { get; set; } = new SelectableList<T>();

        public Type ItemType { get { return typeof(T); } }

        public override object DeserializeByType(JsonSerializer serializer, JsonTextReader jreader, Type type)
        {
            if (TypeHelper.IsBaseType(type, ItemType))
            {
                var dictionary = new Dictionary<IInvoker, object>();
                var invoker = (IInvoker)null;
                var id = (object)null;
                var newItem = (T)null;
                var add = true;
                while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
                {
                    if (jreader.TokenType == JsonToken.PropertyName)
                    {
                        invoker = EmitInvoker.Initialize(type, (string)jreader.Value);
                    }
                    else if (jreader.TokenType == JsonToken.StartObject)
                    {
                        if (invoker != null)
                        {
                            dictionary[invoker] = DeserializeObject(serializer, jreader, invoker.DataType);
                        }
                    }
                    else if (invoker != null)
                    {
                        dictionary[invoker] = jreader.Value;
                        if (invoker.Name == IdProperty)
                        {
                            id = jreader.Value;
                        }
                    }
                }
                if (id != null)
                {
                    newItem = Select((K)Helper.Parse(id, typeof(K)));
                    add = false;
                }
                if (newItem == null)
                {
                    newItem = (T)EmitInvoker.CreateObject(type);
                }
                foreach (var entry in dictionary)
                {
                    entry.Key.Set(newItem, Helper.Parse(entry.Value, entry.Key.DataType));
                }
                if (add)
                {
                    Items.Add(newItem);
                }
                return newItem;
            }
            return base.DeserializeByType(serializer, jreader, type);
        }

        public virtual T Select(K id)
        {
            return Items.SelectOne(IdProperty, id);
        }

        public virtual T Get(K id)
        {
            return Select(id) ?? GetAsync(id, CancellationToken.None).Result;
        }

        public abstract Task<List<T>> GetAsync(CancellationToken cancellationToken);

        public Task GetAsync() { return GetAsync(CancellationToken.None); }

        public abstract Task<T> PutAsync(T value, CancellationToken cancellationToken);

        public Task PutAsync(object value) { return PutAsync((T)value, CancellationToken.None); }

        public abstract Task<T> PostAsync(T value, CancellationToken cancellationToken);

        public Task PostAsync(object value) { return PostAsync((T)value, CancellationToken.None); }

        public abstract Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);

        public Task FindAsync(string filter) { return FindAsync(filter, CancellationToken.None); }

        public abstract Task<T> GetAsync(K id, CancellationToken cancellationToken);

        public Task GetAsync(object id) { return GetAsync((K)id, CancellationToken.None); }

        public abstract Task<bool> DeleteAsync(K id, CancellationToken cancellationToken);

        public Task DeleteAsync(object id) { return DeleteAsync((K)id, CancellationToken.None); }
    }
}