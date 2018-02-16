﻿using DataWF.Gui;
using DataWF.Common;
using DataWF.Data;
using System;
using Xwt.Drawing;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Xwt;
using System.Linq;

namespace DataWF.Data.Gui
{
    [Module(true)]
    public class DataExplorer : VPanel, IDockContent, IGlyph
    {
        private ToolWindow ose = new ToolWindow();
        private ListExplorer listExplorer = new ListExplorer();
        private DataTree dataTree = new DataTree();

        private GlyphMenuItem toolCMainAdd = new GlyphMenuItem();
        private GlyphMenuItem toolCMainTools = new GlyphMenuItem();
        private GlyphMenuItem toolCMainRemove = new GlyphMenuItem();
        private GlyphMenuItem toolCMainCopy = new GlyphMenuItem();
        private GlyphMenuItem toolCMainProperty = new GlyphMenuItem();
        private GlyphMenuItem toolAddDB = new GlyphMenuItem();
        private GlyphMenuItem toolAddTableGroup = new GlyphMenuItem();
        private GlyphMenuItem toolAddTable = new GlyphMenuItem();
        private GlyphMenuItem toolAddColumnGroup = new GlyphMenuItem();
        private GlyphMenuItem toolAddColumn = new GlyphMenuItem();
        private GlyphMenuItem toolAddIndex = new GlyphMenuItem();
        private GlyphMenuItem toolAddConstraint = new GlyphMenuItem();
        private GlyphMenuItem toolAddForeign = new GlyphMenuItem();
        private GlyphMenuItem toolAddProcedure = new GlyphMenuItem();
        private GlyphMenuItem toolAddProcedureParam = new GlyphMenuItem();

        private GlyphMenuItem toolDBCheck = new GlyphMenuItem();
        private GlyphMenuItem toolDBRelation = new GlyphMenuItem();
        private GlyphMenuItem toolDBRefreshSchema = new GlyphMenuItem();
        private GlyphMenuItem toolDBPatchCreate = new GlyphMenuItem();
        private GlyphMenuItem toolDBPatchLoad = new GlyphMenuItem();
        private GlyphMenuItem toolDBExport = new GlyphMenuItem();
        private GlyphMenuItem toolDBGenerate = new GlyphMenuItem();
        private GlyphMenuItem toolTableRefresh = new GlyphMenuItem();
        private GlyphMenuItem toolTableReport = new GlyphMenuItem();
        private GlyphMenuItem toolTableExplorer = new GlyphMenuItem();
        private GlyphMenuItem toolExtractDDL = new GlyphMenuItem();
        private GlyphMenuItem toolSerialize = new GlyphMenuItem();
        private GlyphMenuItem toolDeSerialize = new GlyphMenuItem();
        private GlyphMenuItem toolRefresh = new GlyphMenuItem();
        private GlyphMenuItem toolLoadFile = new GlyphMenuItem();
        private ToolItem toolMainRemove = new ToolItem();
        private ToolItem toolMainCopy = new ToolItem();
        private ToolDropDown toolMainAdd = new ToolDropDown();
        private ToolDropDown toolMainTools = new ToolDropDown();
        private ToolSearchEntry toolMainSearch = new ToolSearchEntry();
        private Toolsbar barMain = new Toolsbar();
        private ToolItem toolChangesCommit = new ToolItem();
        private ToolItem toolChangesSkip = new ToolItem();
        private Toolsbar barChanges = new Toolsbar();
        private Menu contextMain = new Menu();
        private Menu contextAdd = new Menu();
        private Menu contextTools = new Menu();

        private VPaned container = new VPaned();
        private LayoutList changesView = new LayoutList();
        private SelectableList<DBSchemaChange> changes = new SelectableList<DBSchemaChange>();

        public DataExplorer()
            : base()
        {
            barMain.Add(toolMainAdd);
            barMain.Add(toolMainRemove);
            barMain.Add(toolMainCopy);
            barMain.Add(toolMainTools);
            barMain.Add(toolMainSearch);

            toolRefresh.Name = "toolRefresh";
            toolRefresh.ForeColor = Colors.DarkBlue;
            toolRefresh.Click += ToolMainRefreshOnClick;

            toolDBGenerate.Name = "toolDBGenerate";
            toolDBGenerate.Click += ToolDBGenerateClick;

            toolDBCheck.Name = "toolDBCheck";
            toolDBCheck.Click += ToolDBCheckClick;

            toolDBRefreshSchema.Name = "toolDBRefresh";
            toolDBRefreshSchema.Click += ToolDBRefreshClick;

            toolDBExport.Name = "toolDbExport";
            toolDBExport.Click += ToolDBExportClick;

            toolDBPatchCreate.Name = "toolDBPatchCreate";
            toolDBPatchCreate.Click += ToolDBPatchCreateClick;

            toolDBPatchLoad.Name = "toolDBPatchLoad";
            toolDBPatchLoad.Click += ToolDBPatchLoadClick;

            toolDBRelation.Name = "toolDBRelation";
            toolDBRelation.Click += ToolDBRelationClick;

            toolTableExplorer.Name = "toolTableExplorer";
            toolTableExplorer.Click += ToolTableExplorerOnClick;

            toolTableRefresh.Name = "toolTableRefresh";
            toolTableRefresh.Click += ToolTableRefreshOnClick;

            toolTableReport.Name = "toolTableReport";
            toolTableReport.Click += ToolTableReportOnClick;

            toolExtractDDL.Name = "toolExtractDDL";
            toolExtractDDL.Click += ToolExtractDDLOnClick;

            toolSerialize.Name = "toolSerialize";
            toolSerialize.Click += ToolSerializeOnClick;

            toolDeSerialize.Name = "toolDeSerialize";
            toolDeSerialize.Click += ToolDeSerializeOnClick;

            toolTableReport.Name = "toolAdd";
            toolTableReport.Click += ToolReportClick;

            toolAddDB.Name = "toolAddDB";
            toolAddDB.Click += ToolAddDBClick;

            toolAddTableGroup.Name = "toolAddTableGroup";
            toolAddTableGroup.Click += ToolAddTableGroupClick;

            toolAddTable.Name = "toolAddTable";
            toolAddTable.Click += ToolAddTableClick;

            toolAddColumnGroup.Name = "toolAddColumnGroup";
            toolAddColumnGroup.Click += ToolAddColumnGroupClick;

            toolAddColumn.Name = "toolAddColumn";
            toolAddColumn.Click += ToolAddColumnClick;

            toolAddIndex.Name = "toolAddIndex";
            toolAddIndex.Click += ToolAddIndexClick;

            toolAddConstraint.Name = "toolAddConstraint";
            toolAddConstraint.Click += ToolAddConstraintClick;

            toolAddForeign.Name = "toolAddForeign";
            toolAddForeign.Click += ToolAddForeignClick;

            toolAddProcedure.Name = "toolAddProcedure";

            toolAddProcedureParam.Name = "toolAddProcedureParameter";

            contextMain.Name = "contextMain";

            toolCMainAdd.Name = "toolCMainAdd";
            toolCMainAdd.ForeColor = Colors.DarkGreen;

            toolCMainTools.Name = "toolCMainTools";

            toolCMainRemove.Name = "toolCMainRemove";
            toolCMainRemove.ForeColor = Colors.DarkRed;
            toolCMainRemove.Click += ToolRemoveClick;

            toolCMainCopy.Name = "toolCMainCopy";
            toolCMainCopy.Click += ToolCopyClick;

            toolCMainProperty.Name = "toolCMainProperty";
            toolCMainProperty.Click += ToolPropertyClick;

            barMain.Name = "toolStrip1";

            toolMainRemove.Name = "toolMainRemove";
            toolMainRemove.ForeColor = Colors.DarkRed;
            toolMainRemove.Click += ToolRemoveClick;

            toolMainAdd.Name = "toolMainAdd";
            toolMainAdd.ForeColor = Colors.DarkGreen;
            toolMainAdd.DropDown = contextAdd;

            toolMainCopy.Name = "toolMainCopy";
            toolMainCopy.Click += ToolCopyClick;

            toolMainTools.Name = "toolMainTools";
            toolMainTools.DropDown = contextTools;

            barChanges.Name = "toolChanges";

            toolChangesCommit.Name = "toolChangesCommit";
            toolChangesCommit.Click += ToolChangesCommitOnClick;

            toolChangesSkip.Name = "toolChangesSkip";
            toolChangesSkip.Click += ToolChangesSkipOnClick;

            toolLoadFile.Name = "toolLoadFile";
            toolLoadFile.Click += ToolLoadFileClick;

            changesView.Name = "changesView";
            changesView.GenerateColumns = false;
            changesView.AutoToStringFill = true;
            changesView.CheckView = true;

            dataTree.Name = "dataTree";
            dataTree.DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table |
                DataTreeKeys.ColumnGroup | DataTreeKeys.Column |
                DataTreeKeys.Index | DataTreeKeys.Constraint | DataTreeKeys.Foreign;
            dataTree.CellMouseClick += DataTreeOnNodeMouseClick;
            dataTree.CellDoubleClick += DataTreeOnNodeMouseDoubleClick;
            dataTree.SelectionChanged += DataTreeOnAfterSelect;
            dataTree.Menu = contextMain;

            contextTools.Items.Add(toolRefresh);
            contextTools.Items.Add(toolDBCheck);
            contextTools.Items.Add(toolDBRefreshSchema);
            contextTools.Items.Add(toolDBRelation);
            contextTools.Items.Add(toolDBGenerate);
            contextTools.Items.Add(new SeparatorMenuItem());
            contextTools.Items.Add(toolDBExport);
            contextTools.Items.Add(toolDBPatchCreate);
            contextTools.Items.Add(toolDBPatchLoad);
            contextTools.Items.Add(new SeparatorMenuItem());
            contextTools.Items.Add(toolTableRefresh);
            contextTools.Items.Add(toolTableReport);
            contextTools.Items.Add(toolTableExplorer);
            contextTools.Items.Add(new SeparatorMenuItem());
            contextTools.Items.Add(toolExtractDDL);
            contextTools.Items.Add(toolSerialize);
            contextTools.Items.Add(toolDeSerialize);
            contextTools.Items.Add(toolLoadFile);

            contextAdd.Items.Add(toolAddDB);
            contextAdd.Items.Add(toolAddTableGroup);
            contextAdd.Items.Add(toolAddTable);
            contextAdd.Items.Add(toolAddColumnGroup);
            contextAdd.Items.Add(toolAddColumn);
            contextAdd.Items.Add(toolAddIndex);
            contextAdd.Items.Add(toolAddConstraint);
            contextAdd.Items.Add(toolAddForeign);
            contextAdd.Items.Add(toolAddProcedure);
            contextAdd.Items.Add(toolAddProcedureParam);

            contextMain.Items.Add(toolCMainAdd);
            contextMain.Items.Add(toolCMainCopy);
            contextMain.Items.Add(toolCMainRemove);
            contextMain.Items.Add(new SeparatorMenuItem());
            contextMain.Items.Add(toolCMainTools);
            contextMain.Items.Add(toolCMainProperty);

            //toolCMainAdd.DropDown = contextAdd;
            //toolCMainTools.DropDown = contextTools;

            barChanges.Items.AddRange(new ToolItem[] {
            toolChangesCommit,
            toolChangesSkip,
            });

            toolChangesCommit.DisplayStyle = ToolItemDisplayStyle.Text;
            toolChangesSkip.DisplayStyle = ToolItemDisplayStyle.Text;

            var panel1Box = new VPanel();
            panel1Box.PackStart(barMain, false, false);
            panel1Box.PackStart(dataTree, true, true);
            container.Panel1.Content = panel1Box;
            var panel2Box = new VPanel();
            panel2Box.PackStart(barChanges, false, false);
            panel2Box.PackStart(changesView, true, true);
            panel2Box.Visible = false;
            container.Panel2.Content = panel2Box;

            PackStart(container, true, true);
            Name = "DataExplorer";

            listExplorer.GetCellEditor += LayoutDBTable.InitCellEditor;
            ose.Target = listExplorer;
            ose.ButtonAcceptClick += AcceptOnActivated;

            changesView.ListSource = changes;
            changesView.ListInfo.ColumnsVisible = false;

            DBService.DBSchemaChanged += OnDBSchemaChanged;

            Localize();
        }

        public DBSchema CurrentSchema
        {
            get { return dataTree.CurrentSchema; }
        }
        private void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
        {
            DBSchemaItem item = sender as DBSchemaItem;
            if (item.Container == null)
                return;
            if (item is IDBTableContent)
            {
                var table = ((IDBTableContent)item).Table;
                if (table is IDBVirtualTable || table.Container == null)
                    return;
            }
            DBSchemaChange change = null;

            var list = changes.Select("Item", CompareType.Equal, item).ToList();

            if (list.Count > 0)
            {
                change = list[0];
                if (change.Change != e.Type)
                {
                    if (change.Change == DDLType.Create && e.Type == DDLType.Alter)
                        return;
                    //if (change.Change == DDLType.Create && e.Type == DDLType.Drop)
                    //{
                    //    changes.Remove(change);
                    //    return;
                    //}
                    change = null;
                }
            }

            if (change == null)
            {
                change = new DBSchemaChange() { Item = item, Change = e.Type };
                changes.Add(change);
            }
            Application.Invoke(() => container.Panel2.Content.Visible = true);
        }

        private void ToolDeSerializeOnClick(object sender, EventArgs e)
        {
            string file = null;
            var dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
            if (dialog.Run(ParentWindow))
            {
                file = dialog.FileName;
                var item = Serialization.Deserialize(file);
                if (item is DBTable)
                {
                    DBSchema schema = dataTree.SelectedNode.Tag as DBSchema;
                    if (schema == null && dataTree.SelectedNode.Tag is DBSchemaItem)
                        schema = ((DBSchemaItem)dataTree.SelectedNode.Tag).Schema;

                    if (schema.Tables.Contains(((DBTable)item).Name))
                        schema.Tables.Remove(((DBTable)item).Name);
                    schema.Tables.Add((DBTable)item);
                }
                else if (item is DBSchema)
                {
                    DBSchema schema = (DBSchema)item;
                    if (DBService.Schems.Contains(schema.Name))
                        schema.Name = schema.Name + "1";
                    DBService.Schems.Add((DBSchema)item);
                }
                else if (item is DBColumn)
                {
                    var table = dataTree.SelectedNode.Tag as DBTable;
                    if (table != null)
                        table.Columns.Add((DBColumn)item);
                }
                else if (item is SelectableList<DBSchemaItem>)
                {
                    var list = (SelectableList<DBSchemaItem>)item;
                    foreach (var i in list)
                    {
                        if (i is DBColumn && dataTree.SelectedNode.Tag is DBTable)
                            ((DBTable)dataTree.SelectedNode.Tag).Columns.Add((DBColumn)i);
                        else if (i is DBTable && dataTree.SelectedNode.Tag is DBSchema)
                            ((DBSchema)dataTree.SelectedNode.Tag).Tables.Add((DBTable)i);
                    }

                }
            }
            dialog.Dispose();
        }

        private void ToolSerializeOnClick(object sender, EventArgs e)
        {
            if (this.dataTree.SelectedNode != null)
            {
                string file = null;
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
                    if (dialog.Run(ParentWindow))
                    {
                        file = dialog.FileName;
                        if (this.dataTree.Selection.Count > 1)
                        {
                            var nodes = this.dataTree.Selection.GetItems<Node>();
                            var items = new SelectableList<DBSchemaItem>();
                            foreach (var node in nodes)
                                items.Add((DBSchemaItem)node.Tag);
                            Serialization.Serialize(items, file);
                        }
                        else
                            Serialization.Serialize(this.dataTree.SelectedNode.Tag, file);
                    }
                }
            }
        }

        private void ToolChangesSkipOnClick(object sender, EventArgs e)
        {
            changes.Clear();
            HideChanges();
        }

        private void ToolChangesCommitOnClick(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in changes)
            {
                string val = item.Generate();
                if (item.Check && !string.IsNullOrEmpty(val))
                {
                    sb.Append("-- ");
                    sb.AppendLine(item.ToString());
                    sb.AppendLine(val);
                    sb.AppendLine("go");
                    sb.AppendLine();
                }
                item.Item.OldName = null;
            }
            DataQuery query = new DataQuery();
            query.Query = sb.ToString();
            query.ShowDialog(this);

            changes.Clear();
            HideChanges();
        }

        public void HideChanges()
        {
            container.Panel2.Content.Hide();
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public void Localize()
        {
            GuiService.Localize(toolChangesCommit, Name, "Commit", GlyphType.Check);
            GuiService.Localize(toolChangesSkip, Name, "Skip", GlyphType.MinusSquare);

            GuiService.Localize(toolCMainAdd, Name, "Add");
            GuiService.Localize(toolCMainTools, Name, "Tools");
            GuiService.Localize(toolCMainRemove, Name, "Remove");
            GuiService.Localize(toolCMainCopy, Name, "Copy");
            GuiService.Localize(toolCMainProperty, Name, "Property");
            GuiService.Localize(toolDBGenerate, Name, "Generate DB");
            GuiService.Localize(toolLoadFile, Name, "Load Files", GlyphType.FilesO);

            GuiService.Localize(toolAddDB, typeof(DBSchema).FullName, "DBSchema", GlyphType.Database);
            GuiService.Localize(toolAddTableGroup, typeof(DBTableGroup).FullName, "DBTableGroup", GlyphType.FolderOTable);
            GuiService.Localize(toolAddTable, typeof(DBTable).FullName, "DBTable", GlyphType.Table);
            GuiService.Localize(toolAddColumnGroup, typeof(DBColumnGroup).FullName, "DBColumnGroup", GlyphType.FolderOColumn);
            GuiService.Localize(toolAddColumn, typeof(DBColumn).FullName, "DBColumn", GlyphType.Columns);
            GuiService.Localize(toolAddIndex, typeof(DBIndex).FullName, "DBIndex", GlyphType.Anchor);
            GuiService.Localize(toolAddConstraint, typeof(DBConstraint).FullName, "DBConstraint", GlyphType.Check);
            GuiService.Localize(toolAddForeign, typeof(DBForeignKey).FullName, "DBForeign", GlyphType.Link);
            GuiService.Localize(toolAddProcedure, typeof(DBProcedure).FullName, "DBProcedure", GlyphType.GearAlias);
            GuiService.Localize(toolAddProcedureParam, typeof(DBProcParameter).FullName, "DBProcedureParam", GlyphType.Columns);


            GuiService.Localize(toolRefresh, Name, "Refresh", GlyphType.Refresh);
            GuiService.Localize(toolDBRefreshSchema, Name, "Initialize DB");
            GuiService.Localize(toolDBExport, Name, "Export DB");
            GuiService.Localize(toolDBPatchCreate, Name, "Patch Create");
            GuiService.Localize(toolDBPatchLoad, Name, "Patch Load");
            GuiService.Localize(toolDBCheck, Name, "Check connection");
            GuiService.Localize(toolDBRelation, Name, "Refresh relations");
            GuiService.Localize(toolTableExplorer, Name, "Table Explorer");
            GuiService.Localize(toolTableReport, Name, "Table Report");
            GuiService.Localize(toolTableRefresh, Name, "Table Initialize");
            GuiService.Localize(toolExtractDDL, Name, "Extract DDL");
            GuiService.Localize(toolSerialize, Name, "Serialize");
            GuiService.Localize(toolDeSerialize, Name, "Deserialize");

            GuiService.Localize(toolMainRemove, Name, "Remove", GlyphType.MinusCircle);
            GuiService.Localize(toolMainAdd, Name, "Add", GlyphType.PlusCircle);
            GuiService.Localize(toolMainTools, Name, "Tools", GlyphType.Wrench);
            GuiService.Localize(toolMainCopy, Name, "Copy", GlyphType.CopyAlias);

            GuiService.Localize(this, Name, "Data Explorer", GlyphType.Database);

            this.dataTree.Localize();
        }

        private void ShowNewItem(object item)
        {
            listExplorer.Value = item;
            ose.Mode = ToolShowMode.Dialog;
            ose.Show(this, new Point(0, 0));
        }

        private class PatchParam
        {
            public ExportMode Mode { get; set; }
            public DateTime Stamp { get; set; }
            public string File { get; set; }
        }

        private void ToolDBPatchLoadClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Patch(zip)", "*.zip"));
                if (dialog.Run(ParentWindow))
                {
                    DBExport export = null;
                    string dbfile = null;
                    var path = Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(dialog.FileName));
                    if (Helper.ReadZip(dialog.FileName, path))
                    {
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            if (file.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                                export = Serialization.Deserialize(file) as DBExport;
                            if (file.EndsWith("sdb", StringComparison.OrdinalIgnoreCase))
                                dbfile = file;
                        }
                        export.Source.Connection.Host = dbfile;
                        var editor = new DataExport();
                        editor.Export = export;
                        editor.Show(this);
                    }
                }
            }
        }

        private void ToolDBPatchCreateClick(object sender, EventArgs e)
        {
            var export = new DBExport()
            {
                Mode = ExportMode.Patch,
                Stamp = DateTime.Today.AddDays(-7),
                Source = DBService.DefaultSchema,
                Target = new DBSchema()
                {
                    Name = "patch",
                    Connection = new DBConnection()
                    {
                        System = DBSystem.SQLite,
                        Host = "dataPatch" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".sdb"
                    }
                }
            };
            var editor = new DataExport();
            editor.Export = export;
            editor.ExportComplete += (oo, ee) =>
            {
                editor.Patch();
                editor.ParentWindow.Close();
            };
            editor.Show(this);
            editor.Initialise();
        }

        private void AcceptOnActivated(object sender, EventArgs e)
        {
            if (listExplorer.Value is DBSchema)
                DBService.Schems.Add((DBSchema)listExplorer.Value);
            else if (listExplorer.Value is DBTable)
                ((DBTable)listExplorer.Value).Schema.Tables.Add((DBTable)listExplorer.Value);
            else if (listExplorer.Value is DBTableGroup)
                ((DBTableGroup)listExplorer.Value).Schema.TableGroups.Add((DBTableGroup)listExplorer.Value);
            else if (listExplorer.Value is DBColumnGroup)
                ((DBColumnGroup)listExplorer.Value).Table.ColumnGroups.Add((DBColumnGroup)listExplorer.Value);
            else if (listExplorer.Value is DBColumn)
                ((DBColumn)listExplorer.Value).Table.Columns.Add((DBColumn)listExplorer.Value);
            else if (listExplorer.Value is DBIndex)
                ((DBIndex)listExplorer.Value).Schema.Indexes.Add((DBIndex)listExplorer.Value);
            else if (listExplorer.Value is DBForeignKey)
                ((DBForeignKey)listExplorer.Value).Schema.Foreigns.Add((DBForeignKey)listExplorer.Value);
            else if (listExplorer.Value is DBConstraint)
                ((DBConstraint)listExplorer.Value).Schema.Constraints.Add((DBConstraint)listExplorer.Value);
        }

        private void ToolDBGenerateClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode != null && dataTree.SelectedNode.Tag is DBSchema)
            {
                var list = new LayoutList();
                list.ListSource = AppDomain.CurrentDomain.GetAssemblies();

                var window = new ToolWindow();
                window.Label.Text = "Select Assembly";
                window.Target = list;
                window.Show(this, Point.Zero);
                window.ButtonAcceptClick += (s, a) =>
                  {
                      if (list.SelectedItem is System.Reflection.Assembly)
                      {
                          var schems = DBService.Generate((System.Reflection.Assembly)list.SelectedItem);
                          if (schems.Count > 0)
                          {
                              var text = new StringBuilder();
                              foreach (var schema in schems)
                              {
                                  text.AppendLine(schema.FormatSql(DDLType.Create));
                                  text.AppendLine("go");
                                  text.AppendLine(schema.FormatSchema());
                              }
                              var query = new DataQuery();
                              query.Query = text.ToString();

                              GuiService.Main.DockPanel.Put(query);
                          }
                      }
                  };
            }
            if (dataTree.SelectedNode == null)
            {
                //DataQuery dq = new DataQuery();
                //dq.Query = dwf.flow.FlowEnvir.Config.Generate(dwf.flow.FlowEnvir.Config.Schema);
                //GuiService.Main.DockPanel.Put(dq);
            }
        }

        private void ToolDBCheckClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode == null)
                return;
            DBSchema schema = dataTree.SelectedNode.Tag as DBSchema;
            if (schema != null)
            {
                try
                {
                    schema.Connection.CheckConnection();
                    string message = Common.Locale.Get("DataExplorer", "Connection Test Complete!");
                    MessageDialog.ShowMessage(ParentWindow, message, "DB Manager");
                    GuiService.Main.SetStatus(new StateInfo("DB Manager", message, null, StatusType.Warning, schema));
                }
                catch (Exception exception)
                {
                    string message = Common.Locale.Get("DataExplorer", "Connection Test Fail!");
                    MessageDialog.ShowMessage(ParentWindow, message, "DB Manager");
                    GuiService.Main.SetStatus(new StateInfo("DB Manager", message + "\n" + exception.Message, null, StatusType.Error, schema));
                }
            }
        }

        private void ToolTableRefreshOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBTable)
            {
                //DBSystem.LoadColumns(.SelectedObject);
            }
        }

        private void ToolTableReportOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBTable)
            {
            }
        }

        private void ToolExtractDDLOnClick(object sender, EventArgs e)
        {
            var item = dataTree.SelectedObject as DBSchemaItem;
            if (item != null)
            {
                DataQuery query = new DataQuery();
                query.Query = item.FormatSql(DDLType.Create);
                GuiService.Main.DockPanel.Put(query);
            }
        }

        private void EditTableData(DBTable table)
        {
            TableExplorer cont = null;
            if (cont == null)
                cont = new TableExplorer();
            cont.Initialize(table, null, null, TableFormMode.Table, false);
            GuiService.Main.DockPanel.Put(cont, DockType.Content);
        }

        private void AddSchema()
        {
            var connection = new DBConnection();
            connection.Name = "nc" + DBService.Connections.Count;
            DBService.Connections.Add(connection);

            var schema = new DBSchema();
            schema.Connection = connection;
            schema.Name = "new";
            ShowNewItem(schema);
        }

        public void AddTableGroup(DBSchema schema, DBTableGroup parent)
        {
            DBTableGroup tg = new DBTableGroup();
            tg.Name = "NewTableGroup";
            tg.Group = parent;
            tg.Schema = schema;
            ShowNewItem(tg);
        }

        public void AddTable(DBSchema schema, DBTableGroup gp)
        {
            DBTable table = new DBTable<DBItem>();
            table.Name = "NewTable";
            table.Group = gp;
            table.Schema = schema;
            table.Columns.Add(new DBColumn()
            {
                Name = "unid",
                Keys = DBColumnKeys.Primary,
                DBDataType = DBDataType.Decimal,
                Size = 28
            });
            table.Columns.Add(new DBColumn()
            {
                Name = "datec",
                Keys = DBColumnKeys.Date,
                DBDataType = DBDataType.DateTime
            });
            table.Columns.Add(new DBColumn()
            {
                Name = "dateu",
                Keys = DBColumnKeys.Stamp,
                DBDataType = DBDataType.DateTime
            });
            table.Columns.Add(new DBColumn()
            {
                Name = "stateid",
                Keys = DBColumnKeys.State,
                DBDataType = DBDataType.Decimal,
                Size = 28
            });
            table.Columns.Add(new DBColumn()
            {
                Name = "access",
                Keys = DBColumnKeys.Access,
                DBDataType = DBDataType.Blob,
                Size = 2000
            });

            ShowNewItem(table);
        }

        public void AddColumnGroup(DBTable table)
        {
            var item = new DBColumnGroup();
            item.Table = table;
            item.Name = "NewColumnGroup";

            ShowNewItem(item);
        }

        public void AddColumn(DBTable table, DBColumnGroup gp)
        {
            var item = new DBColumn();
            item.Group = gp;
            item.Table = table;
            item.Name = "NewColumn";

            ShowNewItem(item);
        }

        public void AddIndex(DBTable table, DBColumn column)
        {
            var item = new DBIndex();
            item.Table = table;
            item.Name = item.Table.Name + "NewIndex";
            if (column != null)
                item.Columns.Add(column);

            ShowNewItem(item);
        }

        public void AddConstraint(DBTable table, DBColumn column)
        {
            var item = new DBConstraint();
            item.Table = table;
            if (column != null)
                item.Columns.Add(column);
            item.GenerateName();

            ShowNewItem(item);
        }

        public void AddForeign(DBTable table, DBColumn column)
        {
            var item = new DBForeignKey();
            item.Table = table;
            if (column != null)
                item.Columns.Add(column);
            item.GenerateName();

            ShowNewItem(item);
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        protected void ShowItem(Widget editor)
        {
            if (GuiService.Main != null)
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            else
            {
                editor.Show(this);
            }
        }

        private void ToolRemoveClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode == null)
                return;
            var text = Common.Locale.Get(base.Name, "Remove select items?");
            if (MessageDialog.AskQuestion("Confirmation", text, Command.No, Command.Yes) == Command.Yes)
            {
                var items = dataTree.Selection.GetItems<Node>();
                foreach (Node node in items)
                {
                    var obj = node.Tag as DBSchemaItem;
                    if (obj != null)
                        obj.Container.Remove(obj);
                }
            }
        }

        private void ToolAddDBClick(object sender, EventArgs e)
        {
            AddSchema();
        }

        private void ToolDbEditClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBSchema)
            {
                var editor = new ListExplorer();
                editor.DataSource = dataTree.SelectedObject;
                editor.ShowDialog(this);
            }
        }

        private void ToolDBRefreshClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBSchema)
            {
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        var schema = (DBSchema)dataTree.SelectedObject;
                        schema.GetTablesInfo();
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                });
            }
        }


        private void ToolReportClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBTable)
            {
                ProjectHandler ph = new ProjectHandler();
                ph.Project = new QQuery();
                ((QQuery)ph.Project).Table = (DBTable)dataTree.SelectedObject;
                GuiService.Main.CurrentProject = ph;
            }
        }

        private void ToolAddTableGroupClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBSchema)
                AddTableGroup((DBSchema)dataTree.SelectedObject, null);
            else if (dataTree.SelectedObject is DBTableGroup)
                AddTableGroup(((DBTableGroup)dataTree.SelectedObject).Schema, (DBTableGroup)dataTree.SelectedObject);
        }

        private void ToolAddTableClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBSchema)
                AddTable((DBSchema)dataTree.SelectedObject, null);
            else if (dataTree.SelectedObject is DBTableGroup)
                AddTable(((DBTableGroup)dataTree.SelectedObject).Schema, (DBTableGroup)dataTree.SelectedObject);
            else if (dataTree.SelectedObject is DBTable)
                AddTable(((DBTable)dataTree.SelectedObject).Schema, ((DBTable)dataTree.SelectedObject).Group);
        }

        private void ToolAddColumnGroupClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBTable)
                AddColumnGroup((DBTable)dataTree.SelectedObject);
            else if (dataTree.SelectedObject is DBColumn)
                AddColumnGroup(((DBColumn)dataTree.SelectedObject).Table);
            else if (dataTree.SelectedObject is DBColumnGroup)
                AddColumnGroup(((DBColumnGroup)dataTree.SelectedObject).Table);
        }

        private void ToolAddColumnClick(object sender, EventArgs e)
        {
            var obj = dataTree.SelectedObject;
            if (obj is DBTable)
                AddColumn((DBTable)obj, null);
            else if (obj is DBColumn)
                AddColumn(((DBColumn)obj).Table, ((DBColumn)obj).Group);
            else if (obj is DBColumnGroup)
                AddColumn(((DBColumnGroup)obj).Table, (DBColumnGroup)obj);
        }

        private void ToolAddIndexClick(object sender, EventArgs e)
        {
            var obj = dataTree.SelectedObject;
            if (obj is DBTable)
                AddIndex((DBTable)obj, null);
            else if (obj is DBColumn)
                AddIndex(((DBColumn)obj).Table, (DBColumn)obj);
            else if (obj is DBColumnGroup)
                AddIndex(((DBColumnGroup)obj).Table, null);
        }

        private void ToolAddConstraintClick(object sender, EventArgs e)
        {
            var obj = dataTree.SelectedObject;
            if (obj is DBTable)
                AddConstraint((DBTable)obj, null);
            else if (obj is DBColumn)
                AddConstraint(((DBColumn)obj).Table, (DBColumn)obj);
            else if (obj is DBColumnGroup)
                AddConstraint(((DBColumnGroup)obj).Table, null);
        }

        private void ToolAddForeignClick(object sender, EventArgs e)
        {
            var obj = dataTree.SelectedObject;
            if (obj is DBTable)
                AddForeign((DBTable)obj, null);
            else if (obj is DBColumn)
                AddForeign(((DBColumn)obj).Table, (DBColumn)obj);
            else if (obj is DBColumnGroup)
                AddForeign(((DBColumnGroup)obj).Table, null);
        }

        private void ToolAddProcedureClick(object sender, EventArgs e)
        {
            var row = new DBProcedure();
            if (dataTree.SelectedObject is DBProcedure)
            {
                ((DBProcedure)row).Parent = (DBProcedure)dataTree.SelectedObject;
            }
        }

        private void ToolAddProcedureParamClick(object sender, EventArgs e)
        {
            var row = new DBProcParameter();
            if (dataTree.SelectedObject is DBProcedure)
            {
                ((DBProcParameter)row).Procedure = (DBProcedure)dataTree.SelectedObject;
            }
        }

        private void ToolDBExportClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedObject as DBSchema;
            if (schema != null)
            {
                //var doc = new System.Xml.XmlDocument();
                var stream = new System.IO.MemoryStream();
                var writer = System.Xml.XmlWriter.Create(stream);
                writer.WriteStartDocument(true);
                writer.WriteStartElement("html");
                writer.WriteElementString("title", schema.DisplayName);
                writer.WriteStartElement("body");
                writer.WriteElementString("H1", schema.DisplayName);
                schema.Tables.Sort(new InvokerComparer<DBTable>("Code"));
                foreach (var table in schema.Tables)
                {
                    if (table.Type == DBTableType.Table)
                    {
                        writer.WriteElementString("H2", table.DisplayName + " (" + table.Name + ")");
                        writer.WriteStartElement("table");
                        writer.WriteAttributeString("border", "1");
                        writer.WriteAttributeString("cellspacing", "0");
                        writer.WriteAttributeString("cellpadding", "5");
                        writer.WriteStartElement("tr");

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Code");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Name");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Type");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Size");
                        writer.WriteEndElement();//th
                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Prec");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Spec");
                        writer.WriteEndElement();//th        

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Reference");
                        writer.WriteEndElement();//th


                        writer.WriteEndElement();//tr

                        foreach (var column in table.Columns)
                        {
                            writer.WriteStartElement("tr");

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Name);
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Name);
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.DBDataType.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Size.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Scale.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Keys.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.ReferenceTable != null ? (column.ReferenceTable + " (" + column.ReferenceTable.Name + ")") : null);
                            writer.WriteEndElement();//td

                            writer.WriteEndElement();//tr
                        }
                        writer.WriteEndElement();//table
                    }
                }

                writer.WriteEndElement();//body
                writer.WriteEndElement();//html
                writer.WriteEndDocument();
                writer.Flush();

                System.IO.File.WriteAllBytes("temp.html", stream.ToArray());
            }
            //ProjectHandler ph = new ProjectHandler();
            //ph.Project = new DBExport();
            //if (dataTree.SelectedObject is DBSchema)
            //    ((DBExport)ph.Project).SourceSchema = schema;
            //GuiService.Main.CurrentProject = ph;
        }

        private void ToolDBRelationClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedObject as DBSchema;
            if (schema == null && dataTree.SelectedObject is DBSchemaItem)
                schema = ((DBSchemaItem)dataTree.SelectedObject).Schema;
            //foreigns
            (schema).GenerateRelations();
            //column gruop
            foreach (var table in schema.Tables)
            {
                var ngcolumn = table.ColumnGroups[null];
                if (ngcolumn != null)
                    ngcolumn.Name = "name";
                var gcolumn = table.ColumnGroups["system"];
                if (gcolumn == null)
                {
                    gcolumn = new DBColumnGroup("system");
                    table.ColumnGroups.Add(gcolumn);
                    foreach (var column in table.Columns)
                    {
                        if ((column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary ||
                            (column.Keys & DBColumnKeys.Date) == DBColumnKeys.Date ||
                            (column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp ||
                            (column.Keys & DBColumnKeys.State) == DBColumnKeys.State ||
                            (column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                            column.Group = gcolumn;
                    }
                }


            }
            //            
        }

        private void ToolTableExplorerOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedObject is DBTable)
            {
                EditTableData((DBTable)dataTree.SelectedObject);
            }
        }

        private void ToolCopyClick(object sender, EventArgs e)
        {
            var obj = dataTree.SelectedObject;
            if (obj == null)
                return;
            if (obj is DBTable)
            {
                DBTable newTable = (DBTable)((DBTable)obj).Clone();
                ShowNewItem(newTable);
            }
            else if (obj is DBColumn)
            {
                var selected = (DBColumn)obj;
                var column = (DBColumn)selected.Clone();
                column.Table = selected.Table;
                ShowNewItem(column);
            }
            else if (obj is DBProcedure)
            {
                var procedure = (DBProcedure)((DBProcedure)obj).Clone();
                foreach (var par in ((DBProcedure)obj).Parameters)
                {
                    var parameter = (DBProcParameter)par.Clone();
                    parameter.Procedure = procedure;
                    procedure.Parameters.Add(parameter);
                }
            }
            //dataTree.SelectedNode.Tag
            //           if (dataTree.SelectedNode.Tag is DBSchema) {
            //               DataEnvir.Schems.Remove (dataTree.SelectedNode.Tag as DBSchema);
            //           } else if (dataTree.SelectedObject is DBTableGroup) {
            //               RemoveTableGroup ((DBTableGroup)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBTable) {
            //               RemoveTable ((DBTable)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBColumn) {
            //               RemoveColumn ((DBColumn)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBColumnGroup) {
            //               RemoveColumnGroup ((DBColumnGroup)dataTree.SelectedObject);
            //           }
        }

        private void ToolPropertyClick(object sender, EventArgs e)
        {
            GuiService.Main.ShowProperty(this, dataTree.SelectedNode, true);
        }

        private void DataTreeOnNodeMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            if (dataTree.SelectedNode != null && e.HitTest.MouseButton == PointerButton.Right)
            {
                if (dataTree.SelectedNode.Tag is DBSchema)
                {
                    //contextMain.Items.Add(toolMainAdd);
                }

                contextMain.Popup(this, e.HitTest.Point.X, e.HitTest.Point.Y);
            }
        }

        private void DataTreeOnAfterSelect(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode != null)
                GuiService.Main.ShowProperty(this, dataTree.SelectedObject, false);
        }

        private void DataTreeOnNodeMouseDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (dataTree.SelectedObject != null)
            {
                DBTable dt = dataTree.SelectedObject as DBTable;
                if (dt != null)
                {
                    EditTableData(dt);
                }
                else if (dataTree.SelectedObject is DBProcedure)
                {
                    var procedure = (DBProcedure)dataTree.SelectedObject;
                    var editor = GuiService.Main.DockPanel.Find(ProcedureEditor.GetName(procedure)) as ProcedureEditor;
                    if (editor == null)
                        editor = new ProcedureEditor() { Procedure = procedure };
                    GuiService.Main.DockPanel.Put(editor, DockType.Content);
                }
            }
        }

        private void ToolMainRefreshOnClick(object sender, EventArgs e)
        {
            dataTree.Localize();
        }

        private void ToolLoadFileClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog() { Multiselect = true };
            if (dialog.Run(ParentWindow))
            {
                foreach (string fileName in dialog.FileNames)
                {
                    string name = System.IO.Path.GetFileName(fileName);
                    var query = new Query(new[]
                        {
                        new QueryParameter(){
                            Invoker = EmitInvoker.Initialize<DBProcedure>(nameof(DBProcedure.DataName)),
                            Value = name
                        },
                        new QueryParameter(){
                            Invoker = EmitInvoker.Initialize<DBProcedure>(nameof(DBProcedure.ProcedureType)),
                            Value = ProcedureTypes.File
                        },
                        });
                    var procedire = CurrentSchema.Procedures.Find(query) as DBProcedure;
                    if (procedire == null)
                    {
                        procedire = new DBProcedure();
                        procedire.ProcedureType = ProcedureTypes.File;
                        procedire.DataName = name;
                        procedire.Name = System.IO.Path.GetFileNameWithoutExtension(name);
                    }
                    procedire.Data = System.IO.File.ReadAllBytes(fileName);
                    procedire.Date = System.IO.File.GetLastWriteTime(fileName);
                    procedire.Save();
                }
                MessageDialog.ShowMessage(ParentWindow, Common.Locale.Get("FlowExplorer", "Files load complete!"), "File Loader!");
            }
        }
    }

    public class DBSchemaChange : ICheck
    {
        private DBSchemaItem item;
        private DDLType change;
        private bool check = true;

        public string Type
        {
            get { return item == null ? null : Locale.Get(item.GetType().FullName, item.GetType().Name); }
        }

        public DBSchemaItem Item
        {
            get { return item; }
            set { item = value; }
        }

        public DDLType Change
        {
            get { return change; }
            set { change = value; }
        }

        public string Generate()
        {
            return item.FormatSql(change);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", change, Type, item);
        }

        public bool Check
        {
            get { return check; }
            set { check = value; }
        }
    }
}