﻿using System;
using System.Collections;
using System.Collections.Generic;
using Xwt.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using DSBarCode;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System.ComponentModel;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;
using DataWF.Module.CommonGui;

namespace DataWF.Module.FlowGui
{
    public enum DocumentEditorState
    {
        Readonly,
        Edit,
        Create,
        Send,
        Procedure,
        None
    }

    public class DocumentEditor : VPanel, IDocked, IDockContent, ISerializableElement
    {
        static Dictionary<Type, List<Type>> typesCache = new Dictionary<Type, List<Type>>();
        static List<Type> GetTypes(Type documentType)
        {
            if (!typesCache.TryGetValue(documentType, out List<Type> types))
            {
                types = new List<Type>();
                foreach (var property in TypeHelper.GetPropertyes(documentType))
                {
                    if (property.PropertyType.IsGenericType
                        && TypeHelper.IsInterface(property.PropertyType, typeof(IEnumerable))
                        && TypeHelper.GetBrowsable(property))
                    {
                        var type = property.PropertyType.GetGenericArguments().First();
                        if (TypeHelper.IsBaseType(type, typeof(DocumentDetail)))
                        {
                            types.Add(type);
                        }
                    }
                }
                typesCache[documentType] = types;
            }
            return types;
        }

        private Toolsbar tools;
        private ToolItem toolSave;
        private ToolItem toolRefresh;
        private ToolItem toolSend;
        private ToolItem toolLogs;
        private ToolItem toolDelete;
        private ToolItem toolBarCode;
        private ToolItem toolReturn;
        private ToolItem toolForward;
        private ToolItem toolNext;
        private ToolDropDown toolProcedures;
        private DockBox dock;
        private ToolLabel toolLabel = new ToolLabel();
        private IEnumerable<ToolItem> toolsItems;
        private DockPage pageWorks;
        private DockPage pageRefers;
        private DockPage pageHeader;

        private List<Document> _list;
        private Document document;
        private Template template;
        private DocumentWork work;

        private DocumentEditorState state = DocumentEditorState.None;
        private Type documentType;
        private DocumentReferenceView references;

        public DocumentEditor()
        {
            toolProcedures = new ToolDropDown { Name = "Procedures", Glyph = GlyphType.PuzzlePiece, DropDown = new Menubar { Name = "Procedures" } };
            toolSave = new ToolItem(ToolSaveClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Save", Glyph = GlyphType.SaveAlias };
            toolRefresh = new ToolItem(ToolRefreshClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Refresh", Glyph = GlyphType.Refresh };
            toolDelete = new ToolItem(ToolDeleteClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Delete", Glyph = GlyphType.MinusSquare };
            toolLogs = new ToolItem(ToolLogsOnClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Logs", Glyph = GlyphType.History };
            toolBarCode = new ToolItem(ToolBarCodeClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "BarCode", Glyph = GlyphType.Barcode };
            toolReturn = new ToolItem(ToolReturnClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Return", Glyph = GlyphType.StepBackward };
            toolSend = new ToolItem(ToolAcceptClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Send/Accept", Glyph = GlyphType.CheckCircle };
            toolForward = new ToolItem(ToolForwardClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Forward", Glyph = GlyphType.StepForward };
            toolNext = new ToolItem(ToolNextClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Next", Glyph = GlyphType.Forward };

            tools = new Toolsbar(
               //toolProcedures,
               toolSave,
               toolRefresh,
               toolDelete,
               new ToolSeparator(),
               toolLogs,
               toolBarCode,
               new ToolSeparator(),
               //toolReturn,
               toolSend
               //toolForward,
               //toolNext,
               //toolLabel
               )
            { Name = "tools" };
            toolsItems = tools.Items.Cast<ToolItem>();

            dock = new DockBox()
            {
                Name = "dock",
                BackgroundColor = GuiEnvironment.Theme["Page"].BaseColor,
                VisibleClose = false
            };
            pageHeader = dock.Put(new DocumentHeader(), DockType.Left);
            pageHeader.Panel.DockItem.Width = 350;//pageHeader.Panel.MapItem.FillWidth = true;
            pageWorks = dock.Put(new DocumentWorkView(), DockType.LeftBottom);
            pageRefers = dock.Put((references = new DocumentReferenceView()), DockType.Content);

            Glyph = GlyphType.Book;
            Name = "DocumentEditor";
            Text = "Document";
            Tag = "Document";

            PackStart(tools, false, false);
            PackStart(dock, true, true);

            Localize();
        }

        public Toolsbar MainMenu
        {
            get { return tools; }
        }

        public IDockContainer DockPanel
        {
            get { return dock; }
        }

        public void SetList(List<Document> list)
        {
            this._list = list;
        }

        public List<Document> GetList()
        {
            if (_list == null)
                _list = new List<Document>();
            if (!_list.Contains(document))
                _list.Insert(0, document);
            return _list;
        }

        private void DockPageSelected(object sender, DockPageEventArgs e)
        {
            LoadPage(e.Page);
        }

        private async void LoadPage(DockPage page)
        {
            if (page == null || document == null)
                return;
            if (page.Widget is IReadOnly)
            {
                ((IReadOnly)page.Widget).ReadOnly = state == DocumentEditorState.Readonly || !document.Access.Edit;
            }
            else
            {
                page.Widget.Sensitive = state == DocumentEditorState.Edit && document.Access.Edit;
            }
            if (page.Widget is IDocument)
                ((IDocument)page.Widget).Document = document;
            if (page.Widget is IExecutable)
                ((IExecutable)page.Widget).Execute(new ExecuteArgs(document));
            if (page.Widget is TableEditor)
                ((TableEditor)page.Widget).OwnerRow = document;
            if (document.Attached)
            {
                if (page.Widget is ILoader)
                    await ((ILoader)page.Widget).Loader.LoadAsync();
                if (page.Widget is ISync)
                    await ((ISync)page.Widget).SyncAsync();
            }
        }

        public override void Localize()
        {
            base.Localize();
            //tools.Localize();
            //dock.Localize();
        }

        private void ToolLogsOnClick(object sender, EventArgs e)
        {
            var logViewer = new DataLogView { Filter = document, Mode = DataLogMode.Document };
            logViewer.ShowWindow(this);
        }

        #region IDocumentUserControl Members

        public static bool ExecuteDocumentsProcedure(DBProcedure proc, IEnumerable documents)
        {
            if (proc.ProcedureType == ProcedureTypes.Assembly || proc.ProcedureType == ProcedureTypes.Source)
            {
                var type = proc.GetObjectType();
                if (type.GetInterface("IDocuments") != null)
                {
                    object result = proc.CreateObject();
                    if (result is IDocuments)
                    {
                        ((IDocuments)result).Documents = documents;
                        return true;
                    }
                }
            }
            return false;
        }

        public object ExecuteDocumentProcedure(DBProcedure proc, Document document, bool callback)
        {
            document.Save();
            var param = new ExecuteArgs(document);
            object result = null;
            try
            {
                result = proc.CreateObject(param);
                if (TypeHelper.IsBaseType(result.GetType(), typeof(Widget)))
                {
                    result = proc.ExecuteObject(result, param);
                }
                else
                {
                    var task = proc.ExecuteTask(result, param);

                    if (GuiService.Main != null)
                    {
                        if (callback)
                            task.Callback += TaskCallback;
                        GuiService.Main.AddTask(this, task);
                        result = null;
                    }
                    else
                        result = task.Execute();
                }

            }
            catch (Exception ex)
            {
                result = ex;
            }
            return result;
        }


        private void ProcedureItemClick(object sender, EventArgs e)
        {
            var sen = sender as MenuItemProcedure;
            if (sen.DropDown?.Items.Count > 0)
            {
                return;
            }
            DBProcedure proc = sen.Procedure;
            object result = null;
            var list = GetList();
            if (list != null && list.Count > 1)
            {
                if (!ExecuteDocumentsProcedure(proc, list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Document listDocument = list[i];
                        result = ExecuteDocumentProcedure(proc, listDocument, false);//i == list.Count - 1);
                        if (result != null)
                            CheckProcRezult(new ExecuteDocumentArg(listDocument, proc, result, this));
                    }
                }
                if (GuiService.Main != null)
                {
                    var task = new TaskExecutor();
                    task.Name = "Confirmation!";
                    task.Action = () =>
                    {
                        Application.Invoke(() => MessageDialog.ShowMessage(ParentWindow,
                                                                           string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecute successful!"), proc.Name), "Methods"));
                        return null;
                    };
                    GuiService.Main.AddTask(this, task);
                }
            }
            else
            {
                result = ExecuteDocumentProcedure(proc, document, true);
                if (result != null)
                    CheckProcRezult(new ExecuteDocumentArg(document, proc, result, this));
            }
        }

        private void TaskCallback(RProcedureEventArgs e)
        {
            Application.Invoke(() => CheckProcRezult(new ExecuteDocumentArg((Document)e.Task.Tag, (DBProcedure)e.Task.Object, e.Result, this)));
        }

        private void CheckProcRezult(object p)
        {
            ExecuteDocumentArg arg = p as ExecuteDocumentArg;
            CheckProcRezult(arg);
            if (arg.Procedure.ProcedureType == ProcedureTypes.StoredFunction || arg.Procedure.ProcedureType == ProcedureTypes.StoredProcedure)
            {
                document.IniType = DocInitType.Default;
            }
            CheckState(DocumentEditorState.None);
        }

        public void CheckProcRezult(ExecuteDocumentArg arg)
        {
            if (arg.Document != this.Document)
                return;
            if (arg.Result is Window)
            {
                var f = arg.Result as Window;
                f.ShowInTaskbar = false;
                f.Show();
            }
            else if (arg.Result is Widget)
            {
                var c = arg.Result as Widget;
                if (c is IText)
                    ((IText)c).Text = arg.Procedure.Name;
                if (arg.Tag == this)
                    dock.Put(c);
            }

            else if (arg.Result is IList<DocumentReference>)
            {
                pageRefers.Panel.CurrentPage = pageRefers;
            }
            else if (arg.Result is DocInitType)
            {
                var ini = (DocInitType)arg.Result;
                if (ini == DocInitType.References)
                    pageRefers.Panel.CurrentPage = pageRefers;
                else if (ini == DocInitType.Data)
                    pageRefers.Panel.CurrentPage = pageHeader;
            }
            else if (arg.Result is Document && arg.Result != arg.Document)
            {
                var editor = new DocumentEditor();
                editor.Document = (Document)arg.Result;
                editor.ShowWindow(arg.Tag as DocumentEditor);
            }
            else if (arg.Result is DocumentData)
            {
                ((DocumentData)arg.Result).Execute();
            }
            else if (arg.Result is Exception)
            {
                if (GuiService.Main == null)
                    Helper.OnException((Exception)arg.Result);
                MessageDialog.ShowError(ParentWindow, "Document Procedure", string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecution fail!"), arg.Procedure.Name));
            }
            else
            {
                if (arg.Result == null || arg.Result.ToString().Length == 0)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Document Procedure", string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecut successful!"), arg.Procedure.Name));
                }
                else
                {
                    ShowResultDialog(arg.Tag, arg.Procedure, arg.Result);
                }
            }
        }

        public static void ShowResultDialog(object parent, DBProcedure proc, object result)
        {
            var textbox = new RichTextView();
            textbox.LoadText(result.ToString(), Xwt.Formats.TextFormat.Plain);
            var wind = new ToolWindow()
            {
                Target = textbox,
                Mode = ToolShowMode.Dialog,
                Size = new Size(600, 400)
            };
            wind.ButtonClose.Visible = false;
            wind.Label.Text = "Result of " + (proc != null ? proc.Name : string.Empty);
            wind.Show();
            //wind.Dispose();
        }

        public DocumentWork Stage
        {
            get { return work; }
            set
            {
                if (work != value)
                {
                    work = value;
                    var stage = work.Stage;
                    if (stage != null)
                    {
                        foreach (var param in stage.GetParams())
                        {
                            if (param is StageForeign)
                                InitReference(stage, (StageForeign)param);
                            else if (param is StageProcedure)
                                InitProcedure(stage, (StageProcedure)param);
                            //else if (param.Type == ParamType.Template)
                            //    InitTemplate(stage, param.Param as Template, toolTemplates.DropDown.Items);
                        }
                    }
                    foreach (MenuItemProcedure item in toolProcedures.DropDownItems)
                    {
                        item.Visible = item.Tag == template || item.Tag == stage;
                    }
                    foreach (var page in dock.GetPages())
                    {
                        page.Visible = page.Tag == null || page.Tag.Equals(template) || page.Tag.Equals(DocumentType);
                    }
                    //foreach (TemplateMenuItem item in toolTemplates.DropDownItems)
                    //    item.Visible = item.Tag == template || item.Tag == stage;

                }
            }
        }

        public Template Template
        {
            get { return template; }
            set
            {
                if (template != value)
                {
                    template = value;

                    //if (template != null)
                    //    foreach (TemplateParam param in template.TemplateAllParams)
                    //    {
                    //        if (param.Type == ParamType.Reference)
                    //            InitReference(template, param);
                    //        else if (param.Type == ParamType.Procedure)
                    //            InitProcedure(template, param);
                    //        //else if (param.Type == ParamType.Template)
                    //        //    InitTemplate(template, param.Param as Template, toolTemplates.DropDownItems);
                    //    }
                    //foreach (MenuItemProcedure item in toolProcedures.DropDownItems)
                    //{
                    //    item.Visible = item.Tag == template;
                    //}
                    foreach (DockPage page in dock.GetPages())
                    {
                        page.Visible = page.Tag == null || page.Tag.Equals(template) || page.Tag.Equals(DocumentType);
                    }
                    //foreach (TemplateMenuItem item in toolTemplates.DropDownItems)
                    //    item.Visible = item.Tag == template;
                }
            }
        }

        public DocumentEditorState EditorState
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;
                state = value;
                Text = document.ToString();// +"(" + this.Tag.ToString() + ")";

                pageHeader.Tag = document.Template;
                pageRefers.Tag = document.Template;

                bool from = false;
                dock.PageSelected -= DockPageSelected;
                if (state != DocumentEditorState.Create)
                {
                    Template = document.Template;
                    var cwork = document.WorkCurrent;
                    if (cwork != null)
                        Stage = cwork;
                    else if (document.GetLastWork() != null)
                        cwork = document.GetLastWork();
                    from = cwork != null && cwork.From != null && (cwork.From.IsCurrent || cwork.IsCurrent);
                    //pages
                    toolLabel.Text = cwork == null || cwork.Stage == null ? "" : cwork.Stage.ToString();
                }

                dock.PageSelected += DockPageSelected;

                foreach (var panel in dock.GetDockPanels())
                {
                    if (panel.CurrentPage == null)
                        panel.CurrentPage = panel.FirstOrDefault();

                    LoadPage(panel.CurrentPage);
                }
                //if (documentType != null)
                //{
                //    XmlDeserialize(documentType.Name.ToString());
                //}
                toolReturn.Sensitive = from;
                toolSend.Sensitive = state != DocumentEditorState.Create;
                toolNext.Sensitive = state == DocumentEditorState.Edit;
                toolForward.Sensitive = state == DocumentEditorState.Edit;
                toolProcedures.Sensitive = state == DocumentEditorState.Edit;
                //toolTemplates.Sensitive = state != DocumentEditorState.Create;
                toolRefresh.Sensitive = state != DocumentEditorState.Create;
                toolSave.Sensitive = state != DocumentEditorState.Readonly;
                toolLogs.Sensitive = state != DocumentEditorState.Create;
                toolBarCode.Sensitive = state != DocumentEditorState.Create;
                pageRefers.Visible = state != DocumentEditorState.Create;
            }
        }

        public Document Document
        {
            get { return document; }
            set
            {
                if (document == value || state == DocumentEditorState.Send)
                    return;

                if (document != null)
                {
                    document.PropertyChanged -= DocumentPropertyChanged;
                    document.ReferenceChanged -= DocumentPropertyChanged;
                }
                document = value;
                toolLabel.Text = "";
                if (document == null)
                {
                    foreach (var item in toolsItems)
                    {
                        item.Sensitive = false;
                    }
                    return;
                }

                DocumentType = document.GetType();

                document.PropertyChanged += DocumentPropertyChanged;
                document.ReferenceChanged += DocumentPropertyChanged;

                if (document.Attached && document.UpdateState == DBUpdateState.Default && document.GetLastWork() == null)
                    document.GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);

                if (document.IsCurrent)
                {
                    var cwork = document.WorkCurrent;
                    if (cwork.UpdateState == DBUpdateState.Default && cwork.DateRead == DateTime.MinValue)
                    {
                        cwork.DateRead = DateTime.Now;
                        cwork.Save();
                    }
                }

                if (document.Id != null && document.Id != null)
                    Name = "DocumentEditor" + document.Id.ToString();

                //var works = document.GetWorks();
                toolDelete.Visible = document.Access.Delete;// works.Count == 0 || (works.Count == 1 && works[0].IsUser);

                CheckState(DocumentEditorState.None);
            }
        }

        public Type DocumentType
        {
            get { return documentType; }
            set
            {
                if (documentType == value)
                    return;
                //if (documentType != null)
                //{
                //    XmlSerialize(documentType.Name.ToString());
                //}
                documentType = value;
                GetPages(documentType).ForEach(p => p.Tag = value);
            }
        }

        public DockType DockType { get; set; }

        public bool HideOnClose { get; set; }

        private void DocumentPropertyChanged(object sender, EventArgs e)
        {
            if (document != null && state != DocumentEditorState.Send)
                Application.Invoke(() => toolSave.Sensitive = document != null && (document.IsChanged || state == DocumentEditorState.Create));
        }

        private void CheckState(object obj)
        {
            if (obj is DocumentEditorState)
                state = (DocumentEditorState)obj;
            if (document != null)
            {
                var work = document.WorkCurrent;
                EditorState = !document.Attached || document.UpdateState == DBUpdateState.Insert
                    ? DocumentEditorState.Create
                    : work != null && work.IsCurrent
                    ? DocumentEditorState.Edit
                    : DocumentEditorState.Readonly;
                toolSave.Sensitive = document.IsChanged || state == DocumentEditorState.Create;
            }
        }

        public DockPage InitReference(DBItem owner, StageForeign param)
        {
            var foreign = param.Foreign;
            if (foreign == null || foreign.ReferenceTable != Document.DBTable)
                return null;

            var name = foreign.Table.Name + " (" + foreign.Column.Name + ")";
            var page = dock.GetPage(name);
            if (page == null)
            {
                var editor = new TableEditor()
                {
                    Name = name,
                    Text = param.Name == null || param.Name.Length == 0 ? foreign.Table.ToString() : param.Name,
                    TableView = foreign.Table.CreateItemsView("", DBViewKeys.None, DBStatus.Current),
                    OwnerColumn = foreign.Column,
                    OpenMode = TableEditorMode.Referencing
                };
                //editor.ToolSave = false;
                editor.SelectionChanged += OnReferenceTableRowSelected;

                page = dock.Put(editor, DockType.Content);
            }
            page.Tag = owner;
            return page;
        }

        private void OnReferenceTableRowSelected(object sender, ListEditorEventArgs e)
        {
            if (this.Parent != null && GuiService.Main != null)
                GuiService.Main.ShowProperty(this, e.Item, false);
        }

        public void InitProcedure(DBItem owner, StageProcedure param)
        {
            var proc = param.Procedure as DBProcedure;
            if (proc == null || param.ProcedureType != ParamProcudureType.Manual)
                return;

            string name = "procedure" + proc.Name;

            if (proc.ProcedureType == ProcedureTypes.Query)
            {
                DockPage page = dock.GetPage(name);
                if (page == null)
                {
                    var qview = new PQueryView();
                    qview.Name = name;
                    qview.Text = param.Name == null || param.Name.Length == 0 ? proc.ToString() : param.Name;
                    qview.Document = document;
                    qview.Procedure = proc;
                    page = dock.Put(qview, DockType.Content);
                }
                page.Tag = owner;
            }
            Type t = proc.ProcedureType == ProcedureTypes.Assembly || proc.ProcedureType == ProcedureTypes.Source ? proc.GetObjectType() : null;
            if (t != null && !TypeHelper.IsBaseType(t, typeof(Window)) && TypeHelper.IsBaseType(t, typeof(Widget)))
            {
                DockPage page = dock.GetPage(name);
                if (page == null)
                {
                    var control = (Widget)EmitInvoker.CreateObject(t, true);
                    control.Name = name;
                    if (control is IText)
                        ((IText)control).Text = param.Name == null || param.Name.Length == 0 ? proc.ToString() : param.Name;
                    page = dock.Put(control, DockType.Content);
                }
                page.Tag = owner;
            }
            else
            {
                var item = toolProcedures.DropDown?.Items[name] as MenuItemProcedure;
                if (item == null)
                {
                    item = new MenuItemProcedure(proc);
                    item.Name = name;
                    item.Click += ProcedureItemClick;
                    toolProcedures.DropDown.Items.Add(item);
                }
                item.Tag = owner;
            }
        }

        public static bool CheckVisible(ToolItem collection)
        {
            foreach (var item in collection)
            {
                if (!(item is ToolSeparator) && item.Sensitive)
                    return true;
            }
            return false;
        }


        #endregion        

        private void ToolSaveClick(object sender, EventArgs e)
        {
            var list = GetList();
            foreach (Document document in list)
            {
                if (document.IsEdited())
                {
                    document.Save(new ExecuteDocumentCallback(CheckProcRezult));
                }
                document.IsChanged = false;
            }

            CheckState(null);
        }

        private void ToolRefreshClick(object sender, EventArgs e)
        {
            foreach (var relation in Document.DBTable.GetChildRelations())
            {
                foreach (DBItem row in document.GetReferencing(relation, DBLoadParam.None))
                    row.Reject();
            }
            document.Reject();
            document.IniType = DocInitType.Default;
            CheckState(DocumentEditorState.None);
        }

        private void ToolDeleteClick(object sender, EventArgs e)
        {
            var deleter = new RowDeleting { Row = document };
            deleter.Show(this, Point.Zero);
            //deleter.Dispose();
            if (document != null && (document.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                Document = null;
            }
        }

        private void ToolBarCodeClick(object sender, EventArgs e)
        {
            if (Document == null)
                return;
            var barcode = new BarCodeCtrl
            {
                ShowFooter = true,
                //FooterFont = new Font (control.FooterFont.FontFamily, 8.0F),
                BarCodeHeight = 25,
                BarCode = document.Id.ToString(),
                Weight = BarCodeCtrl.BarCodeWeight.Small
            };
            barcode.ShowWindow(ParentWindow, new Size(200, 100));

            //using (var stImage = new MemoryStream())
            //{
            //    barcode.SaveImage(stImage);
            //    var im = Image.FromStream(stImage);
            //    var editor = new ImageEditor();
            //    editor.Image = im;
            //    editor.Text = "Bar Code";
            //    editor.ShowDialog(this);
            //}
        }

        private void ToolAcceptClick(object sender, EventArgs e)
        {
            var work = document.WorkCurrent;
            if (work != null)
            {
                if (work.User == null)
                {
                    var question = new QuestionMessage("Accept", "Accept to work?");
                    question.Buttons.Add(Command.No);
                    question.Buttons.Add(Command.Yes);
                    if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                    {
                        if (work.Stage != null && !work.Stage.Access.Edit)
                        {
                            MessageDialog.ShowMessage(ParentWindow, "Access denied!", "Accept");
                        }
                        else
                        {
                            work.User = User.CurrentUser;
                        }
                    }
                }
            }
            else
            {
                work = document.GetWork();
                if (work != null && work.User != null && !work.User.IsCurrent)
                {
                    var rezult = MessageDialog.AskQuestion("Accept", "Document current on " + work.User + " Accept anywhere?", Command.No, Command.Yes);
                    if (rezult == Command.No)
                        return;
                }
            }
            Send(null, null, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document = null;
                dock.Dispose();
            }
            base.Dispose(disposing);
        }

        public List<DockPage> GetPages(Type documentType)
        {
            var documentWidgets = new List<DockPage>();
            foreach (var type in GetTypes(documentType))
            {
                if (TypeHelper.IsBaseType(type, typeof(DocumentWork)))
                    documentWidgets.Add(pageWorks);
                else if (TypeHelper.IsBaseType(type, typeof(DocumentReference)))
                    documentWidgets.Add(pageRefers);
                else
                {
                    var name = type.Name;
                    var page = dock.GetPage(name);
                    if (page == null)
                    {
                        var widgetType = TypeHelper.IsBaseType(type, typeof(DocumentData))
                            ? typeof(DocumentDataView<>).MakeGenericType(type)
                            : typeof(DocumentDetailView<>).MakeGenericType(type);
                        Widget widget = (Widget)EmitInvoker.CreateObject(widgetType);
                        widget.Name = name;
                        page = dock.Put(widget, TypeHelper.IsBaseType(type, typeof(DocumentData)) ? DockType.LeftBottom : DockType.Content);
                    }
                    documentWidgets.Add(page);
                }
            }
            return documentWidgets;
        }

        private void ToolReturnClick(object sender, EventArgs e)
        {
            var work = document.WorkCurrent ?? document.GetWork() ?? document.GetLastWork();
            Send(work, work.From.Stage, work.From.User, DocumentSendType.Return);
        }

        private void ToolNextClick(object sender, EventArgs e)
        {
            Send(document.WorkCurrent, null, null, DocumentSendType.Next);
        }

        private void ToolForwardClick(object sender, EventArgs e)
        {
            Send(document.WorkCurrent, null, null, DocumentSendType.Forward);
        }

        private void Send(DocumentWork work, Stage stage, User user, DocumentSendType sendType = DocumentSendType.Next)
        {
            state = DocumentEditorState.Send;
            var sender = new DocumentSender();
            sender.Localize();
            sender.Initialize(GetList());
            sender.SendType = sendType;
            sender.Hidden += SenderSendComplete;
            if (stage != null && user != null)
                sender.Send(stage, user, sendType);
            sender.Show(toolSend.Bar, toolSend.Bound.Location);
        }

        public event EventHandler SendComplete;

        private void SenderSendComplete(object senderObj, EventArgs e)
        {
            CheckState(DocumentEditorState.None);
            SendComplete?.Invoke(this, e);
        }

        public override void Serialize(ISerializeWriter writer)
        {
            writer.WriteAttribute("TemplateId", Template?.Id);
            writer.WriteAttribute("DocumentId", Document?.Id);
            base.Serialize(writer);
        }

        public override void Deserialize(ISerializeReader reader)
        {
            var templateid = reader.ReadAttribute("TemplateId", typeof(int));
            var documentid = reader.ReadAttribute("DocumentId", typeof(long));
            base.Deserialize(reader);
            Document = Document.DBTable.LoadById(documentid);
        }

        public bool Closing()
        {
            if (Document != null && (Document.UpdateState & DBUpdateState.Delete) != DBUpdateState.Delete && EditorState != DocumentEditorState.Readonly && Document.IsChanged)
            {
                var question = new QuestionMessage(Locale.Get(nameof(DocumentEditor), "On Close"), Locale.Get(nameof(DocumentEditor), "Save changes?"));
                question.Buttons.Add(Command.No);
                question.Buttons.Add(Command.Yes);
                question.Buttons.Add(Command.Cancel);
                var dr = MessageDialog.AskQuestion(ParentWindow, question);
                if (dr == Command.Cancel)
                {
                    return false;
                }
                else if (dr == Command.Yes)
                {
                    Document.Save(null);
                }
            }
            return true;
        }

        public void Activating()
        {
            throw new NotImplementedException();
        }
    }
}
