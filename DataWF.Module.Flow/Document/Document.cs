﻿/*
 Document.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System.Runtime.Serialization;
using System.IO;

namespace DataWF.Module.Flow
{

    public class DocumentExecuteArgs : ExecuteArgs
    {
        public DocumentWork Work { get; set; }
        public Stage Stage { get; set; }
        public StageProcedure StageProcedure { get; set; }
    }

    [DataContract, Table("ddocument", "Document", BlockSize = 200)]
    public class Document : DBGroupItem, IDisposable
    {
        public static DBTable<Document> DBTable
        {
            get { return GetTable<Document>(); }
        }

        public static Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = $"{DBTable.ParseProperty(nameof(Template)).Name}={template.Id} and {DBTable.ParseProperty(nameof(Customer)).Name}={p}";
            return DBTable.Load(filter, DBLoadParam.Load).FirstOrDefault();
        }

        private static List<Document> saving = new List<Document>();

        public static object ExecuteProcedures(DocumentExecuteArgs param, IEnumerable<StageProcedure> enumer)
        {
            object result = null;
            foreach (var item in enumer)
            {
                if (item.Procedure == null)
                {
                    throw new ArgumentNullException($"{nameof(StageProcedure)}.{nameof(StageProcedure.Procedure)} not defined!");
                }
                param.StageProcedure = item;
                result = item.Procedure.Execute(param);
            }

            return result;
        }

        public static QQuery CreateRefsFilter(object id)
        {
            var query = new QQuery("", DocumentReference.DBTable);
            query.Parameters.Add(CreateRefsParam(id));
            return query;
        }

        public static QParam CreateRefsParam(object id)
        {
            var qrefing = new QQuery(string.Format("select {0} from {1} where {2} = {3}",
                                                   DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name,
                                                   DocumentReference.DBTable.Name,
                                                   DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name,
                                                   id));
            var qrefed = new QQuery(string.Format("select {2} from {1} where {0} = {3}",
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name,
                                                  DocumentReference.DBTable.Name,
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name,
                                                  id));

            var param = new QParam();
            param.Parameters.Add(QQuery.CreateParam(LogicType.And, DBTable.PrimaryKey, CompareType.In, qrefed));
            param.Parameters.Add(QQuery.CreateParam(LogicType.Or, DBTable.PrimaryKey, CompareType.In, qrefing));
            return param;
        }

        public static void LoadDocuments(User user)
        {
            var qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.Columns.Add(new QColumn(nameof(DocumentWork.Document)));
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, user);

            var qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qWork);

            Document.DBTable.Load(qDocs, DBLoadParam.Synchronize);
            DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize);
        }

        public static event DocumentSaveDelegate Saved;

        private DocInitType initype = DocInitType.Default;
        private int changes = 0;
        //private DBItem parent = DBItem.EmptyItem;

        public event EventHandler<DBItemEventArgs> ReferenceChanged;

        public override void OnAttached()
        {
            base.OnAttached();
            return;
            if (UpdateState == DBUpdateState.Default && (WorkStage == null || WorkStage.Length == 0))
                GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
        }

        internal void OnReferenceChanged(DBItem item)
        {
            if (!item.Attached)
                return;

            if (item is DocumentWork)
            {
                var work = (DocumentWork)item;
                if (work.IsComplete || work.UpdateState == DBUpdateState.Default)
                    RefreshCache();
            }
            else if (item is DocumentReference)
            {
                var reference = (DocumentReference)item;
                RefChanged?.Invoke(this, ListChangedType.Reset);
            }
            if (item.UpdateState != DBUpdateState.Default && (item.UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit && item.Attached)
            {
                changes++;
            }
            else if (changes > 0 && (item.UpdateState == DBUpdateState.Default || !item.Attached))
            {
                changes--;
            }
            ReferenceChanged?.Invoke(this, new DBItemEventArgs(item));
        }

        public Document()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("template_id", Keys = DBColumnKeys.View), Index("ddocument_template_id", Unique = false)]
        public int? TemplateId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [ReadOnly(true)]
        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get { return GetPropertyReference<Template>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetGroupValue<long?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Document Parent
        {
            get { return GetGroupReference<Document>(); }
            set
            {
                SetGroupReference(value);
                if (Customer == null)
                {
                    Customer = value?.Customer;
                    //Address = value?.Address;
                }
            }
        }

        [DataMember, Column("document_date")]
        public DateTime? DocumentDate
        {
            get { return GetProperty<DateTime?>(nameof(DocumentDate)); }
            set { SetProperty(value, nameof(DocumentDate)); }
        }

        [DataMember, Column("document_number", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("ddocuument_document_number")]
        public string Number
        {
            get { return GetProperty<string>(nameof(Number)); }
            set
            {
                SetProperty(value, nameof(Number));
                var data = GetTemplatedData();
                if (data != null)
                    data.RefreshName();
            }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference(nameof(CustomerId))]
        public virtual Customer Customer
        {
            get { return GetPropertyReference<Customer>(); }
            set
            {
                SetPropertyReference(value);
                //Address = Customer?.Address;
            }
        }

        //[Browsable(false)]
        //[DataMember, Column("address_id")]
        //public int? AddressId
        //{
        //    get { return GetProperty<int?>(nameof(AddressId)); }
        //    set { SetProperty(value, nameof(AddressId)); }
        //}

        //[Reference(nameof(AddressId))]
        //public virtual Address Address
        //{
        //    get { return GetPropertyReference<Address>(); }
        //    set { SetPropertyReference(value); }
        //}

        [DataMember, Column("title", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public virtual string Title
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        [Browsable(false)]
        [DataMember, Column("work_id", ColumnType = DBColumnTypes.Internal)]
        public long? WorkId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [Reference(nameof(WorkId))]
        public DocumentWork WorkCurrent
        {
            get { return GetPropertyReference<DocumentWork>(); }
            set { SetPropertyReference(value); }
        }

        public Stage Stage
        {
            get { return WorkCurrent?.Stage; }
        }

        //[Browsable(false)]
        [DataMember, Column("work_user", ColumnType = DBColumnTypes.Internal)]
        public string WorkUser
        {
            get { return GetProperty<string>(); }
            private set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("work_stage", ColumnType = DBColumnTypes.Internal)]
        public string WorkStage
        {
            get { return GetProperty<string>(nameof(WorkStage)); }
            private set { SetProperty(value, nameof(WorkStage)); }
        }

        [Browsable(false)]
        public DateTime? WorkDate
        {
            get { return WorkCurrent?.DateCreate; }
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get { return WorkCurrent != null; }
        }

        [Browsable(false)]
        public Work Work
        {
            get { return Template.Work; }
        }

        [Browsable(false)]
        public DocInitType IniType
        {
            get { return initype; }
            set { initype = value; }
        }

        [Browsable(false)]
        [DataMember, Column("is_important")]
        public bool? Important
        {
            get { return GetProperty<bool?>(nameof(Important)); }
            set { SetProperty(value, nameof(Important)); }
        }

        [Browsable(false)]
        [DataMember, Column("is_comlete", Default = "False")]
        public bool? IsComplete
        {
            get { return GetProperty<bool?>(nameof(IsComplete)); }
            set { SetProperty(value, nameof(IsComplete)); }
        }

        public event Action<Document, ListChangedType> RefChanged;

        [ControllerMethod(true)]
        public virtual IEnumerable<DocumentReference> GetReferences()
        {
            if ((initype & DocInitType.References) != DocInitType.References)
            {
                initype |= DocInitType.References;
                DocumentReference.DBTable.Load(CreateRefsFilter(Id));
            }
            return GetReferencing<DocumentReference>(nameof(DocumentReference.DocumentId), DBLoadParam.None);
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentWork> GetWorks()
        {
            if ((initype & DocInitType.Workflow) != DocInitType.Workflow)
            {
                initype |= DocInitType.Workflow;
                GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
            }

            return GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.None);
        }

        [ControllerMethod(true)]
        public virtual IEnumerable<DocumentData> GetDatas()
        {
            if ((initype & DocInitType.Data) != DocInitType.Data)
            {
                initype |= DocInitType.Data;
                GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), DBLoadParam.Load);
            }
            return GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), DBLoadParam.None);
        }

        [ControllerMethod]
        [Browsable(false)]
        public IEnumerable<DocumentCustomer> GetCustomers()
        {
            if ((initype & DocInitType.Customer) != DocInitType.Customer)
            {
                initype |= DocInitType.Customer;
                GetReferencing<DocumentCustomer>(nameof(DocumentCustomer.DocumentId), DBLoadParam.Load);
            }
            return GetReferencing<DocumentCustomer>(nameof(DocumentCustomer.DocumentId), DBLoadParam.None);
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentComment> GetComments()
        {
            if ((initype & DocInitType.Comment) != DocInitType.Comment)
            {
                initype |= DocInitType.Comment;
                GetReferencing<DocumentComment>(nameof(DocumentComment.DocumentId), DBLoadParam.Load);
            }

            return GetReferencing<DocumentComment>(nameof(DocumentComment.DocumentId), DBLoadParam.None);
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get { return Template?.Access ?? base.Access; }
        }

        [Browsable(false)]
        public new bool IsChanged
        {
            get { return (UpdateState != DBUpdateState.Default) || changes != 0; }
            set
            {
                if (!value)
                    changes = 0;
            }
        }

        [ControllerMethod]
        public DocumentData GetDataByFileName(string fileName)
        {
            foreach (var data in GetDatas())
                if (data.FileName == fileName || data.FileName.EndsWith(fileName))
                    return data;
            return null;
        }

        [ControllerMethod]
        public IEnumerable<DocumentWork> GetWorksByStage(Stage stage)
        {
            foreach (var work in GetWorks().Reverse())
            {
                if (work.Stage == stage)
                    yield return work;
            }
        }

        [ControllerMethod]
        public IEnumerable<DocumentWork> GetWorksUncompleted(Stage filter = null)
        {
            foreach (DocumentWork work in GetWorks())
            {
                if (!work.IsComplete && (filter == null || work.Stage == filter))
                {
                    yield return work;
                }
            }
        }

        [ControllerMethod]
        public DocumentWork GetLastWork()
        {
            return GetWorks().LastOrDefault();
        }

        public string GetWorkFlow()
        {
            var workFlows = string.Empty;
            foreach (DocumentWork work in GetWorks())
            {
                string flow = work.Stage != null ? work.Stage.Work.Name : "<no name>";
                if (!work.IsComplete
                    && (workFlows == null || workFlows.IndexOf(flow, StringComparison.Ordinal) < 0))
                    workFlows = workFlows + flow + " ";
            }
            if (workFlows == null)
            {
                DocumentWork work = GetLastWork();
                workFlows = work != null && work.Stage != null ? work.Stage.Work.Name : "None";
            }
            else
                workFlows = workFlows.TrimEnd();
            return workFlows;
        }

        [ControllerMethod]
        public virtual DocumentData GetTemplatedData()
        {
            foreach (DocumentData data in GetDatas())
                if (data.IsTemplate)
                    return data;
            return null;
        }

        [ControllerMethod]
        public virtual DocumentData CreateTemplatedData()
        {
            return GenerateFromTemplate<DocumentData>(Template.GetDatas().FirstOrDefault());
        }

        public T GenerateFromTemplate<T>(TemplateData templateData) where T : DocumentData, new()
        {
            if (templateData == null)
            {
                return null;
            }
            return new T()
            {
                Document = this,
                TemplateData = templateData
            };
        }

        [ControllerMethod]
        public virtual DocumentData CreateData(string fileName)
        {
            return CreateData<DocumentData>(fileName).First();
        }

        public IEnumerable<T> CreateData<T>(params string[] files) where T : DocumentData, new()
        {
            foreach (var file in files)
            {
                var data = new T { Document = this };
                data.Load(file);
                data.GenerateId();
                data.Attach();
                yield return data;
            }
        }

        [ControllerMethod]
        public virtual DocumentReference CreateReference(Document document)
        {
            if (document == null)
                return null;

            var reference = new DocumentReference { Document = this, Reference = document };
            reference.GenerateId();
            reference.Attach();
            return reference;
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByDepartment(DocumentWork from, Stage stage, Position position)
        {
            return CreateWork(from, stage, position);
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByPosition(DocumentWork from, Stage stage, Position position)
        {
            return CreateWork(from, stage, position);
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByUser(DocumentWork from, Stage stage, User user)
        {
            return CreateWork(from, stage, user);
        }

        public DocumentWork CreateWork(DocumentWork from, Stage stage, DBItem staff)
        {
            var work = new DocumentWork
            {
                DateCreate = DateTime.Now,
                Document = this,
                Stage = stage,
                Staff = staff,
                From = from
            };

            if (stage != null)
            {
                if (stage.Keys != null
                    && (stage.Keys & StageKey.IsStop) == StageKey.IsStop
                    && (stage.Keys & StageKey.IsAutoComplete) == StageKey.IsAutoComplete)
                    work.DateComplete = DateTime.Now;
                if (stage.TimeLimit != null)
                    work.DateLimit = DateTime.Now + stage.TimeLimit;
            }
            if (staff is User && ((User)staff).IsCurrent)
            {
                work.DateRead = DateTime.Now;
            }
            work.GenerateId();
            work.Attach();
            return work;
        }

        public void Save(DocInitType type)
        {
            if (type == DocInitType.Default)
                Save();
            else if (type == DocInitType.References)
                DocumentReference.DBTable.Save(GetReferences().ToList());
            else if (type == DocInitType.Data)
                DocumentData.DBTable.Save(GetDatas().ToList());
            else if (type == DocInitType.Workflow)
                DocumentWork.DBTable.Save(GetWorks().ToList());
            else if (type == DocInitType.Customer)
                DocumentCustomer.DBTable.Save(GetCustomers().ToList());
        }

        [ControllerMethod]
        public void SaveComplex()
        {
            if (saving.Contains(this))//prevent recursion
                return;
            saving.Add(this);
            var transaction = DBTransaction.GetTransaction(this, Table.Schema.Connection);
            var param = new DocumentExecuteArgs() { Document = this, ProcedureCategory = Template.Code };
            try
            {
                var works = GetWorks().ToList();
                bool isnew = works.Count == 0;

                base.Save();

                if (isnew)
                {
                    var flow = Template.Work;
                    var work = Send(null, flow?.GetStartStage(), new[] { User.CurrentUser }).First();
                    base.Save();
                }

                var relations = Document.DBTable.GetChildRelations();
                foreach (var relation in relations)
                {
                    if (relation.Table != DocumentData.DBTable)
                    {
                        var references = GetReferencing(relation, DBLoadParam.None);
                        var updatind = new List<DBItem>();
                        foreach (DBItem reference in references)
                            if (reference.IsChanged)
                                updatind.Add(reference);
                        if (updatind.Count > 0)
                            relation.Table.Save(updatind);
                    }
                }
                if (isnew)//Templating
                {
                    var data = GetTemplatedData();
                    if (data != null)
                        data.Parse(param);
                }
                Save(DocInitType.Data);
                Saved?.Invoke(null, new DocumentEventArgs(this));
                if (transaction.Owner == this)
                    transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction.Owner == this)
                    transaction.Rollback();
                throw ex;
            }
            finally
            {
                if (transaction.Owner == this)
                    transaction.Dispose();
                saving.Remove(this);
            }
        }

        [ControllerMethod]
        public List<DocumentWork> Send()
        {
            var work = GetWorksUncompleted().FirstOrDefault();
            if (work == null)
            {
                throw new InvalidOperationException("No Actual works Found!");
            }
            if (work.Stage == null)
            {
                throw new InvalidOperationException("Stage on Work not Defined!");
            }
            var stageReference = work.Stage.GetNextReference();
            if (stageReference == null)
            {
                throw new InvalidOperationException("Next Stage not Defined!");
            }
            return Send(work, stageReference.ReferenceStage, stageReference.GetDepartment(Template));
        }

        [ControllerMethod]
        public object ExecuteProceduresByWork(DocumentWork work, StageParamProcudureType type)
        {
            if (work?.Stage == null)
                throw new ArgumentNullException();
            var param = new DocumentExecuteArgs { Document = this, Work = work, Stage = work.Stage };
            return ExecuteProcedures(param, work.Stage.GetProceduresByType(type));
        }

        [ControllerMethod]
        public object ExecuteProceduresByStage(Stage stage, StageParamProcudureType type)
        {
            var param = new DocumentExecuteArgs { Document = this, Stage = stage };
            return ExecuteProcedures(param, stage.GetProceduresByType(type));
        }

        [ControllerMethod]
        public void Complete(DocumentWork work)
        {
            if (work.User == null)
            {
                work.User = User.CurrentUser;
            }

            work.DateComplete = DateTime.Now;
            if (work.Stage != null)
            {
                if ((work.Stage.Keys & StageKey.IsAutoComplete) == StageKey.IsAutoComplete)
                {
                    foreach (var unWork in GetWorksUncompleted(work.Stage).ToList())
                    {
                        if (unWork.From == work.From)
                            work.DateComplete = work.DateComplete;
                    }
                }

                if (!work.IsResend
                    && GetWorksUncompleted(work.Stage).Count() == 0)
                {
                    var checkResult = ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Check);
                    if (checkResult != null)
                        throw new InvalidOperationException($"Check Fail {checkResult}");

                    ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Finish);
                }
            }
        }

        [ControllerMethod]
        public void Return(DocumentWork work)
        {
            if (work.From == null || work.From.Stage == null)
                throw new InvalidOperationException("Can't Return to undefined Stage");
            foreach (var unWork in GetWorksUncompleted().ToList())
            {
                if (unWork.From == work.From && unWork != work)
                {
                    unWork.DateComplete = DateTime.Now;
                    if (DBTransaction.Current != null)
                    {
                        DBTransaction.Current.Rows.Add(work);
                    }
                }
            }
            work.IsResend = true;
            Send(work, work.From.Stage, new[] { work.From.User });
        }

        public List<DocumentWork> Send(DocumentWork from, Stage stage, IEnumerable<DBItem> staff)
        {
            if (!(staff?.Any() ?? false))
            {
                throw new InvalidOperationException($"Destination not specified {stage}!");
            }

            if (from != null)
            {
                if (from.Stage == stage)
                {
                    from.IsResend = true;
                }
                Complete(from);
            }

            var result = new List<DocumentWork>();
            foreach (var item in staff)
            {
                if (!GetWorksUncompleted().Any(p => p.Stage == stage && p.Staff == stage))
                {
                    result.Add(CreateWork(from, stage, item));
                }
            }

            CheckComplete();

            if (stage != null)
            {
                ExecuteProceduresByStage(stage, StageParamProcudureType.Start);
            }

            return result;
        }

        public bool IsEdited()
        {
            if (UpdateState != DBUpdateState.Default)
                return true;
            var relations = Document.DBTable.GetChildRelations();
            foreach (var relation in relations)
            {
                foreach (DBItem row in GetReferencing(relation, DBLoadParam.None))
                    if (row.UpdateState != DBUpdateState.Default)
                        return true;
            }
            return false;
        }

        [ControllerMethod]
        public Document FindReference(Template template, bool create)
        {
            foreach (var refer in GetReferences())
            {
                if ((refer.Reference.Template == template && refer.Reference != this)
                    || (refer.Document.Template == template && refer.Document != this))
                    return refer.Reference;
            }
            if (create)
            {
                var newdoc = template.CreateDocument(this);
                newdoc.SaveComplex();
                return newdoc;
            }
            return null;
        }

        [ControllerMethod]
        public DocumentReference FindReference(Document document)
        {
            if (document == null || document == this)
                return null;
            foreach (var item in GetReferences())
            {
                if ((item.ReferenceId.Equals(document.Id)) || (item.DocumentId.Equals(document.Id)))
                    return item;
            }
            return null;
        }

        [ControllerMethod]
        public bool ContainsReference(Document document)
        {
            return FindReference(document) != null;
        }

        public void RefreshCache()
        {
            string workUsers = string.Empty;
            string workStages = string.Empty;
            DocumentWork current = null;
            var works = GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.None);
            foreach (DocumentWork dw in works)
            {
                var stage = dw.Stage?.ToString() ?? "none";
                var user = dw.User?.Name ?? "empty";
                if (!dw.IsComplete)
                {
                    if (workUsers.Length == 0 || workUsers.IndexOf(user, StringComparison.Ordinal) < 0)
                    {
                        workUsers = workUsers + user + " ";
                    }
                    if (workStages.Length == 0 || workStages.IndexOf(stage, StringComparison.Ordinal) < 0)
                    {
                        workStages = workStages + stage + " ";
                    }
                    if (dw.IsCurrent && (current == null || current.User == null))
                    {
                        current = dw;
                    }
                }
            }
            if (workStages.Length == 0)
                workStages = works.LastOrDefault()?.Stage?.ToString() ?? "none";
            if (workUsers.Length == 0)
                workUsers = works.LastOrDefault()?.User?.Name ?? "empty";
            WorkUser = workUsers;
            WorkStage = workStages;
            WorkCurrent = current;
        }

        public override string ToString()
        {
            return base.ToString();
            //return (Template == null ? "" : Template.ToString() + " ") + " № " + Code;
        }

        public void CheckComplete()
        {
            foreach (var work in GetWorks())
            {
                if (!work.IsComplete)
                {
                    IsComplete = false;
                    if (Status == DBStatus.Archive)
                    {
                        Status = DBStatus.Edit;
                    }
                    return;
                }
            }
            IsComplete = true;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    public enum DocumentSearchDate
    {
        Create,
        Document,
        WorkBegin,
        WorkEnd
    }
}
