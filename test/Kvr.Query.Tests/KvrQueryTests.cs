using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Dapper;
using Kvr.Query.Tests.Models;

namespace Kvr.Query.Tests
{
    public class KvrQueryTests : TestBase
    {
        [Fact]
        public async Task QueryAsync_WithOneToManyRelationship_ShouldBuildCorrectQuery()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId);

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.Equal("Test Customer", customer.Name);
            Assert.NotEmpty(customer.Orders);
            Assert.Equal(100m, customer.Orders.First().Amount);
        }

        [Fact]
        public async Task QueryAsync_WithOneToOneRelationship_ShouldBuildCorrectQuery()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeOne( c => c.Address, c => c.Id,a => a.AddressId);

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.NotNull(customer.Address);
            Assert.Equal("Test Street", customer.Address.Street);
            Assert.Equal("Test City", customer.Address.City);
            Assert.Equal(customer.AddressId, customer.Address.AddressId);
        }

        [Fact]
        public async Task QueryAsync_WithBothRelationshipTypes_ShouldBuildCorrectQuery()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .IncludeOne(c => c.Address, c => c.CustomerId);

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.NotNull(customer.Address);
            Assert.NotEmpty(customer.Orders);
            Assert.Equal("Test Customer", customer.Name);
            Assert.Equal("Test Street", customer.Address.Street);
            Assert.Equal(100m, customer.Orders.First().Amount);
        }

        [Fact]
        public async Task QueryAsync_WithWhereClause_ShouldFilterResults()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .Where<Customer, string>(c => c.Name, "'Test Customer'");

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.Equal("Test Customer", customer.Name);
            Assert.NotEmpty(customer.Orders);
        }

        [Fact]
        public async Task QueryAsync_WithMultipleWhereConditions_ShouldFilterResults()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .Where<Customer, string>(c => c.Name, "'Test Customer'")
                .And<Customer, int>(c => c.CustomerId, "1");

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.Equal("Test Customer", customer.Name);
            Assert.Equal(1, customer.CustomerId);
        }

        [Fact]
        public async Task QueryAsync_WithOrderBy_ShouldSortResults()
        {
            // First add another customer for testing ordering
            Connection.Execute(@"
                INSERT INTO CustomerAddresses (AddressId, Street, City)
                VALUES (2, 'Another Street', 'Another City');

                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, 2, 'Another Customer');
                
                INSERT INTO Orders (OrderId, CustomerId, OrderDate, Amount) 
                VALUES (2, 2, '2024-01-02', 200.00);");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .OrderBy<Customer, string>(c => c.Name);

            // Act
            var customers = (await query.QueryAsync()).ToList();

            // Assert
            Assert.Equal(2, customers.Count);
            Assert.Equal("Another Customer", customers[0].Name);
            Assert.Equal("Test Customer", customers[1].Name);
        }

        [Fact]
        public async Task QueryAsync_WithOrderByDescending_ShouldSortResultsDescending()
        {
            // First add another customer for testing ordering
            Connection.Execute(@"
                INSERT INTO CustomerAddresses (AddressId, Street, City)
                VALUES (2, 'Another Street', 'Another City');

                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, 2, 'Another Customer');
                
                INSERT INTO Orders (OrderId, CustomerId, OrderDate, Amount) 
                VALUES (2, 2, '2024-01-02', 200.00);");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .OrderBy<Customer, string>(c => c.Name, ascOrder: false);

            // Act
            var customers = (await query.QueryAsync()).ToList();

            // Assert
            Assert.Equal(2, customers.Count);
            Assert.Equal("Test Customer", customers[0].Name);
            Assert.Equal("Another Customer", customers[1].Name);
        }

        [Fact]
        public async Task QueryAsync_WithWhereAndOrderBy_ShouldFilterAndSortResults()
        {
            // First add more test data
            Connection.Execute(@"
                INSERT INTO CustomerAddresses (AddressId, Street, City)
                VALUES (2, 'Another Street', 'Another City'),
                       (3, 'Third Street', 'Third City');

                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, 2, 'Another Test Customer'),
                       (3, 3, 3, 'Another Customer');
                
                INSERT INTO Orders (OrderId, CustomerId, OrderDate, Amount) 
                VALUES (2, 2, '2024-01-02', 200.00),
                       (3, 3, '2024-01-03', 300.00);");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)                
                .Where<Customer, string>(c => c.Name, "'%Test%'", "LIKE")
                .OrderBy<Customer, string>(c => c.Name);

            // Act
            var customers = (await query.QueryAsync()).ToList();

            // Assert
            Assert.Equal(2, customers.Count);
            Assert.Equal("Another Test Customer", customers[0].Name);
            Assert.Equal("Test Customer", customers[1].Name);
            Assert.All(customers, c => Assert.Contains("Test", c.Name));
        }

        [Fact]
        public async Task QueryAsync_WithThenIncludeOne_ShouldMapGrandChildEntities()
        {
            // Add test data for grandchild relationship
            Connection.Execute(@"
                INSERT INTO OrderDetails (DetailId, OrderId, ProductName, Quantity)
                VALUES (2, 1, 'Test Product', 5);");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .ThenIncludeOne<OrderDetail>(o => o.Detail, o => o.OrderId, d => d.OrderId);

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.NotEmpty(customer.Orders);
            var order = customer.Orders.First();
            Assert.NotNull(order.Detail);
            Assert.Equal("Test Product", order.Detail.ProductName);
            Assert.Equal(5, order.Detail.Quantity);
        }

        [Fact]
        public async Task QueryAsync_WithMultipleIncludes_ShouldMapAllRelationships()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId)
                .ThenIncludeOne<Category>(o => o.Category, o => o.CategoryId)
                .IncludeOne(c => c.Address, c => c.AddressId);
            
            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.NotNull(customer.Address);
            Assert.NotEmpty(customer.Orders);
            Assert.NotNull(customer.Orders.First().Category);
            Assert.Equal("Test Category", customer.Orders.First().Category.Name);
        }

        [Fact]
        public async Task QueryAsync_WithExcludeColumns_ShouldNotSelectExcludedColumns()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId, excludeColumns: new[] 
                { 
                    (Expression<Func<Order, object>>)(o => o.OrderDate) 
                });

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.NotEmpty(customer.Orders);
            Assert.Equal(default, customer.Orders.First().OrderDate);
        }
        
        [Fact]
        public async Task QueryAsync_WithCallbackAfterMapRow_ShouldExecuteCallback()
        {
            // Arrange
            var callbackExecuted = false;
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId);

            // Act
            var customers = await query.QueryAsync(callbackAfterMapRow: objects =>
            {
                callbackExecuted = true;
                Assert.NotNull(objects);
                Assert.True(objects.Length > 0);
                Assert.IsType<Customer>(objects[0]);
            });

            // Assert
            Assert.True(callbackExecuted);
        }

        [Fact]
        public async Task QueryAsync_WithTransaction_ShouldUseTransaction()
        {
            // Arrange
            using var transaction = Connection.BeginTransaction();
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .IncludeMany(c => c.Orders, o => o.CustomerId);

            try
            {
                // Act
                var customers = await query.QueryAsync(transaction: transaction);
                
                // Assert
                Assert.NotEmpty(customers);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [Fact]
        public async Task QueryAsync_WithoutIncludes_ShouldReturnBasicData()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId);

            // Act
            var customers = await query.QueryAsync();
            var customer = customers.FirstOrDefault();

            // Assert
            Assert.NotNull(customer);
            Assert.Equal("Test Customer", customer.Name);
            Assert.Equal(1, customer.Id);
        }

        [Fact]
        public async Task QueryAsync_WithWhereClause_WithoutIncludes_ShouldFilterResults()
        {
            // Add another customer
            Connection.Execute(@"
                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, null, 'Another Customer');");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .Where<Customer, string>(c => c.Name, "'Test Customer'");

            // Act
            var customers = await query.QueryAsync();

            // Assert
            Assert.Single(customers);
            Assert.Equal("Test Customer", customers.First().Name);
        }

        [Fact]
        public async Task QueryAsync_WithOrderBy_WithoutIncludes_ShouldSortResults()
        {
            // Add more customers
            Connection.Execute(@"
                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, null, 'Another Customer'),
                       (3, 3, null, 'Yet Another Customer');");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .OrderBy<Customer, string>(c => c.Name);

            // Act
            var customers = (await query.QueryAsync()).ToList();

            // Assert
            Assert.Equal(3, customers.Count);
            Assert.Equal("Another Customer", customers[0].Name);
            Assert.Equal("Test Customer", customers[1].Name);
            Assert.Equal("Yet Another Customer", customers[2].Name);
        }

        [Fact]
        public async Task QueryAsync_WithMultipleConditions_WithoutIncludes_ShouldFilterCorrectly()
        {
            // Add more customers
            Connection.Execute(@"
                INSERT INTO Customers (Id, CustomerId, AddressId, Name) 
                VALUES (2, 2, null, 'Test Customer 2'),
                       (3, 3, null, 'Another Customer');");

            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .Where<Customer, string>(c => c.Name, "'%Test%'", "LIKE")
                .And<Customer, int>(c => c.CustomerId, "1");

            // Act
            var customers = await query.QueryAsync();

            // Assert
            Assert.Single(customers);
            Assert.Equal("Test Customer", customers.First().Name);
            Assert.Equal(1, customers.First().CustomerId);
        }

        [Fact]
        public async Task QueryAsync_WithRawSql_WithoutIncludes_ShouldExecuteCorrectly()
        {
            // Arrange
            var query = Connection.Select<Customer>(c => c.CustomerId)
                .RawSql("WHERE Name LIKE '%Test%'");

            // Act
            var customers = await query.QueryAsync();

            // Assert
            Assert.NotEmpty(customers);
            Assert.All(customers, c => Assert.Contains("Test", c.Name));
        }

        [Fact]
        public async Task QueryAsync_WithTransaction_WithoutIncludes_ShouldUseTransaction()
        {
            // Arrange
            using var transaction = Connection.BeginTransaction();
            var query = Connection.Select<Customer>(c => c.CustomerId);

            try
            {
                // Act
                var customers = await query.QueryAsync(transaction: transaction);
                
                // Assert
                Assert.NotEmpty(customers);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    // Additional model classes for testing
    public class OrderDetail
    {
        public int DetailId { get; set; }
        public int OrderId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }

    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string Name { get; set; }
    }
} 