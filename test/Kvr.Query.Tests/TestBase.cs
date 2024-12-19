using System.Data;
using Dapper;
using Kvr.SqlBuilder.Convention;
using Microsoft.Data.Sqlite;

namespace Kvr.Query.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IDbConnection Connection;

        protected TestBase()
        {
            SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create().UsePluralTableNames());
            Connection = CreateInMemoryDatabase();
            SetupDatabase();
        }

        private IDbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        protected virtual void SetupDatabase()
        {
            // Create tables
            Connection.Execute(@"
                -- Create Categories table first as it's referenced by Orders
                CREATE TABLE Categories (
                    CategoryId INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL
                );

                -- Create CustomerAddresses table first as it's referenced by Customers
                CREATE TABLE CustomerAddresses (
                    AddressId INTEGER PRIMARY KEY,
                    Street TEXT NOT NULL,
                    City TEXT NOT NULL
                );

                -- Create Customers table with foreign key to CustomerAddresses
                CREATE TABLE Customers (
                    Id INTEGER PRIMARY KEY,
                    CustomerId INTEGER NOT NULL,
                    AddressId INTEGER,
                    Name TEXT NOT NULL,
                    FOREIGN KEY (AddressId) REFERENCES CustomerAddresses(AddressId)
                );

                -- Create Orders table with foreign keys to both Customers and Categories
                CREATE TABLE Orders (
                    OrderId INTEGER PRIMARY KEY,
                    CustomerId INTEGER NOT NULL,
                    CategoryId INTEGER,
                    OrderDate TEXT NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
                );

                -- Create OrderDetails table with foreign key to Orders
                CREATE TABLE OrderDetails (
                    DetailId INTEGER PRIMARY KEY,
                    OrderId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
                );");

            // Seed initial test data
            Connection.Execute(@"
                -- Insert Categories
                INSERT INTO Categories (CategoryId, Name) 
                VALUES (1, 'Test Category');

                -- Insert CustomerAddresses
                INSERT INTO CustomerAddresses (AddressId, Street, City)
                VALUES (1, 'Test Street', 'Test City');

                -- Insert Customers with AddressId
                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (1, 1, 1, 'Test Customer');

                -- Insert Orders with CategoryId
                INSERT INTO Orders (OrderId, CustomerId, CategoryId, OrderDate, Amount) 
                VALUES (1, 1, 1, '2024-01-01', 100.00);

                -- Insert OrderDetails
                INSERT INTO OrderDetails (DetailId, OrderId, ProductName, Quantity)
                VALUES (1, 1, 'Test Product', 5);");
        }

        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
} 