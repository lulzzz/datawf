﻿/*
 Account.cs
 
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
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceAttribute : Attribute
    {
        private string name;

        public ReferenceAttribute(string property, string name = null)
        {
            ColumnProperty = property;
            this.name = name;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string ColumnProperty { get; set; }
    }
}