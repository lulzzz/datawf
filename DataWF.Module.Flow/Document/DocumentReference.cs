﻿/*
 DocumentReference.cs
 
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
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public enum DocumentReferenceMode
    {
        None,
        Refed,
        Refing
    }

    public class DocumentReferenceList : DBTableView<DocumentReference>
    {
        private readonly Document doc;
        private readonly DocumentReferenceMode mode = DocumentReferenceMode.None;

        public DocumentReferenceList()
            : this("", DBViewKeys.None)
        { }

        public DocumentReferenceList(string filter, DBViewKeys mode)
            : base(filter, mode)
        {
        }

        public DocumentReferenceList(Document document, DocumentReferenceMode Mode)
            : this(Mode == DocumentReferenceMode.Refed
                   ? DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name + "=" + document.PrimaryId
                : Mode == DocumentReferenceMode.Refing
                   ? DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name + "=" + document.PrimaryId
                   : DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name + "=" + document.PrimaryId +
                   " or " + DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name + "=" + document.PrimaryId, DBViewKeys.Static)
        {
            this.doc = document;
            this.mode = Mode;
        }

        public Document Document
        {
            get { return doc; }
        }

        public DocumentReferenceMode Mode
        {
            get { return mode; }
        }

        public override int AddInternal(DocumentReference item)
        {
            if (doc != null)
            {
                if (mode == DocumentReferenceMode.Refed && item.Document == null)
                    item.Document = doc;
                else if (mode == DocumentReferenceMode.Refing && item.Reference == null)
                    item.Reference = doc;
            }
            return base.AddInternal(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && !item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Add(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && !item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Add(item);
            //}
        }

        public override bool Remove(DocumentReference item)
        {
            bool flag = base.Remove(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Remove(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Remove(item);
            //}

            return flag;
        }
    }

    [DataContract, Table("ddocument_reference", "Document", BlockSize = 400)]
    public class DocumentReference : DocumentDetail<DocumentReference>
    {
        private static DBColumn referenceKey = DBColumn.EmptyKey;

        public static DBColumn ReferenceKey => DBTable.ParseProperty(nameof(ReferenceId), ref referenceKey);

        private Document reference;

        public DocumentReference()
        {
        }
        [Browsable(false)]
        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_reference_unique", true)]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [Browsable(false)]
        [DataMember, Column("reference_id", Keys = DBColumnKeys.View)]
        [Index("ddocument_reference_unique", true)]
        public long? ReferenceId
        {
            get { return GetValue<long?>(ReferenceKey); }
            set { SetValue(value, ReferenceKey); }
        }

        [Reference(nameof(ReferenceId))]
        public Document Reference
        {
            get { return GetReference(ReferenceKey, ref reference); }
            set { reference = SetReference(value, ReferenceKey); }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            GetReference<Document>(Table.ParseProperty(nameof(Reference)), ref reference, DBLoadParam.None);
            if (Attached && reference != null)
            {
                reference.OnReferenceChanged(this);
            }
        }

    }
}
