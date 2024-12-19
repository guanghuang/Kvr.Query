# 🔄 Kvr.Query

Copyright © 2025 Kvr.Query. All rights reserved.

A lightweight extension for Dapper that provides type-safe querying of related entities with support for one-to-many and one-to-one relationships.

## 📑 Table of Contents
- ✨ [Features](#-features)
- 📦 [Installation](#-installation)
- 🚀 [Quick Start Guide](#-quick-start-guide)
- 📚 [Usage](#-usage)
  - 🔌 [Extension Methods](#-extension-methods-to-start-a-query-for-a-specific-entities)
  - 🔗 [Fluent API](#-fluent-api-to-include-related-entities)
  - 🔄 [Automatic Deduplication](#-automatic-remove-duplication-children-entities)
  - 🔑 [Key Detection](#-primary-key-and-foreign-key-detection-order)
- 💡 [Best Practices](#-best-practices)
- ⚠️ [Limitations](#-limitations)
- 🔧 [Supported Frameworks](#-supported-frameworks)
- 📝 [Version History](#-version-history)
- 📄 [License](#-license)
- 🤝 [Contributing](#-contributing)
- 📦 [Dependencies](#-dependencies)
- 💬 [Support](#-support)

## ✨ Features
- 🔄 Fluent API for querying related entities
- 🔑 Automatic key detection (primary & foreign)
- 🔗 Support for one-to-many and one-to-one relationships
- 📦 Nested relationship querying (ThenInclude)
- 🛡️ Type-safe property selection
- 🔍 WHERE and ORDER BY clause support
- 📝 Minimal boilerplate code
- 🏷️ Support for Data Annotations

## 📦 Installation

You can install the package via NuGet Package Manager:

```
dotnet add package Kvr.Query
```

## 🚀 Quick Start Guide

Here is a quick start guide to get you up and running with Kvr.Query.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int AddressId { get; set; }
    public Address Address { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; }
    public User User { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}

public class Address
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Street { get; set; }
    public User User { get; set; }
}

public class PaymentMethod
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Method { get; set; }
    public Order Order { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Description { get; set; }
    public Order Order { get; set; }
}

// Query all orders for a user with id 1
// Orders include User, PaymentMethod and OrderItems
// User includes Address
var orders = await connection.Select<Order>()
    .IncludeOne(o => o.User)
        .ThenIncludeOne(u => u.Address)
        .ThenIncludeOne(u => u.UserProfile)
    .IncludeOne(o => o.PaymentMethod)
    .IncludeMany(o => o.OrderItems)
    .Where<User>(u => u.Name, "@userId")
    .QueryAsync(new { userId = 1 });
```
The above query will return all orders for a user with id 1, including the user's address and payment method, and the order items.

It will use primary keys and foreign keys conventions to detect the relationships.

If you want to use attributes to define the relationships, you can use the `Key`, `ForeignKey`, `InverseProperty` attributes same as in Entity Framework.

Or you could use expressions to define the relationships.

```csharp
var orders = await connection.Select<Order>(o => o.Id)
    .Include(o => o.User, o => o.UserId, u => u.Id)
        .ThenInclude(u => u.Address, u => u.AddressId, a => a.Id)
        .ThenInclude(u => u.UserProfile, u => u.UserProfileId, up => up.Id)
    .Include(o => o.PaymentMethod, o => o.PaymentMethodId, p => p.Id)
    .Include(o => o.OrderItems, oi => oi.OrderId, i => i.Id)
    .Where<User>(u => u.Name, "@userId")
    .QueryAsync(new { userId = 1 });
```

## 🔄 Usage
### 🚀 Extension Methods to start a query for a specific entities
- 📝 `IDbConnection.Select<T>()` - Creates a query for an entity
- 🔍 `IDbConnection.Select<T>(Expression<Func<T, object>> keySelector)` - Creates a query with a specific key selector
    - 🔗 `keySelector`: The key selector to use for the query.
### 🔄 Fluent API to include related entities
- 📊 One-to-many relationships
    - `IncludeMany<TChild>(Expression<Func<TParent, ICollection<TChild>>> navigationProperty, Expression<Func<TChild, object>>? foreignKeyProperty = null, Expression<Func<TChild, object>>? childPrimaryKeyProperty = null, Expression<Func<TChild, object>>[]? excludeColumns = null)`
        - 🔗 `navigationProperty`: The navigation property to include.
        - 🔑 `foreignKeyProperty`: Optional. The foreign key property to use for the relationship. If not provided, the foreign key property will be detected by convention and attribute.
        - 🔐 `childPrimaryKeyProperty`: Optional. The primary key property to use for the relationship. If not provided, the primary key property will be detected by convention and attribute.
        - ❌ `excludeColumns`: Optional. The columns to exclude from the query, normally not needed, but can be used to exclude columns from the query.
- 🔄 One-to-one or many-to-one relationships
    - 📥 `IncludeOne<TChild>(Expression<Func<TParent, TChild>> navigationProperty, Expression<Func<TParent, object>>? navigationForeignKeyProperty = null,  Expression<Func<TChild, object>>? childPrimaryKeyProperty = null, bool includeNavigationKeyInSql = false, Expression<Func<TChild, object>>[]? excludeColumns = null)`
        - 🔗 `navigationProperty`: The navigation property to include.
        - 🔑 `navigationForeignKeyProperty`: Optional. The foreign key property to use for the relationship. If not provided, the foreign key property will be detected by convention and attribute.
        - 🔐 `childPrimaryKeyProperty`: Optional. The primary key property to use for the relationship. If not provided, the primary key property will be detected by convention and attribute.
        - ⚡ `includeNavigationKeyInSql`: Optional. If true, the navigation key will be included in the SQL query.
        - ❌ `excludeColumns`: Optional. The columns to exclude from the query, normally not needed, but can be used to exclude columns from the query.
- 🌳 Nested relationships
   - ⚠️ only supports 2 levels of nested relationships.
   - 1️⃣ first level is defined by using `IncludeOne` one-to-one relationship method or `IncludeMany` one-to-many relationship method.
   - 2️⃣ second level only supports `ThenIncludeOne` one-to-one relationship method.
   - 🔄 could include multiple `ThenIncludeOne` methods to define same level of nested relationship to one entity (e.g. `ThenInclude(u => u.UserProfile).ThenInclude(up => up.User)`)
   - ℹ️ It is different from EF, it does not support `ThenIncludeOne` to higher level nested relationships. And not needed to call `Include` method to return to parent entity then call `ThenInclude` method to include another child entity.
   - `ThenIncludeOne<TGrandChild>(Expression<Func<TChild, TGrandChild>> navigationProperty, Expression<Func<TChild, object>>? navigationKeyProperty = null, Expression<Func<TGrandChild, object>>? grandChildPrimaryKeyProperty = null)`
     - 🔗 `navigationProperty`: The navigation property to include.
     - 🔑 `navigationKeyProperty`: Optional. The foreign key property to use for the relationship. If not provided, the foreign key property will be detected by convention and attribute.
     - 🔐 `grandChildPrimaryKeyProperty`: Optional. The primary key property to use for the relationship. If not provided, the primary key property will be detected by convention and attribute.  

```csharp
// Entity Framework Include and ThenInclude
var orders = await context.Orders
    .Include(o => o.User)
        .ThenInclude(u => u.Address)
    .Include(o => o.User)
        .ThenInclude(a => a.UserProfile)
    ....

// Kvr.Query IncludeOne/IncludeMany and ThenIncludeOne
var orders = await connection.Select<Order>()
    .IncludeOne(o => o.User)
        .ThenIncludeOne(u => u.Address)
        .ThenIncludeOne(u => u.UserProfile)
    ....
```
### 🔄 Automatic remove duplication children entities 
- 🔀 If an entity includes multiple `IncludeMany` methods, the cartesian cross product of child entities will be returned from `Dapper` query (e.g. If order has multiple `OrderItems` and multiple `PaymentMethods`, the query will return the cartesian cross product of `OrderItems` and `PaymentMethods`).
- ♻️ `QueryAsync` method will automatically remove duplication children entities using `Distinct` method by primary key property.

### 🔍 Primary Key and Foreign Key Detection order
- 💻 Expressions to define primary key and foreign key properties on `IncludeOne`, `IncludeMany`, `ThenIncludeOne` methods:
- 🏷️ Attribute to define the primary key and foreign key properties entities:
    - 🔑 `Key` attribute is used to define the primary key.
    - 🔗 `ForeignKey` attribute is used to define the foreign key.
    - ↩️ `InverseProperty` attribute is used to define the inverse property.
- 📝 Convention to define the primary key and foreign key properties on entities:  
    - 🔑 Primary key property fields:
        - `Id`. 
        - `$"{typeof(T).Name}Id"` is used as the primary key.
    - 🔗 Foreign key property fields:
    - `$"{navigationPropertyInfo.Name}Id"`.
    - `$"{navigationPropertyInfo.PropertyType.Name}Id"` is used as the foreign key.
- 🔄 Please see the test case to detect the primary key and foreign key properties: [UtilsTests.cs](https://github.com/guanghuang/Kvr.Query/blob/main/test/Kvr.Query.Tests/UtilsTests.cs)
## 🌟 Best Practices
- 🔄 Use conventions to automatically detect the primary keys and foreign keys in relationship navigation properties.
- 🏷️ Use attributes to define the relationships if you want to use Data Annotations.
    - 🔑 `Key` attribute is used to define the primary key.
    - 🔗 `ForeignKey` attribute is used to define the foreign key.
    - ↩️ `InverseProperty` attribute is used to define the inverse property.
- ⚡ Use expression to define keys explicitly.

## 🚫 Limitations
- Only supports 2 levels of relationships.
    - First level relationships are supported by using `IncludeOne` method and `IncludeMany` method. 
    - Second level relationships are only supported by using `ThenIncludeOne` method.
- Only includes the same navigation property once, not detection for duplicate navigation properties.
- `Key` and `ForeignKey` attributes only support one property, not supporting composite keys.
- Lazy loading is not supported, all requests for related entities are eager loading.

## 🔧 Supported Frameworks
- .NET Standard 2.0+
- .NET 5.0+
- .NET 6.0+
- .NET 7.0+

## 📝 Version History
- 1.0.0
    - Initial release with support for one-to-many and one-to-one relationships by using conventions, attributes and expressions.

## 📄 License
Apache License 2.0 - see [LICENSE](LICENSE)

## 🤝 Contributing
Contributions welcome! Please read our [Contributing Guide](CONTRIBUTING.md)

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📦 Dependencies
- [DapperRelMapper](https://github.com/guanghuang/DapperRelMapper) - Relationship mapping extension for Dapper
    - [Dapper](https://github.com/StackExchange/Dapper)
- [SqlBuilder](https://github.com/guanghuang/SqlBuilder)
    - [System.ComponentModel.Annotations](https://github.com/dotnet/runtime/tree/main/src/libraries/System.ComponentModel.Annotations)

## 💬 Support
If you encounter any issues or have questions, please file an issue on the GitHub repository.


## 🏗️ Build Status
![Build and Test](https://github.com/guanghuang/Kvr.Query/actions/workflows/build.yml/badge.svg)
![Publish to NuGet](https://github.com/guanghuang/Kvr.Query/actions/workflows/publish.yml/badge.svg)
