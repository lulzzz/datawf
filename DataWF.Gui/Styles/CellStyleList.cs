﻿using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class CellStyleList : SelectableList<CellStyle>, INamedList
    {
        public CellStyleList()
        {
            Indexes.Add(new Invoker<CellStyle, string>(nameof(CellStyle.Name), (item) => item.Name));
        }

        public void GenerateDefault()
        {
            var defaultFont = Font.SystemSansSerifFont;
            if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                defaultFont = defaultFont.WithSize(defaultFont.Size * 0.9);

            AddRange(new CellStyle[]{
                new CellStyle()
                {
                    Name = "List",
                    Font = defaultFont,
                    Round = 0,
                    BaseColor = Colors.LightGray
                },
                new CellStyle()
                {
                    Name = "Window",
                    Font = defaultFont,
                    Round = 0,
                    BaseColor = Colors.LightGray.WithIncreasedLight(-0.1)
                },
                new CellStyle()
                {
                    Name = "Row",
                    Font = defaultFont,
                    Round = 0,
                    BaseColor = Colors.LightGray
                },
                new CellStyle()
                {
                    Name = "ChangeRow",
                    BaseColor = Colors.LightGray
                },
                new CellStyle()
                {
                    Name = "MessageRow",
                    Alternate = false
                },
                new CellStyle()
                {
                    Name = "Node",
                    Alternate = false,
                    Font = defaultFont,
                    Round = 0,
                    BaseColor = Colors.White
                },
                new CellStyle()
                {
                    Name = "Cell",
                    Font = defaultFont,
                    LineWidth = 0,
                    BaseColor = Colors.WhiteSmoke
                },
                new CellStyle()
                {
                    Name = "Value",
                    Font = defaultFont,
                    BaseColor = Colors.White
                },
                new CellStyle()
                {
                    Name = "CellCenter",
                    BaseColor = Colors.White,
                    Alignment = Alignment.Center
                },
                new CellStyle()
                {
                    Name = "CellFar",
                    BaseColor = Colors.White,
                    Alignment = Alignment.End
                },
                new CellStyle()
                {
                    Name = "Column",
                    Font = defaultFont.WithWeight(FontWeight.Semibold),
                    BaseColor = Colors.Silver
                },
                new CellStyle()
                {
                    Name = "Group",
                    Font = defaultFont.WithSize(defaultFont.Size+1).WithWeight(FontWeight.Semibold),
                    Round = 5,
                    BaseColor = Colors.Gray.WithIncreasedLight(0.3).Invert()
                },
                new CellStyle()
                {
                    Name = "Red",
                    Font = defaultFont,
                    Round = 0,
                    Invert = false,
                    BaseColor = Colors.Red
                },
                new CellStyle()
                {
                    Name = "Header",
                    Alternate = false,
                    Alignment = Alignment.End,
                    BaseColor = Colors.Silver
                },
                new CellStyle()
                {
                    Name = "Field",
                    Font = defaultFont,
                    Alternate = false,
                    BaseColor = Colors.White
                },
                new CellStyle()
                {
                    Name = "Collect",
                    Font = defaultFont,
                    BaseColor = Colors.LightGray
                },
                new CellStyle()
                {
                    Name = "DocumentDock",
                    Round = 4,
                    Font = defaultFont.WithSize(10),
                    Invert = false,
                    BaseColor = Colors.LightBlue
                },
                new CellStyle()
                {
                    Name = "Page",
                    Font = defaultFont.WithWeight(FontWeight.Semibold),
                    BaseColor = Colors.Gray.WithIncreasedLight(0.3)
                },
                new CellStyle()
                {
                    Name = "PageClose",
                    Font = defaultFont,
                    Round = 3,
                    BaseColor = Color.FromBytes(150, 150, 150)
                },
                new CellStyle()
                {
                    Name = "GroupBoxHeader",
                    Round = 5,
                    Font = defaultFont.WithSize(defaultFont.Size).WithWeight(FontWeight.Bold),
                    BaseColor = Colors.LightGray
                },
                new CellStyle()
                {
                    Name = "GroupBox",
                    Round = 4,
                    BaseColor = Colors.Gray
                },
                new CellStyle()
                {
                    Name = "Logs",
                    BaseColor = Colors.DarkBlue
                },
                new CellStyle()
                {
                    Name = "Notify",
                    Font = defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Bold)
                },
                new CellStyle()
                {
                    Name = "DropDown",
                    Round = 4,
                    BackBrush = new CellStyleBrush() { ColorHover = Colors.DarkGray },
                    BorderBrush = new CellStyleBrush() { Color = Colors.SlateGray, ColorHover = Colors.DarkGray }
                },
                new CellStyle()
                {
                    Name = "Glyph",
                    FontBrush = new CellStyleBrush() { ColorHover = Colors.LightGray }
                },
                new CellStyle()
                {
                    Name = "Selection",
                    BackBrush = new CellStyleBrush() { Color = Colors.LightSkyBlue.WithAlpha(0.5) },
                    BorderBrush = new CellStyleBrush() { Color = Colors.SkyBlue.WithAlpha(0.5) }
                },
                new CellStyle()
                {
                    Name = "Tool",
                    Font = Font.SystemFont,
                    LineWidth = 1.5,
                    Round = 3,
                    BaseColor = Colors.LightGray
                }
            });

            this["Row"].BackBrush.Color = this["Row"].BaseColor;
            this["Column"].BackBrush.Color = this["Column"].BaseColor;
            this["Column"].BackBrush.Type = CellStyleBrushType.Gradient;
            this["GroupBoxHeader"].BackBrush.Color = this["GroupBoxHeader"].BaseColor;
            this["GroupBoxHeader"].BackBrush.Type = CellStyleBrushType.Gradient;
        }

        public CellStyle this[string param]
        {
            get { return SelectOne(nameof(CellStyle.Name), param); }
            set
            {
                var exists = this[param];
                if (exists != value)
                {
                    Remove(exists);
                    Add(value);
                }
            }
        }

        public override int AddInternal(CellStyle item)
        {
            if (item == null)
                throw new ArgumentException();
            var exist = this[item.Name];
            if (exist != item)
            {
                if (exist != null)
                    item.Name += "Clone";
                return base.AddInternal(item);
            }
            return -1;
        }

        public bool Contains(string name)
        {
            return this[name] != null;
        }

        public bool Remove(string name)
        {
            return Remove(this[name]);
        }

        public override void Dispose()
        {
            foreach (var item in items)
                item.Dispose();
            base.Dispose();
        }

        public INamed Get(string name)
        {
            return this[name];
        }

        public void Set(INamed value)
        {
            if (value != null)
            {
                this[value.Name] = (CellStyle)value;
            }
        }
    }


}
