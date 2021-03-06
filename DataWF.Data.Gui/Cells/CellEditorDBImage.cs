﻿using DataWF.Gui;
using System;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    public class CellEditorDBImage : CellEditorImage
    {
        private DBColumn column;
        public CellEditorDBImage()
            : base()
        {
        }

        public DBColumn Column
        {
            get { return column; }
            set
            {
                column = value;
            }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (dataSource is DBItem && column != null)
            {
                DBItem row = (dataSource is DBItem) ? (DBItem)dataSource : value as DBItem;
                Image img = null;//row.GetCache(column) as Image;
                if (img == null)
                {
                    if (row[column] is byte[] bytes)
                    {
                        img = GuiService.ImageFromByte(bytes);
                        //row.SetCache(column, img);
                    }
                }
                return img;
            }
            else
                return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is Image)
            {
                return GuiService.ImageToByte((Image)value);
            }
            return base.ParseValue(value, dataSource, valueType);
        }
    }
}

