﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPanel : Canvas, IEnumerable, IDockContainer, ILocalizable
    {
        private DockMapItem mapItem;
        private DockPage currentPage;
        private Menu context = new Menu();
        private GlyphMenuItem toolHide = new GlyphMenuItem();
        private LayoutAlignType pagesAlign = LayoutAlignType.Top;
        private DockPageBox pages = new DockPageBox();
        private VBox panel = new VBox();
        private LinkedList<DockPage> pagesHistory = new LinkedList<DockPage>();

        public event EventHandler<DockPageEventArgs> PageSelected;

        private Widget widget;

        public DockPanel()
            : base()
        {

            context.Items.Insert(0, toolHide);

            toolHide.Name = "toolHide";
            toolHide.Text = "Hide";

            pages.Name = "toolStrip";
            pages.Visible = true;
            pages.PageClick += PagesPageClick;
            pages.Items.ListChanged += PageListOnChange;

            panel.Visible = true;

            this.Name = "DocTabControl";

            this.Visible = true;
            AddChild(pages);
            AddChild(panel);

            //BackgroundColor = Colors.Gray;
        }

        public void Localize()
        {
            foreach (DockPage page in pages.Items)
            {
                var loc = page.Widget as ILocalizable;
                if (loc != null)
                    loc.Localize();
            }
        }

        public DockPage AddPage(Widget c)
        {
            var page = DockBox.CreatePage(c);
            Pages.Items.Add(page);
            return page;
        }

        private void PagesPageClick(object sender, DockPageEventArgs e)
        {
            SelectPage(e.Page);
        }

        private void PageListOnChange(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                if (mapItem != null && !mapItem.Visible)
                {
                    mapItem.Visible = true;
                    //Parent.ResumeLayout(true);
                }
                DockPage page = this.pages.Items[e.NewIndex];
                SelectPage(page);
            }
            else if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                DockPage page = this.pages.Items[e.NewIndex];
                if (CurrentWidget == page.Widget && page.Visible == false)
                {
                    RemovePage(page, false);
                }
            }
            else if (e.ListChangedType == ListChangedType.ItemDeleted)
            {
                if (e.NewIndex != -1)
                {
                    DockPage page = this.pages.Items[e.NewIndex];
                    RemovePage(page, true);
                }
                else
                {
                    if (this.pages.Items.Count > 0)
                    {
                        if (CurrentWidget == null)
                            SelectPage(this.pages.Items[0]);
                    }
                    else
                    {
                        if (mapItem != null && !mapItem.FillWidth)
                        {
                            mapItem.Visible = false;
                            foreach (object item in mapItem.Map.Items)
                            {
                                if (item is DockMapItem)
                                {
                                    var mapItem = (DockMapItem)item;
                                    if (mapItem.Panel.Pages.Items.Count == 0)
                                        mapItem.Visible = false;
                                }
                            }
                            if (Parent is DockBox)
                            {
                                ((DockBox)Parent).QueueForReallocate();
                            }
                        }
                    }
                }
            }
        }

        public void RemovePage(DockPage page, bool RemoveHistory)
        {
            if (page != null)
            {
                if (CurrentWidget == page.Widget)
                {
                    DockPage npage = null;
                    if (pagesHistory.Last != null)
                    {
                        var item = pagesHistory.Last;

                        while (item != null && (item.Value == page || !item.Value.Visible))
                            item = item.Previous;
                        if (item != null)
                            npage = item.Value;
                    }
                    SelectPage(npage);
                }

                if (RemoveHistory)
                    while (pagesHistory.Remove(page))
                    {
                    }
            }
        }

        public LayoutAlignType PagesAlign
        {
            get { return this.pagesAlign; }
            set
            {
                if (pagesAlign == value)
                    return;
                pagesAlign = value;
                if (pagesAlign == LayoutAlignType.Left ||
                    pagesAlign == LayoutAlignType.Right)
                    pages.Orientation = Orientation.Vertical;
                else
                    pages.Orientation = Orientation.Horizontal;
                //PerformLayout();
            }
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            double def = 25;
            var pagesRect = Rectangle.Zero;
            var widgetRect = Rectangle.Zero;
            if (pagesAlign == LayoutAlignType.Top)
            {
                if (pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;
                pagesRect = new Rectangle(0D, 0D, Size.Width, def);
                widgetRect = new Rectangle(0D, def, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;

                pagesRect = new Rectangle(0D, Size.Height - def, Size.Width, def);
                widgetRect = new Rectangle(0D, 0D, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(0D, 0D, def, Size.Height);
                widgetRect = new Rectangle(def, 0D, Size.Width - def, Size.Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(Size.Width - def, 0D, def, Size.Height);
                widgetRect = new Rectangle(0D, 0D, Size.Width - def, Size.Height);
            }
            SetChildBounds(pages, pagesRect);
            SetChildBounds(panel, widgetRect);
        }

        public DockPageBox Pages
        {
            get { return pages; }
        }

        public DockMapItem MapItem
        {
            get { return mapItem; }
            set { mapItem = value; }
        }

        public Widget CurrentWidget
        {
            get { return widget; }
            set
            {
                if (value == widget)
                    return;

                if (widget != null)
                {
                    panel.Remove(widget);
                    widget.Visible = false;
                }

                widget = value;

                if (widget != null)
                {
                    widget.Visible = true;
                    panel.PackStart(widget, true);
                }
            }
        }

        public int Count
        {
            get { return pages.Items.Count; }
        }

        public void ClearPages()
        {
            pages.Items.Clear();
        }

        public void SelectPageByControl(Widget control)
        {
            var page = GetPage(control);
            if (page != null)
            {
                SelectPage(page);
            }
        }

        public DockPage CurrentPage
        {
            get { return currentPage; }
        }

        public void SelectPage(DockPage page)
        {
            if (currentPage == page)
                return;

            currentPage = page;
            if (page != null)
            {
                pagesHistory.AddLast(page);
                page.Active = true;
                CurrentWidget = page.Widget;
            }
            else
            {
                CurrentWidget = null;
            }
            if (PageSelected != null)
                PageSelected(this, new DockPageEventArgs(page));
        }

        #region IDockContainer implementation

        public IDockContainer DockParent
        {
            get { return GuiService.GetDockParent(this); }
        }

        public bool Contains(Widget control)
        {
            foreach (DockPage t in pages.Items)
                if (t.Widget == control)
                    return true;
            return false;
        }

        public IEnumerable<Widget> GetControls()
        {
            foreach (DockPage t in pages.Items)
                yield return t.Widget;
        }

        public Widget Find(string name)
        {
            foreach (DockPage page in pages.Items)
                if (page.Widget.Name == name)
                    return page.Widget;
            return null;
        }

        public DockPage Put(Widget control)
        {
            return Put(control, DockType.Content);
        }

        public DockPage Put(Widget control, DockType type)
        {
            if (control is ILocalizable)
                ((ILocalizable)control).Localize();
            var page = DockBox.CreatePage(control);
            Put(page);
            return page;
        }

        public void Put(DockPage page)
        {
            pages.Items.Add(page);
        }

        public DockPage GetPage(Widget control)
        {
            foreach (DockPage t in pages.Items)
                if (t.Widget == control)
                {
                    return t;
                }
            return null;
        }

        public bool Delete(Widget control)
        {
            var tp = GetPage(control);
            if (tp != null)
            {
                tp.List.Remove(tp);
                return true;
            }
            return false;
        }

        public IEnumerable<IDockContainer> GetDocks()
        {
            foreach (DockPage t in pages.Items)
            {
                if (t.Widget is IDockContainer)
                    yield return (IDockContainer)t.Widget;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            pages.Dispose();
            base.Dispose(disposing);
        }

        public IEnumerator GetEnumerator()
        {
            return pages.Items.GetEnumerator();
        }
    }
}