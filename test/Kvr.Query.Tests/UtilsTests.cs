using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kvr.Dapper;

namespace Kvr.Query.Tests;

/// <summary>
/// Tests for the Utils class.
/// </summary>
public class UtilsTests
{
    #region Test Models
    public class EntityWithKeyAttribute
    {
        [Key]
        public int TestId { get; set; }
    }

    public class EntityWithConventionalId
    {
        public int Id { get; set; }
    }

    public class EntityWithTypedId
    {
        public int EntityWithTypedIdId { get; set; }
    }

    // One-to-Many with ForeignKey on navigation
    public class Parent
    {
        public int Id { get; set; }
        
        [InverseProperty("Parent")]
        public ICollection<Child> Children { get; set; }
    }

    public class Child
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        
        [ForeignKey("ParentId")]
        public Parent Parent { get; set; }
    }

    // One-to-Many with ForeignKey on property
    public class Order
    {
        public int Id { get; set; }
        public ICollection<OrderLine> Lines { get; set; }
    }

    public class OrderLine
    {
        public int Id { get; set; }
        
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order Order { get; set; }
    }

    // One-to-Many with convention naming
    public class Category
    {
        public int Id { get; set; }
        public ICollection<Product> Products { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }

    // One-to-One relationships
    public class Customer
    {
        public int Id { get; set; }
        
        [ForeignKey("Address")]
        public int AddressId { get; set; }
        public CustomerAddress Address { get; set; }
    }

    public class CustomerAddress
    {
        public int Id { get; set; }
        public Customer Customer { get; set; }
    }
    #endregion

    #region GetPrimaryKeyExpression Tests
    [Fact]
    public void GetPrimaryKeyExpression_WithKeyAttribute_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetPrimaryKeyExpression<EntityWithKeyAttribute>();
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("TestId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetPrimaryKeyExpression_WithConventionalId_ShouldReturnIdProperty()
    {
        // Act
        var expression = Utils.GetPrimaryKeyExpression<EntityWithConventionalId>();
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("Id", memberExpression.Member.Name);
    }

    [Fact]
    public void GetPrimaryKeyExpression_WithTypedId_ShouldReturnTypedIdProperty()
    {
        // Act
        var expression = Utils.GetPrimaryKeyExpression<EntityWithTypedId>();
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("EntityWithTypedIdId", memberExpression.Member.Name);
    }
    #endregion

    #region GetForeignKeyExpression (Single Navigation) Tests
    [Fact]
    public void GetForeignKeyExpression_WithForeignKeyOnNavigation_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Child, Parent>(c => c.Parent);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("ParentId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetForeignKeyExpression_WithForeignKeyOnProperty_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<OrderLine, Order>(ol => ol.Order);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("OrderId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetForeignKeyExpression_WithConventionNaming_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Product, Category>(p => p.Category);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("CategoryId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetForeignKeyExpression_OneToOne_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Customer, CustomerAddress>(c => c.Address);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("AddressId", memberExpression.Member.Name);
    }
    #endregion

    #region GetForeignKeyExpression (Collection Navigation) Tests
    [Fact]
    public void GetForeignKeyExpression_Collection_WithInverseProperty_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Parent, Child>(p => p.Children);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("ParentId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetForeignKeyExpression_Collection_WithConventionNaming_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Category, Product>(c => c.Products);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("CategoryId", memberExpression.Member.Name);
    }

    [Fact]
    public void GetForeignKeyExpression_Collection_WithForeignKeyAttribute_ShouldReturnCorrectProperty()
    {
        // Act
        var expression = Utils.GetForeignKeyExpression<Order, OrderLine>(o => o.Lines);
        
        // Assert
        Assert.NotNull(expression);
        var memberExpression = expression.GetMemberExpression();
        Assert.Equal("OrderId", memberExpression.Member.Name);
    }
    #endregion
} 