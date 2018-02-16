﻿using DataWF.Common;

namespace DataWF.Gui
{
    public class ToolFieldEditor : ToolItem
    {

        public ToolFieldEditor() : base(new FieldEditor())
        {
            DisplayStyle = ToolItemDisplayStyle.None;
        }

        public FieldEditor Field
        {
            get { return content as FieldEditor; }
        }

        public double FieldWidth
        {
            get { return MinWidth; }
            set { MinWidth = value; }
        }

        public ILayoutCellEditor Editor
        {
            get { return Field.CellEditor; }
            set
            {
                Field.CellEditor = value;
                CheckSize();
            }
        }

        public object DataValue
        {
            get { return Field.DataValue; }
            set { Field.DataValue = value; }
        }
    }
}