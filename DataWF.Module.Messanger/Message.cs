﻿/*
 Message.cs
 
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
using DataWF.Common;
using System;
using System.ComponentModel;
using DataWF.Module.Common;

namespace DataWF.Module.Messanger
{
	public class MessageList : DBTableView<Message>
	{
		public MessageList(string filter)
			: base(Message.DBTable, filter)
		{
			//_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
		}

		public MessageList()
			: this(string.Empty)
		{ }

		public MessageList(User fromUser, User user)
			: this(string.Format("({1} = {2} and {0} in (select {3} from {4} where {5} = {6})) or ({1} = {6} and {0} in(select {3} from {4} where {5} = {2}))",
								 Message.DBTable.PrimaryKey.Name,
								 Message.DBTable.ParseProperty(nameof(Message.UserId)).Name,
								 user.Id,
								 MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name,
								 MessageAddress.DBTable.Name,
								 MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.UserId)).Name,
								 fromUser.Id))
		{ }

		public MessageList(DBItem document)
			: this(string.Format("{0} = {1} and {2} = {3}",
								 MessageAddress.DBTable.ParseProperty(nameof(Message.DocumentTable)).Name, document.Table.FullName,
								 MessageAddress.DBTable.ParseProperty(nameof(Message.DocumentId)).Name, document.PrimaryId))
		{ }
	}

	[Table("wf_message", "dmessage")]
	public class Message : DBItem, IDisposable
	{
		public static DBTable<Message> DBTable
		{
			get { return DBService.GetTable<Message>(); }
		}

		private MessageAddressList addresses;
		private DBItem cacheDocument;

		public static Message SendToGroup(int from, int to, string data, DBItem document, DBTransaction transaction)
		{
			var message = new Message
			{
				Date = DateTime.Now,
				UserId = from,
				Data = data,
				Document = document
			};
			message.Save(transaction);

			var group = UserGroup.DBTable.LoadById(to);
			var users = group.GetUsers();
			foreach (var user in users)
				if (user.Status == DBStatus.Actual && user.UserType == UserTypes.Persone)
				{
					var address = new MessageAddress();
					address.Message = message;
					address.User = user;
					address.Save(transaction);
				}
			return message;
		}

		public static Message Send(int? from, int? to, string data, DBItem document, DBTransaction transaction)
		{
			var message = new Message()
			{
				Date = DateTime.Now,
				UserId = from,
				Data = data,
				Document = document
			};
			message.Save(transaction);

			var address = new MessageAddress()
			{
				Message = message,
				UserId = to
			};
			address.Save(transaction);
			if (address.User.IsCompaund)
			{
				foreach (var user in address.User.GetSubGroups<User>(DBLoadParam.None))
				{
					if (user != message.User && user.Status == DBStatus.Actual && user.UserType == UserTypes.Persone)
					{
						var saddress = new MessageAddress()
						{
							Message = message,
							User = user
						};
						saddress.Save(transaction);
					}
				}
			}
			return message;
		}

		public Message()
		{
			Build(DBTable);
		}

		[Column("unid", Keys = DBColumnKeys.Primary)]
		public int? Id
		{
			get { return GetProperty<int?>(nameof(Id)); }
			set { SetProperty(value, nameof(Id)); }
		}

		[Browsable(false), Column("document_table")]
		public string DocumentTable
		{
			get { return GetProperty<string>(nameof(DocumentTable)); }
			set { SetProperty(value, nameof(DocumentTable)); }
		}

		[Browsable(false)]
		public DBTable DocumentDBTable
		{
			get { return DBService.ParseTable(DocumentTable); }
		}

		[Browsable(false), Column("documentid")]
		public string DocumentId
		{
			get { return GetProperty<string>(nameof(DocumentId)); }
			set { SetProperty(value, nameof(DocumentId)); }
		}

		public DBItem Document
		{
			get { return cacheDocument ?? (cacheDocument = DocumentDBTable?.LoadItemById(DocumentId)); }
			set
			{
				cacheDocument = value;
				DocumentTable = cacheDocument?.Table.FullName;
				DocumentId = cacheDocument?.PrimaryId.ToString();
			}
		}

		[Browsable(false), Column("userid")]
		public int? UserId
		{
			get { return GetProperty<int?>(nameof(UserId)); }
			set { SetProperty(value, nameof(UserId)); }
		}

		[Reference("fk_dmessage_userid", nameof(UserId))]
		public User User
		{
			get { return GetPropertyReference<User>(nameof(UserId)); }
			set { SetPropertyReference(value, nameof(UserId)); }
		}

		[Column("data")]
		public string Data
		{
			get { return GetProperty<string>(nameof(Data)); }
			set { SetProperty(value, nameof(Data)); }
		}

		public MessageAddressList Addresses
		{
			get
			{
				if (addresses == null && Id != null)
					addresses = new MessageAddressList(this);
				return addresses;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			addresses?.Dispose();
		}
	}
}
