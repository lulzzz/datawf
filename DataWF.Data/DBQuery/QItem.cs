﻿/*
 QItem.cs
 
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
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;
using System.Data;

namespace DataWF.Data
{
    public class QItem : IDisposable, IContainerNotifyPropertyChanged, IComparable
    {
        protected int order = -1;
        protected string text;
        protected string alias;

        public QItem()
        {
        }

        public QItem(string name)
        {
            this.text = name;
        }

        public INotifyListPropertyChanged Container { get; set; }


        [Browsable(false)]
        public IQItemList List
        {
            get { return Container as IQItemList; }
        }

        public int Order
        {
            get { return order; }
            set
            {
                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        public virtual string Text
        {
            get { return text; }
            set
            {
                if (text == value)
                    return;
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public string Alias
        {
            get { return alias; }
            set
            {
                if (alias != value)
                {
                    alias = value;
                    OnPropertyChanged(nameof(Alias));
                }
            }
        }

        [Browsable(false)]
        public virtual IQuery Query
        {
            get { return List?.Query; }
        }

        [Browsable(false)]
        public virtual DBTable Table
        {
            get { return Query?.Table; }
            set { }
        }

        [Browsable(false)]
        public virtual DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        [Browsable(false)]
        public virtual DBSystem DBSystem
        {
            get { return Schema?.System ?? DBSystem.Default; }
        }

        public virtual string Format(IDbCommand command = null)
        {
            return text;
        }

        public virtual object GetValue(DBItem row = null)
        {
            return text;
        }

        public virtual void Dispose()
        {
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            Container?.OnPropertyChanged(this, propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public int CompareTo(object obj)
        {
            return order.CompareTo(((QItem)obj).order);
        }

        public override string ToString()
        {
            return Format();
        }
    }
}
