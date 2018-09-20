﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public class Query<T> : IQuery
    {
        private QueryParameterList<T> parameters;
        private QueryOrdersList<T> orders;

        public Query()
        { }

        public Query(IEnumerable<QueryParameter<T>> parameters)
        {
            Parameters.AddRange(parameters);
        }

        public QueryParameterList<T> Parameters
        {
            get { return parameters ?? (parameters = new QueryParameterList<T>()); }
            set { parameters = value; }
        }

        public QueryOrdersList<T> Orders
        {
            get { return orders ?? (orders = new QueryOrdersList<T>()); }
            set { orders = value; }
        }

        IEnumerable<IQueryParameter> IQuery.Parameters
        {
            get { return Parameters; }
        }

        IEnumerable<IComparer> IQuery.Orders
        {
            get { return Orders; }
        }

        public bool IsNotEmpty
        {
            get { return parameters.Any(p => p.IsEnabled); }
        }

        public void Clear()
        {
            Parameters.Clear();
        }

        public void Add(IQueryParameter parameter)
        {
            Add((QueryParameter<T>)parameter);
        }

        public void Add(QueryParameter<T> parameter)
        {
            Parameters.Add(parameter);
        }

        public QueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            return Parameters.Add(logic, invoker, comparer, value);
        }

        public QueryParameter<T> AddOrUpdate(IInvoker invoker, object value)
        {
            var parameter = Parameters[invoker.Name];
            return AddOrUpdate(parameter?.Logic ?? LogicType.And, invoker, parameter?.Comparer ?? CompareType.Equal, value);
        }

        public QueryParameter<T> AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            var parameter = Parameters[invoker.Name];
            if (parameter == null)
            {
                parameter = Parameters.Add(logic, invoker, comparer, null);
            }
            parameter.Logic = logic;
            parameter.Comparer = comparer;
            parameter.Value = value;
            return parameter;
        }

        IQueryParameter IQuery.Add(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            return Add(logic, invoker, comparer, value);
        }

        public IQueryParameter AddTreeParameter()
        {
            var parameter = new QueryParameter<T>()
            {
                Invoker = new TreeInvoker<IGroup>(),
                Comparer = CompareType.Equal,
                Value = true
            };
            Parameters.Add(parameter);
            return parameter;
        }

        public bool Remove(string parameter)
        {
            return Remove(Parameters[parameter]);
        }

        public bool Remove(IQueryParameter parameter)
        {
            return Remove((QueryParameter<T>)parameter);
        }

        public bool Remove(QueryParameter<T> parameter)
        {
            return Parameters.Remove(parameter);
        }

        public InvokerComparerList<T> GetComparer()
        {
            if (Orders.Count > 0)
            {
                return new InvokerComparerList<T>(Orders);
            }
            return null;
        }

        public void Sort(IList<T> list)
        {
            var comparers = GetComparer();
            if (comparers != null)
            {
                ListHelper.QuickSort(list, comparers);
            }
        }

        public string Format()
        {
            var logic = false;
            var builder = new StringBuilder();
            foreach (var parametr in Parameters)
            {
                if (parametr.IsEnabled && !parametr.IsEmptyFormat)
                {
                    parametr.Format(builder, logic);
                    logic = true;
                }
            }
            return builder.ToString();
        }

        void IQuery.Sort(IList list)
        {
            Sort((IList<T>)list);
        }

        public void ClearValues()
        {
            Parameters.ClearValues();
        }
    }
}

