﻿using DataWF.Common;
/*
 * 
 QParamList.cs
 
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
using System.Data;

namespace DataWF.Data
{
    public class QComparer : QItem
    {
        private CompareTypes type = CompareTypes.Undefined;

        public QComparer(CompareTypes comp)
        {
            Type = comp;
        }

        public CompareTypes Type
        {
            get { return type; }
            set
            {
                if (type != value)
                {
                    type = value;
                    text = CompareType.Format(value);
                }
            }
        }

        public override string Format(IDbCommand command = null)
        {
            return " " + text + " ";
        }
    }
}

