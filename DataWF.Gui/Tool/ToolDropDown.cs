﻿using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolItem
    {
        protected Menubar menu;
        private LayoutAlignType menuAlign = LayoutAlignType.Bottom;

        public ToolDropDown() : base()
        {
            Initialize();
        }

        public ToolDropDown(EventHandler click) : base(click)
        {
            Initialize();
        }

        public ToolDropDown(params ToolItem[] items) : this()
        {
            DropDownItems.AddRange(items);
        }

        private void Initialize()
        {
            GlyphWidget = new GlyphWidget { Visible = false, MinWidth = 16, MinHeight = 16, Glyph = GlyphType.CaretDown };
        }

        public GlyphWidget GlyphWidget
        {
            get { return (GlyphWidget)content; }
            set { Content = value;}
        }

        public event EventHandler DropDownOpened;

        public Menubar DropDown
        {
            get { return menu ?? (DropDown = new Menubar { Name = Name }); }
            set
            {
                if (menu != value)
                {
                    menu = value;
                    menu.OwnerItem = this;
                    GlyphWidget.Visible = true;
                }
            }
        }

        public ToolItem DropDownItems
        {
            get { return DropDown.Items; }
        }

        public bool HasDropDown
        {
            get { return menu?.Items.Count > 0; }
        }

        public LayoutAlignType MenuAlign { get => menuAlign; set => menuAlign = value; }

        public event EventHandler<ToolItemEventArgs> ItemClick
        {
            add { DropDownItems.Bar.ItemClick += value; }
            remove { DropDownItems.Bar.ItemClick -= value; }
        }

        public override void Localize()
        {
            base.Localize();
            if (menu != null)
            {
                if (string.IsNullOrEmpty(menu.Name))
                    menu.Name = Name;
                menu.Localize();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
        }

        protected virtual void OnDropDownOpened()
        {
            DropDownOpened?.Invoke(this, EventArgs.Empty);
        }

        public virtual void ShowMenu()
        {
            if (menu != null)
            {
                if (!menu.Visible)
                {
                    var point = MenuAlign == LayoutAlignType.Left
                        ? Bound.TopLeft
                        : MenuAlign == LayoutAlignType.Right
                        ? Bound.TopRight
                        : Bound.BottomLeft;
                    menu.Popup(Bar, point);
                    OnDropDownOpened();
                }
            }
        }

        public void HideMenu()
        {
            if (menu != null)
            {
                if (menu.Visible)
                {
                    menu.Hide();
                }
            }
        }
    }
}
