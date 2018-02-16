﻿using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    public class PMessage : LayoutDBTable
    {
        //private static PCellStyle StyleOut;
        //private static PCellStyle StyleIn;

        public PMessage()
            : base()
        {
            GenerateColumns = false;
            GenerateToString = false;

            var style = GuiEnvironment.StylesInfo["MessageRow"];


            //var style = _listInfo.StyleCell.Clone();
            listInfo.StyleRow = style;

            listInfo.Indent = 8;
            listInfo.ColumnsVisible = false;
            listInfo.HotTrackingCell = false;

            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.Date), Width = 120, Row = 0, Col = 0, Editable = false });
            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.User), Width = 120, Row = 0, Col = 0, Editable = false, FillWidth = true });
            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.Data), Width = 100, Row = 1, Col = 0, FillWidth = true });

            listInfo.Sorters.Add(new LayoutSort() { ColumnName = nameof(Message.Date), IsGroup = true });
            listInfo.HeaderWidth = 20;

            //HighLight = false;
            ListInfo.CalcHeigh = true;
        }

        //protected override PCellStyle OnGetCellStyle(object listItem, object value, IPCell col)
        //{
        //    if (StyleOut == null)
        //    {
        //        StyleOut = new PCellStyle();
        //        StyleOut.Alternate = false;
        //        StyleOut.BorderBrush.Color = Color.FromArgb(200, Color.Green);
        //        StyleOut.BorderBrush.SColor = Color.FromArgb(255, Color.Green);
        //        StyleOut.BackBrush.Color = Color.FromArgb(40, Color.Green);
        //        StyleOut.BackBrush.SColor = Color.FromArgb(120, Color.Green);

        //        StyleIn = new PCellStyle();
        //        StyleIn.Alternate = false; 
        //        StyleIn.BorderBrush.Color = Color.FromArgb(200, Color.Orange);
        //        StyleIn.BorderBrush.SColor = Color.FromArgb(255, Color.Orange);
        //        StyleIn.BackBrush.Color = Color.FromArgb(40, Color.Orange);
        //        StyleIn.BackBrush.SColor = Color.FromArgb(120, Color.Orange);
        //    }
        //    PCellStyle pcs = base.OnGetCellStyle(listItem, value, col);
        //    if (col == null && listItem is Message)
        //    {
        //        if (((Message)listItem).User == FlowEnvir.Personal.User)
        //            pcs = StyleOut;
        //        else
        //            pcs = StyleIn;
        //    }
        //    return pcs;
        //}

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            var chatItem = e.Item as Message;
            var style = OnGetCellStyle(chatItem, null, null);
            e.Context.DrawGlyph(style, e.Bound, chatItem.User == User.CurrentUser ? GlyphType.SignOut : GlyphType.SignIn);
            if (chatItem.DocumentId != null)
            {
                var r = new Rectangle(e.Bound.X, e.Bound.Y + 20 * listInfo.Scale, e.Bound.Width, 20 * listInfo.Scale);
                e.Context.DrawGlyph(style, r, GlyphType.Paperclip);
            }
            //base.OnPaintHeader(context, index, dataSource, bound, state);
        }

        protected override void OnListChangedApp(object arg)
        {
            base.OnListChangedApp(arg);
            if (listSource.Count > 0)
                SelectedItem = listSource[listSource.Count - 1];
        }
    }
}