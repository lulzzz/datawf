﻿using DataWF.Common;
using System;
using System.Xml.Serialization;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class CellStyle : IDisposable, INamed
    {
        protected Font font = null;
        protected CellStyleBrush back = null;
        protected CellStyleBrush border = null;
        protected CellStyleBrush text = null;
        private Color baseColor;
        private string fontName = "Tahoma, 9";

        public CellStyle()
        {
        }

        public Color BaseColor
        {
            get { return baseColor; }
            set
            {
                baseColor = value;
            }
        }

        public bool Alternate { get; set; } = true;

        public string Name { get; set; }

        public double Angle { get; set; }

        public string FontName
        {
            get { return fontName; }
            set
            {
                if (fontName != value)
                {
                    fontName = value;
                    font = null;
                }
            }
        }

        [XmlIgnore]
        public Font Font
        {
            get { return font ?? (font = Font.FromName(fontName)); }
            set
            {
                if (font != value)
                {
                    font = value;
                    fontName = font?.ToString();
                }
            }
        }

        public int Round { get; set; }

        public double LineWidth { get; set; } = 1;

        public CellStyleBrush BackBrush
        {
            get { return back ?? (back = new CellStyleBrush()); }
            set { back = value; }
        }

        public CellStyleBrush BorderBrush
        {
            get { return border ?? (border = new CellStyleBrush()); }
            set { border = value; }
        }

        public CellStyleBrush FontBrush
        {
            get
            {
                if (text == null)
                {
                    text = new CellStyleBrush()
                    {
                        Color = Colors.Black,
                        ColorSelect = Colors.Black,
                        ColorHover = Colors.Black,
                        ColorPress = Colors.Black,
                        ColorAlternate = Colors.Black,
                    };
                }
                return text;
            }
            set { text = value; }
        }

        public Alignment Alignment { get; set; }

        public CellStyle Clone()
        {
            CellStyle style = new CellStyle()
            {
                Name = Name,
                Alternate = Alternate,
                Alignment = Alignment,
                FontName = FontName,
                BaseColor = BaseColor,
                Round = Round
            };
            if (back != null)
                style.back = (CellStyleBrush)back.Clone();
            if (border != null)
                style.border = (CellStyleBrush)border.Clone();
            if (text != null)
                style.text = (CellStyleBrush)text.Clone();

            return style;
        }

        public void Dispose()
        {
            if (text != null)
                text.Dispose();
            if (border != null)
                border.Dispose();
            if (back != null)
                back.Dispose();
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }

    }
}