﻿using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolSplit : ToolDropDown
    {
        public ToolSplit() : base()
        {
            CarretClick += OnClickSplit;
        }

        public ToolSplit(EventHandler click) : this()
        {
            ButtonClick += click;
        }

        public event EventHandler ButtonClick;

        protected override void OnClick(EventArgs e)
        {
            if (CarretState == CellDisplayState.Default)
                ButtonClick?.Invoke(this, e);
        }

        protected void OnClickSplit(object sender, EventArgs e)
        {
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
        }
    }
}
