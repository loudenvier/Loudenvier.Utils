using Dapper;
using System.Data.SqlClient;

namespace Loudenvier.Utils.Tests
{
    public class SqlExtensionsTests : IDisposable, IClassFixture<SqlExtensionsTests.SqlExtensionsTestsFixture> {
        private readonly SqlExtensionsTestsFixture _sqlExtensionsTestsFixture;

        public SqlExtensionsTests(SqlExtensionsTestsFixture sqlExtensionsTestsFixture) {
            _sqlExtensionsTestsFixture = sqlExtensionsTestsFixture;
        }

        const string ConnectionStringTestDb = "Data Source=.\\SQLEXPRESS;Initial Catalog=SqlExtensionsTests;Persist Security Info=True;Integrated Security=SSPI;";
        const string TestDatabase = "SqlExtensionsTests";
        const string TestTable = "SqlExtensionsTable";

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose() {
            ClearTestData();
        }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        static void ClearTestData() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute($"""
                    USE {TestDatabase}
                    TRUNCATE TABLE {TestTable}
                    """);
        }

        [Fact]
        public void HasPrimaryKeyViolationWorks() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 2')");
            } catch (Exception e) { ex = e; }
            Assert.NotNull(ex);
            Assert.True(ex.HasPrimaryKeyViolation());
        }
        [Fact]
        public void HasPrimaryKeyViolationWorksWithInnerException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 2')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            Assert.True(ex.HasPrimaryKeyViolation());
        }

        [Fact]
        public void HasPrimaryKeyViolationWorksEvenWithConstraintViolationToo() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            } catch (Exception e) { ex = e; }
            Assert.NotNull(ex);
            Assert.True(ex.HasPrimaryKeyViolation());
        }
        [Fact]
        public void HasDuplicateKeyViolationWorks() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = e; }
            Assert.NotNull(ex);
            Assert.True(ex.HasDuplicateKeyViolation());
        }
        [Fact]
        public void HasDuplicateKeyViolationWorksWithInnerException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            Assert.True(ex.HasDuplicateKeyViolation());
        }
        [Fact]
        public void HasOneOfTheseErrorsWorks() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO BlaBlaBla VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = e; }
            Assert.NotNull(ex);
            Assert.True(ex.HasOneOfTheseSqlErrors(208)); // invalid object name
        }
        [Fact]
        public void HasOneOfTheseErrorsWorksWithInnerException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO BlaBlaBla VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            Assert.True(ex.HasOneOfTheseSqlErrors(208)); // invalid object name
        }
        [Fact]
        public void IsOneOfTheseErrorsWorks() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO BlaBlaBla VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = e; }
            Assert.NotNull(ex);
            Assert.True(ex.IsOneOfTheseSqlErrors(208)); // invalid object name
        }
        [Fact]
        public void IsOneOfTheseErrorsDoesntCheckInnerExceptions() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO BlaBlaBla VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            Assert.False(ex.IsOneOfTheseSqlErrors(208)); // invalid object name
        }

        [Fact]
        public void CanGetDuplicateKeyDataFromPrimaryKeyViolationException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 2')");
            } catch (Exception e) {
                var dupKeyData = DuplicateKeyData.FromException(e);
                Assert.NotNull(dupKeyData);
                Assert.True(dupKeyData.IsPrimaryKeyViolation);
                Assert.Equal("1", dupKeyData.Value);
                Assert.Equal($"dbo.{TestTable}", dupKeyData.Object);
            }
        }
        [Fact]
        public void CanGetDuplicateKeyDataFromPrimaryKeyViolationInnerException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 2')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            var dupKeyData = DuplicateKeyData.FromException(ex);
            Assert.NotNull(dupKeyData);
            Assert.True(dupKeyData.IsPrimaryKeyViolation);
            Assert.Equal("1", dupKeyData.Value);
            Assert.Equal($"dbo.{TestTable}", dupKeyData.Object);
        }

        [Fact]
        public void CanGetDuplicateKeyDataFromDuplicateKeyViolationException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (2, 'Name 1')");
            } catch (Exception e) {
                var dupKeyData = DuplicateKeyData.FromException(e);
                Assert.NotNull(dupKeyData);
                Assert.False(dupKeyData.IsPrimaryKeyViolation);
                Assert.Equal("Name 1", dupKeyData.Value);
                Assert.Equal($"dbo.{TestTable}", dupKeyData.Object);
            }
        }
        [Fact]
        public void CanGetDuplicateKeyDataFromDuplicateKeyViolationInnerException() {
            using var conn = new SqlConnection(ConnectionStringTestDb);
            conn.Execute("INSERT INTO SqlExtensionsTable VALUES (1, 'Name 1')");
            Exception? ex = null;
            try {
                conn.Execute("INSERT INTO SqlExtensionsTable VALUES (2, 'Name 1')");
            } catch (Exception e) { ex = new Exception("Test", e); }
            Assert.NotNull(ex);
            var dupKeyData = DuplicateKeyData.FromException(ex);
            Assert.NotNull(dupKeyData);
            Assert.False(dupKeyData.IsPrimaryKeyViolation);
            Assert.Equal("Name 1", dupKeyData.Value);
            Assert.Equal($"dbo.{TestTable}", dupKeyData.Object);
            Assert.Equal("IX_SqlExtensionsUnique", dupKeyData.IndexOrConstraint);
        }

        public class SqlExtensionsTestsFixture : IDisposable
        {
            const string ConnectionString = "Data Source=.\\SQLEXPRESS;Persist Security Info=True;Integrated Security=SSPI;";
            public SqlExtensionsTestsFixture() => CreateDatabase();
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            public void Dispose() => DropDatabase(); // actually does nothing now.. 
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

            static void CreateDatabase() {
                const string CreateDBIfNeeded = $"""
                USE master	
                IF DB_ID(N'{TestDatabase}') IS NULL 
                    CREATE DATABASE {TestDatabase}
                """;
                const string CreateSchemaIfNeeded = $"""
                USE {TestDatabase}
                IF OBJECT_ID('[{TestTable}]', 'U') IS NULL
                BEGIN
                    CREATE TABLE {TestTable} (
                	    Id BIGINT NOT NULL PRIMARY KEY CLUSTERED,
                	    Name NVARCHAR(300) NOT NULL
                    )
                    CREATE UNIQUE NONCLUSTERED INDEX IX_SqlExtensionsUnique
                	    ON {TestTable} (Name)
                END
                """;

                using var conn = new SqlConnection(ConnectionString);
                conn.Execute(CreateDBIfNeeded);
                conn.Execute(CreateSchemaIfNeeded);
            }

            static void DropDatabase() {
                // won't be doing any dropping, just clearing test data
                /*const string DropDBIfNeeded = $"""
                USE master
                IF DB_ID(N'{TestDatabase}') IS NOT NULL 
                    DROP DATABASE {TestDatabase}
                """;
                using var conn = new SqlConnection(ConnectionString);
                conn.Execute(DropDBIfNeeded);*/
            }
        }
    }

    // separating tests that don't need actual database access here
    public class DuplicateKeyDataTests {
        const string PKViolation = "Violation of PRIMARY KEY constraint 'PK__SqlExten__3214EC07E7E723EE'. Cannot insert duplicate key in object 'dbo.SqlExtensionsTable'. The duplicate key value is (1)";
        const string DKViolation = "Cannot insert duplicate key row in object 'dbo.SqlExtensionsTable' with unique index 'IX_SqlExtensionsUnique'. The duplicate key value is (Name 1).";

        [Fact] 
        public void TryParseReturnsTrueForExpectedPrimaryKeyViolation() { 
            Assert.True(DuplicateKeyData.TryParse(SqlErrors.PRIMARY_KEY_VIOLATION, PKViolation, out var _));
        }
        [Fact]
        public void IsPrimaryKeyViolationIsTrueForPKViolationMessage() {
            var data = new DuplicateKeyData(SqlErrors.PRIMARY_KEY_VIOLATION, PKViolation);
            Assert.True(data.IsPrimaryKeyViolation);
        }
        [Fact]
        public void DuplicateKeyDataIsCorrectForPrimaryKeyViolation() {
            var data = new DuplicateKeyData(SqlErrors.PRIMARY_KEY_VIOLATION, PKViolation);
            Assert.Equal("PK__SqlExten__3214EC07E7E723EE", data.IndexOrConstraint);
            Assert.Equal("dbo.SqlExtensionsTable", data.Object);
            Assert.Equal("1", data.Value);
        }

        [Fact]
        public void TryParseReturnsTrueForExpectedDuplicateKeyViolation() {
            Assert.True(DuplicateKeyData.TryParse(SqlErrors.UNIQUE_CONSTRAINT_VIOLATION, DKViolation, out var _));
        }
        [Fact]
        public void IsPrimaryKeyViolationIsFalseForDKViolationMessage() {
            var data = new DuplicateKeyData(SqlErrors.UNIQUE_CONSTRAINT_VIOLATION, DKViolation);
            Assert.False(data.IsPrimaryKeyViolation);
        }
        [Fact]
        public void DuplicateKeyDataIsCorrectForDuplicateKeyViolation() {
            var data = new DuplicateKeyData(SqlErrors.UNIQUE_CONSTRAINT_VIOLATION, DKViolation);
            Assert.Equal("IX_SqlExtensionsUnique", data.IndexOrConstraint);
            Assert.Equal("dbo.SqlExtensionsTable", data.Object);
            Assert.Equal("Name 1", data.Value);
        }
    }
}