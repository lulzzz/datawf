﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.TestGui
{
    public class Files : VPanel
    {
        private LayoutList directoryTree = new LayoutList();
        private LayoutList directoryView = new LayoutList();
        private HPaned split = new HPaned();
        private HBox status = new HBox();
        private Label statusLablel = new Label();

        private Node current;
        private Queue<Node> actions = new Queue<Node>();
        private SelectableList<FileItem> files = new SelectableList<FileItem>();
        private EventWaitHandle flag = new EventWaitHandle(true, EventResetMode.ManualReset);

        public Files()
        {
            split.Name = "splitContainer1";

            directoryTree.AllowCellSize = true;
            directoryTree.Mode = LayoutListMode.Tree;
            directoryTree.Name = "dTree";
            directoryTree.Text = "Directory Tree";
            directoryTree.SelectionChanged += DTreeSelectionChanged;
            directoryTree.Nodes.ListChanged += NodesListChanged;

            directoryView.Mode = LayoutListMode.List;
            directoryView.Name = "flist";
            directoryView.Text = "Directory";
            directoryView.CellDoubleClick += FListCellDoubleClick;
            directoryView.ListSource = files;

            status.Name = "status";
            status.PackStart(statusLablel);

            split.Panel1.Content = directoryTree;
            split.Panel2.Content = directoryView;

            PackStart(split, true, true);
            PackStart(status, false, false);
            Text = "Files";

            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                try
                {
                    directoryTree.Nodes.Add(InitDrive(drive));
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }

            GuiService.Localize(this, "Files", "Files", GlyphType.FilesO);
        }

        private Node InitDrive(DriveInfo drive)
        {
            var node = InitDirectory(drive.RootDirectory);
            node["Drive"] = drive;
            node.Text = string.Format("{0} {1}", drive.Name, drive.VolumeLabel);
            if (drive.DriveType == DriveType.Fixed)
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.HddO);
            else if (drive.DriveType == DriveType.CDRom)
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.Desktop);
            else
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.FloppyO);
            CheckSubDirectory(node);
            return node;
        }

        private Node InitDirectory(DirectoryInfo directory)
        {
            Node node = directoryTree.Nodes.Find(directory.FullName);
            if (node == null)
                node = new Node()
                {
                    Name = directory.FullName,
                    Text = directory.Name,
                    Glyph = Locale.GetGlyph("Files", "Directory", GlyphType.Folder),
                    Tag = new FileItem() { Info = directory }
                };

            return node;
        }

        private void CheckSubDirectory(Node node)
        {
            if (!node.Check && !actions.Contains(node))
            {
                actions.Enqueue(node);
                ThreadPool.QueueUserWorkItem(o => CheckQueue());
            }
        }

        private void CheckQueue()
        {
            flag.WaitOne();
            flag.Reset();
            while (actions.Count > 0)
            {
                var check = actions.Dequeue();
                if (check != null)
                {
                    check.Check = true;
                    Application.Invoke(() =>
                    {
                        statusLablel.Text = string.Format("Check: {0}", check.Name);
                    });

                    var directory = (DirectoryInfo)((FileItem)check.Tag).Info;
                    try
                    {
                        var directories = directory.GetDirectories();
                        foreach (var item in directories)
                        {
                            if ((item.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden &&
                                                     (item.Attributes & FileAttributes.System) != FileAttributes.System)
                            {
                                Node snode = InitDirectory(item);
                                snode.Group = check;
                                directoryTree.Nodes.Add(snode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        check.Text += ex.Message;
                    }
                }

                Application.Invoke(() =>
                {
                    statusLablel.Text = string.Format("Nodes: {0} Queue: {1}",
                                                      directoryTree.Nodes.Count,
                                                     actions.Count);
                });
            }
            flag.Set();
        }

        private void LoadFolder(Node node)
        {
            ThreadPool.QueueUserWorkItem(p =>
            {
                files.Clear();
                foreach (var item in node.Childs)
                    files.Add((FileItem)item.Tag);

                var directory = (DirectoryInfo)((FileItem)node.Tag).Info;
                var dfiles = directory.GetFiles();
                foreach (var file in dfiles)
                    files.Add(new FileItem() { Info = file });

            });
            //fList.ListSource = files;
        }

        public Node Current
        {
            get { return current; }
            set
            {
                if (current != value)
                {
                    current = value;
                    if (current != null)
                    {
                        var drive = current.Group == null ? (DriveInfo)current["Drive"] : null;
                        var text = drive == null ? ((FileItem)current.Tag).Info.FullName : string.Format("{0} free {1} of {2}", ((FileItem)current.Tag).Info.FullName,
                                       Helper.LengthFormat(drive.TotalFreeSpace),
                                       Helper.LengthFormat(drive.TotalSize));
                        statusLablel.Text = text;
                        for (int i = 0; i < current.Childs.Count; i++)
                            if (!current.Childs[i].Check)
                                CheckSubDirectory(current.Childs[i]);

                        LoadFolder(current);
                    }
                }
            }
        }

        private void NodesListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                var ex = (ListPropertyChangedEventArgs)e;
                if (ex.Property == "Expand")
                {
                    var node = directoryTree.Nodes[e.NewIndex];
                    if (node["Drive"] == null)
                        node.Glyph = node.Expand ? Locale.GetGlyph("Files", "DirectoryOpen", GlyphType.FolderOpen) :
                            Locale.GetGlyph("Files", "Directory", GlyphType.Folder);
                }
            }
        }

        private void DTreeSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (e.Value is LayoutSelectionRow && e.Type != LayoutSelectionChange.Remove && e.Type != LayoutSelectionChange.Hover)
                Current = ((LayoutSelectionRow)e.Value).Item as Node;
        }

        private void FListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var item = (FileItem)e.HitTest.Item;
            if (item.Info is DirectoryInfo)
            {
                SelectNode(item);
            }
        }

        private void FListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && directoryView.SelectedItem != null)
            {
                var item = (FileItem)directoryView.SelectedItem;
                if (item.Info is DirectoryInfo)
                {
                    SelectNode(item);
                }
            }
            if (e.Key == Key.BackSpace && Current != null && Current.Group != null)
            {
                SelectNode((FileItem)Current.Group.Tag);
            }
            if (e.Key == Key.F && e.Modifiers == ModifierKeys.Control && directoryView.CurrentCell != null)
            {
                directoryView.AddFilter(directoryView.CurrentCell, true);
            }
        }

        private void SelectNode(FileItem item)
        {
            var node = InitDirectory((DirectoryInfo)item.Info);
            directoryTree.SelectedNode = node;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }
}