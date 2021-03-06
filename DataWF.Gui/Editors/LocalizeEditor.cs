﻿using DataWF.Common;
using System;
using System.IO;
using Xwt;


namespace DataWF.Gui
{
    public class LocalizeEditor : ListExplorer, ILocalizable
    {
        private ToolItem toolLoadImages;
        private ToolItem toolSave;

        public LocalizeEditor()
        {
            toolLoadImages = new ToolItem(ToolLoadImagesClick)
            {
                DisplayStyle = ToolItemDisplayStyle.Text,
                Name = "toolLoadImages",
                Text = "Load Images"
            };

            toolSave = new ToolItem()
            {
                DisplayStyle = ToolItemDisplayStyle.Text,
                Name = "toolSave",
                Text = "Save"
            };

            Editor.Bar.Items.Add(new ToolSeparator());
            Editor.Bar.Items.Add(toolSave);
            Editor.Bar.Items.Add(toolLoadImages);
            Editor.ReadOnly = false;
            Editor.List.RetriveCellEditor += (object listItem, object value, ILayoutCell cell) =>
            {
                if (cell.GetEditor(listItem) == null)
                {
                    if (Equals(cell.Name, "ImageKey"))
                        return new CellEditorLocalizeImage();
                    else if (Equals(cell.Name, "Glyph"))
                        return new CellEditorGlyph();
                }
                return null;
            };

            DataSource = Locale.Instance;

            var images = new ListEditor();
            images.List.ListInfo.ColumnsVisible = false;
            images.List.ListInfo.HeaderVisible = false;
            images.DataSource = Locale.Instance.Images;

            Name = "LocalizeEditor";
        }

        public override void Localize()
        {
            base.Localize();
            var name = GetType().Name;
            GuiService.Localize(this, name, "Localize Editor");
            GuiService.Localize(toolLoadImages, name, "Load Images");
            GuiService.Localize(toolSave, name, "Save", GlyphType.SaveAlias);
        }

        private void ToolLoadImagesClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.Run(ParentWindow))
            {
                foreach (string name in dialog.FileNames)
                {
                    Locale.Instance.Images.Add(new LocaleImage
                    {
                        Data = File.ReadAllBytes(name),
                        FileName = name
                    });
                }
            }
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            Locale.Save();
        }
    }

}
