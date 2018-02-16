﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;

namespace DataWF.Test.Data
{
    [TestFixture]
    public class TestORM
    {
        private const string SchemaName = "test";
        private const string EmployerTableName = "employer";
        private const string PositionTableName = "employer_position";
        private DBSchema schema;

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DBService.Schems.Clear();
            DBService.ClearChache();

            if (DBService.Connections.Count == 0)
                Serialization.Deserialize("connections.xml", DBService.Connections);

            AccessItem.Groups = new List<IAccessGroup> {
                new AccessGroupBung() { Id = 1, Name = "Group1", IsCurrent = true },
                new AccessGroupBung() { Id = 2, Name = "Group2", IsCurrent = true },
                new AccessGroupBung() { Id = 3, Name = "Group3", IsCurrent = true }
            };
        }

        [Test]
        public void GenerateSqlite()
        {
            var dbName = "test.sqlite";
            if (File.Exists(dbName))
                File.Delete(dbName);
            Debug.WriteLine($"Data Base {Path.GetFullPath(dbName)}");

            Generate(DBService.Connections["TestSqlLite"]);
        }

        [Test]
        public void GeneratePostgres()
        {
            Generate(DBService.Connections["TestPostgres"]);
        }

        [Test]
        public void GenerateOracle()
        {
            Generate(DBService.Connections["TestOracle"]);
        }

        [Test]
        public void GenerateMySql()
        {
            Generate(DBService.Connections["TestMySql"]);
        }

        [Test]
        public void GenerateMsSql()
        {
            Generate(DBService.Connections["TestMSSql"]);
        }

        public void Generate(DBConnection connection)
        {
            connection.CheckConnection();
            DBService.Generate(GetType().Assembly);

            schema = DBService.Schems[SchemaName];
            Assert.IsNotNull(schema, "Attribute Generator Fail. On Schema");
            Assert.IsNotNull(Employer.DBTable, "Attribute Generator Fail. On Employer Table");
            Assert.IsNotNull(Position.DBTable, "Attribute Generator Fail. On Position Table");

            var idColumn = Employer.DBTable.Columns["id"];
            Assert.IsNotNull(schema, "Attribute Generator Fail. On Column Employer Id");
            schema.Connection = connection;

            try { DBService.ExecuteQuery(connection, schema.FormatSql(DDLType.Drop), true, DBExecuteType.NoReader); }
            catch (Exception ex) { Debug.WriteLine(ex); }

            var textCreate = schema.FormatSql(DDLType.Create);

            DBService.ExecuteGoQuery(connection, textCreate, true);

            if (connection.Schema?.Length > 0)
            {
                if (connection.System == DBSystem.Oracle)
                    connection.User = SchemaName;
                else if (connection.System != DBSystem.SQLite)
                    connection.DataBase = SchemaName;
            }

            var ddlText = schema.FormatSchema();

            DBService.ExecuteGoQuery(schema.Connection, ddlText, true);
            var result = schema.GetTablesInfo(connection.Schema, EmployerTableName);
            Assert.IsTrue(result.Count == 1, "Generate Sql Table / Get Information Fail.");
            result = schema.GetTablesInfo(connection.Schema, PositionTableName);
            Assert.IsTrue(result.Count == 1, "Generate Sql Table / Get Information Fail.");
            //Insert
            var employer = new Employer()
            {
                Identifier = $"{1:8}",
                Lodar = true,
                Age = 40,
                Height = 180,
                LongId = 120321312321L,
                Weight = 123.12333F,
                DWeight = 123.1233433424434D,
                Salary = 231323.32M,
                Name = "Ivan",
                Access = new AccessValue(new[]
                {
                    new AccessItem() { Group = AccessItem.Groups.First(i => i.Id == 1), View = true },
                    new AccessItem() { Group = AccessItem.Groups.First(i => i.Id == 2), Admin = true },
                    new AccessItem() { Group = AccessItem.Groups.First(i => i.Id == 3), Create = true }
                })
            };
            Assert.AreEqual(employer.Type, EmployerType.Type2, "Default Value & Enum");

            employer.GenerateId();
            Assert.NotNull(employer.Id, "Id Generator Fail");

            employer.Save();
            var qresult = DBService.ExecuteQResult(schema.Connection, $"select * from {EmployerTableName}");
            Assert.AreEqual(1, qresult.Values.Count, "Insert sql Fail");
            Assert.AreEqual(employer.Id, qresult.Get(0, "id"), "Insert sql Fail Int");
            Assert.AreEqual(employer.Identifier, qresult.Get(0, "identifier"), "Insert sql Fail String");
            Assert.AreEqual((int?)employer.Type, qresult.Get(0, "typeid"), "Insert sql Fail Enum");
            Assert.AreEqual(employer.Age, qresult.Get(0, "age"), "Insert sql Fail Byte");
            Assert.AreEqual(employer.Height, qresult.Get(0, "height"), "Insert sql Fail Short");
            Assert.AreEqual(employer.LongId, qresult.Get(0, "longid"), "Insert sql Fail Long");
            Assert.AreEqual(employer.Weight, qresult.Get(0, "weight"), "Insert sql Fail Float");
            Assert.AreEqual(employer.DWeight, qresult.Get(0, "dweight"), "Insert sql Fail Double");
            Assert.AreEqual(employer.Salary, qresult.Get(0, "salary"), "Insert sql Fail Decimal");
            var lodar = qresult.Get(0, "lodar").ToString();
            Assert.IsTrue(lodar == "1" || lodar == "True", "Insert sql Fail Bool");
            Assert.IsInstanceOf<byte[]>(qresult.Get(0, "gaccess"), "Insert sql Fail Byte Array");
            var accessValue = new AccessValue((byte[])qresult.Get(0, "gaccess"));
            Assert.AreEqual(3, accessValue.Items.Count, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items[0].View, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items[1].Admin, "Insert sql Fail Byte Array");
            Assert.AreEqual(false, accessValue.Items[2].Delete, "Insert sql Fail Byte Array");

            Employer.DBTable.Clear();
            Assert.AreEqual(0, Employer.DBTable.Count, "Clear table Fail");

            //Insert Several
            Position.DBTable.Add(new Position() { Code = "1", Name = "First Position" });
            Position.DBTable.Add(new Position() { Code = "2", Name = "Second Position" });
            var position = new Position() { Id = 0, Code = "3", Name = "Group Position" };
            position.Attach();
            var sposition = new Position() { Code = "4", Parent = position, Name = "Sub Group Position" };
            sposition.Attach();

            //Select from internal Index
            Position.DBTable.Add(new Position() { Code = "t1", Name = "Null Index" });
            Position.DBTable.Add(new Position() { Code = "t2", Name = "Null Index" });
            Position.DBTable.Add(new Position() { Code = "t3", Name = "Null Index" });
            var nullIds = Position.DBTable.Select(Position.DBTable.PrimaryKey, null, CompareType.Is).ToList();
            Assert.AreEqual(6, nullIds.Count, "Select by null Fail");

            Position.DBTable.Save();
            Position.DBTable.Clear();
            var positions = Position.DBTable.Load();
            Assert.AreEqual(7, positions.Count, "Insert/Read several Fail");

            //GetById
            employer = Employer.DBTable.LoadById(1);
            Assert.IsNotNull(employer, "GetById Fail");
            position = Position.DBTable.LoadById(4);
            Assert.IsNotNull(position, "GetById Fail");
            //Update
            employer.Position = position;
            employer.Save();

            qresult = DBService.ExecuteQResult(schema.Connection, $"select * from {EmployerTableName}");
            Assert.AreEqual(4, qresult.Get(0, "positionid"), "Update sql Fail");


            DBService.ExecuteQuery(connection,
                                   @"create table test_table(
      id int primary key, 
      test_date date, 
      test_varchar varchar(512),
      test_numeric numeric(20,10))");

            result = schema.GetTablesInfo(connection.Schema, "test_table");
            schema.GenerateTables(result);
            var table = schema.Tables["test_table"] as DBTable<DBItem>;
            Assert.IsNotNull(table, "DBInformation Load Fail");

            table.Load();
            for (int i = 0; i < 1000; i++)
            {
                var row = table.New();
                row["id"] = i;
                row["test_date"] = DateTime.Now.AddDays(-i);
                row["test_varchar"] = "string value " + i;
                row["test_numeric"] = i / 1000M;
                table.Add(row);
            }
            table.Save();
        }

        public enum EmployerType
        {
            Type1,
            Type2,
            Type3,
        }

        [Table(SchemaName, PositionTableName)]
        public class Position : DBItem
        {
            public static DBTable<Position> DBTable
            {
                get { return DBService.GetTable<Position>(); }
            }

            public Position()
            {
                Build(DBTable);
            }

            [Column("id", Keys = DBColumnKeys.Primary)]
            public int? Id
            {
                get { return GetProperty<int?>(nameof(Id)); }
                set { SetProperty(value, nameof(Id)); }
            }

            [Column("code", 20, Keys = DBColumnKeys.Code | DBColumnKeys.Unique | DBColumnKeys.Indexing)]
            [Index("positioncode", true)]
            public string Code
            {
                get { return GetProperty<string>(nameof(Code)); }
                set { SetProperty(value, nameof(Code)); }
            }

            [Column("parentid", Keys = DBColumnKeys.Group)]
            public int? ParentId
            {
                get { return GetProperty<int?>(nameof(ParentId)); }
                set { SetProperty(value, nameof(ParentId)); }
            }

            [Reference("fk_position_positionid", nameof(ParentId))]
            public Position Parent
            {
                get { return GetPropertyReference<Position>(nameof(ParentId)); }
                set { SetPropertyReference(value, nameof(ParentId)); }
            }

            [Column("name", 200, Keys = DBColumnKeys.Culture)]
            public override string Name
            {
                get { return GetName("name"); }
                set { SetName("name", value); }
            }

            [Column("description")]
            public string Description
            {
                get { return GetProperty<string>(nameof(Description)); }
                set { SetProperty(value, nameof(Description)); }
            }
        }

        public class EmployerTable : DBTable<Employer>
        {

        }

        [Table(SchemaName, EmployerTableName)]
        public class Employer : DBItem
        {
            public static DBTable<Employer> DBTable
            {
                get { return DBService.GetTable<Employer>(); }
            }

            public Employer()
            {
                Build(DBTable);
            }

            [Column("id", Keys = DBColumnKeys.Primary)]
            public int? Id
            {
                get { return GetProperty<int?>(nameof(Id)); }
                set { SetProperty(value, nameof(Id)); }
            }

            [Column("identifier", 20, Keys = DBColumnKeys.Code)]
            [Index("employeridentifier", true)]
            public string Identifier
            {
                get { return GetProperty<string>(nameof(Identifier)); }
                set { SetProperty(value, nameof(Identifier)); }
            }

            [Column("positionid")]
            public int? PositionId
            {
                get { return GetProperty<int?>(nameof(PositionId)); }
                set { SetProperty(value, nameof(PositionId)); }
            }

            [Reference("fk_employer_positionid", nameof(PositionId))]
            public Position Position
            {
                get { return GetPropertyReference<Position>(nameof(PositionId)); }
                set { SetPropertyReference(value, nameof(PositionId)); }
            }

            [Column("typeid", Keys = DBColumnKeys.Type, Default = "1")]
            public EmployerType? Type
            {
                get { return GetProperty<EmployerType?>(nameof(Type)); }
                set { SetProperty(value, nameof(Type)); }
            }

            [Column("longid")]
            public long? LongId
            {
                get { return GetProperty<long?>(nameof(LongId)); }
                set { SetProperty(value, nameof(LongId)); }
            }

            [Column("height")]
            public short? Height
            {
                get { return GetProperty<short?>(nameof(Height)); }
                set { SetProperty(value, nameof(Height)); }
            }

            [Column("weight")]
            public float? Weight
            {
                get { return GetProperty<float?>(nameof(Weight)); }
                set { SetProperty(value, nameof(Weight)); }
            }

            [Column("dweight")]
            public double? DWeight
            {
                get { return GetProperty<double?>(nameof(DWeight)); }
                set { SetProperty(value, nameof(DWeight)); }
            }

            [Column("salary", 23, 3)]
            public decimal? Salary
            {
                get { return GetProperty<decimal?>(nameof(Salary)); }
                set { SetProperty(value, nameof(Salary)); }
            }

            [Column("age")]
            public byte? Age
            {
                get { return GetProperty<byte?>(nameof(Age)); }
                set { SetProperty(value, nameof(Age)); }
            }

            [Column("lodar")]
            public bool? Lodar
            {
                get { return GetProperty<bool?>(nameof(Lodar)); }
                set { SetProperty(value, nameof(Lodar)); }
            }

            [Column("name", 20, Keys = DBColumnKeys.Culture)]
            public override string Name
            {
                get { return GetName("name"); }
                set { SetName("name", value); }
            }
        }
    }

    public class AccessGroupBung : IAccessGroup
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsCurrent { get; set; }
    }
}