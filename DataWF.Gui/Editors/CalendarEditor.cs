﻿using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.Gui
{
    public class CalendarList : LayoutList
    {
        private LayoutColumn numberColumn;

        public CalendarList()
        {
            GenerateColumns = false;
            GenerateToString = false;
            ListInfo = new LayoutListInfo(numberColumn = new LayoutColumn() { Name = "Number", Width = 50, Height = 50, StyleName = "Calendar" })
            {
                Indent = 4,
                StyleRowName = "Node",
                GridCol = 7,
                //ColumnsVisible = false,
                HeaderVisible = false
            };
        }


        public override void RefreshBounds(bool group)
        {
            if (Parent != null)
            {
                numberColumn.Width = ((canvas.Size.Width - ((ListInfo.Indent + 1) * 7)) / 7D);
                numberColumn.Height = ((canvas.Size.Height - ((ListInfo.Indent + 1) * 7)) / 7D);
            }
            base.RefreshBounds(group);
        }

        protected override void OnDrawColumn(LayoutListDrawArgs e)
        {
            var format = (DayOfWeek)e.GridIndex;
            var textBound = new Rectangle(new Point(e.Bound.Left + 3, e.Bound.Top + (e.Bound.Height - 20) / 2D),
                                 e.Bound.Size - new Size(6, 6));
            e.Context.DrawCell(listInfo.StyleColumn, format.ToString(), e.Bound, textBound, CellDisplayState.Default);
        }
    }

    public class CalendarEditor : VPanel
    {
        private Month month;
        private ToolLabel lable;
        private LayoutList list;
        private DateTime value;

        public CalendarEditor()
        {
            month = new Month();

            var bar = new Toolsbar(
                lable = new ToolLabel()
                {
                    Font = Font.WithScaledSize(1.5).WithWeight(Xwt.Drawing.FontWeight.Bold)
                },
                new ToolSeparator { FillWidth = true },
                new ToolItem((s, e) => Date = Date.AddMonths(-1)) { Name = "Prev Month", Glyph = Common.GlyphType.ChevronUp },
                new ToolItem((s, e) => Date = Date.AddMonths(1)) { Name = "Next Month", Glyph = Common.GlyphType.ChevronDown }
                );

            list = new CalendarList
            {
                ListSource = month.Days
            };
            list.SelectionChanged += ListSelectionChanged;
            PackStart(bar, false, false);
            PackStart(list, true, true);
            Date = DateTime.Today;
        }

        private void ListSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                var day = (Day)list.SelectedItem;
                if (day.Date != value.Date)
                {
                    value = day.Date;
                    OnSelectionChanged(EventArgs.Empty);
                }

                lable.Text = day.Date.ToString("MMMM yyyy");
            }
        }

        private void OnSelectionChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        public DateTime Date
        {
            get { return value; }
            set
            {
                this.value = value;
                month.SetDate(value.Year, value.Month);
                list.SelectedItem = month.GetDay(value);
            }
        }

        public event EventHandler ValueChanged;
    }

    public class Month
    {
        public Day[] Days = new Day[42];

        public void SetDate(int year, int month)
        {
            DateTime date = new DateTime(year, month, 1);
            var dayofWeek = (int)date.DayOfWeek;
            if (dayofWeek == 0)
                dayofWeek = 7;
            int i = 0;
            int d = 0;
            for (; i < dayofWeek; i++)
            {
                Days[i] = new Day(date.AddDays(-(dayofWeek - i)));
            }
            for (; d < DateTime.DaysInMonth(year, month); d++, i++)
            {
                Days[i] = new Day(date.AddDays(d));
            }
            for (; i < Days.Length; i++, d++)
            {
                Days[i] = new Day(date.AddDays(d));
            }
        }

        public Day GetDay(DateTime date)
        {
            date = date.Date;
            return Days.FirstOrDefault(item => item.Date == date);
        }
    }

    public struct Day
    {
        public Day(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; set; }

        public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }

        public string Number { get => Date.ToString("dd"); }

    }
}
