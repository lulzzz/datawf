﻿using DataWF.Common;
using System;
using System.IO;
using Xwt;

namespace DataWF.Gui
{
    public class LogExplorer : VPanel, IDockContent
    {
        private LogList list;
        private Toolsbar tools;
        private ToolItem toolLoad;
        private ToolItem toolSave;

        public LogExplorer()
        {
            list = new LogList()
            {
                AllowEditColumn = true,
                EditMode = EditModes.ByClick,
                GenerateToString = false,
                Grouping = false,
                Name = "list",
                ReadOnly = true,
                ListSource = Helper.Logs
            };

            toolLoad = new ToolItem(OnToolLoadClick) { Name = "Load" };
            toolSave = new ToolItem(OnToolSaveClick) { Name = "Save" };
            tools = new Toolsbar(toolLoad, toolSave);
            Name = "LogEditor";

            PackStart(tools, false, false);
            PackStart(list, true, true);

            Localize();
            //System.Drawing.SystemIcons.
        }

        public void Add(string source, string message, string description, StatusType type)
        {
            Helper.Logs.Add(new StateInfo
            {
                Date = DateTime.Now,
                Module = source,
                Message = message,
                Description = description,
                Type = type
            });
        }

        public LogList List
        {
            get { return list; }
        }

        #region IDocContent implementation
        public DockType DockType
        {
            get { return DockType.Bottom; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        #endregion

        #region ILocalizable implementation
        public override void Localize()
        {
            GuiService.Localize(toolLoad, "LogExplorer", "Load", GlyphType.FolderOpen);
            GuiService.Localize(toolSave, "LogExplorer", "Save", GlyphType.SaveAlias);
            GuiService.Localize(this, "LogExplorer", "Logs", GlyphType.InfoCircle);

            list.Localize();
        }
        #endregion

        private void OnToolLoadClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.Run(ParentWindow))
            {
                StateInfoList newList = null;
                using (var fileStream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    Stream stream;
                    if (Helper.IsGZip(fileStream))
                        stream = Helper.GetUnGZipStrem(fileStream);
                    else
                        stream = fileStream;
                    newList = Serialization.Deserialize(stream) as StateInfoList;
                    stream.Close();
                }
                var form = new ToolWindow();
                form.Label.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                form.Target = new LogList() { ListSource = newList };
                form.Mode = ToolShowMode.Dialog;
                form.Show(this, new Point(0, 0));
            }
        }

        private void OnToolSaveClick(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            if (dialog.Run(ParentWindow))
            {
                using (Stream f = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write))
                    Serialization.Serialize(Helper.Logs, f);
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }
    }
}
