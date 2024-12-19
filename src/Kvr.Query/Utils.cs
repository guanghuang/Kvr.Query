// Copyright 2025 Kvr.Query
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Kvr.Dapper;

namespace Kvr.Query;

/// <summary>
/// Utility class for generating expressions from property names.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Converts a PropertyInfo to an Expression<Func<T, object>>.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <param name="propertyInfo">The PropertyInfo to convert</param>
    /// <returns>An Expression<Func<T, object>> or null if the PropertyInfo is null</returns>
    private static Expression<Func<T, object>>? ConvertPropertyInfoToExpression<T>(PropertyInfo? propertyInfo)
    {
        if (propertyInfo == null)
        {
            return null;
        }

        // Create parameter expression (x)
        var parameter = Expression.Parameter(typeof(T), "x");

        // Create property access (x.Property)
        var property = Expression.Property(parameter, propertyInfo);

        // Convert value types to object
        Expression body = propertyInfo.PropertyType.IsValueType ?
            Expression.Convert(property, typeof(object)) :
            property;

        return Expression.Lambda<Func<T, object>>(body, parameter);
    }
    
    /// <summary>
    /// Gets the property info with the specified attribute.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <typeparam name="TAttribute">The type of the attribute</typeparam>
    /// <returns>The PropertyInfo or null if not found</returns>
    private static PropertyInfo? GetPropertyWithAttribute<T, TAttribute>() where TAttribute : Attribute
    {
        return typeof(T).GetProperties()
            .FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(TAttribute)));
    }

    /// <summary>
    /// Gets the property info with the specified name.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <param name="propertyName">The name of the property</param>
    /// <returns>The PropertyInfo or null if not found</returns>
    private static PropertyInfo? GetPropertyWithName<T>(string propertyName)
    {
        return typeof(T).GetProperties()
            .FirstOrDefault(prop => string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets the primary key expression for a given type.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <returns>An Expression<Func<T, object>> or null if not found</returns>
    public static Expression<Func<T, object>>? GetPrimaryKeyExpression<T>()
    {
        // First try to find property with Key attribute
        var propertyInfo = GetPropertyWithAttribute<T, KeyAttribute>();
        
        if (propertyInfo == null)
        {
            // Then try common primary key naming conventions
            var conventionNames = new[] { "Id", $"{typeof(T).Name}Id" };
            foreach (var name in conventionNames)
            {
                propertyInfo = GetPropertyWithName<T>(name);
                if (propertyInfo != null) break;
            }
        }

        return ConvertPropertyInfoToExpression<T>(propertyInfo);
    }

    /// <summary>
    /// Gets the foreign key property info from a navigation property.
    /// </summary>
    /// <param name="type">The type containing the navigation property</param>
    /// <param name="navigationPropertyInfo">The navigation property info</param>
    /// <returns>PropertyInfo of the foreign key property</returns>
    private static PropertyInfo? GetForeignKeyPropertyInfo(Type type, PropertyInfo navigationPropertyInfo)
    {
        // Case 1: Check if the navigation property has ForeignKey attribute
        var foreignKeyAttrOnNavigation = navigationPropertyInfo.GetCustomAttribute<ForeignKeyAttribute>();
        if (foreignKeyAttrOnNavigation != null)
        {
            return type.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, foreignKeyAttrOnNavigation.Name, StringComparison.OrdinalIgnoreCase));
        }

        // Case 2: Check if any property has ForeignKey attribute pointing to this navigation
        var propertyWithAttribute = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttributes<ForeignKeyAttribute>()
                .Any(a => string.Equals(a.Name, navigationPropertyInfo.Name, StringComparison.OrdinalIgnoreCase)));
        
        if (propertyWithAttribute != null)
        {
            return propertyWithAttribute;
        }

        // Case 3: Try common foreign key naming conventions
        var conventionNames = new[]
        {
            $"{navigationPropertyInfo.Name}Id",
            $"{navigationPropertyInfo.PropertyType.Name}Id"
        };

        foreach (var name in conventionNames)
        {
            var property = type.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the foreign key expression from a navigation property.
    /// </summary>
    /// <typeparam name="T">The type containing the navigation property</typeparam>
    /// <typeparam name="TNavigation">The type of the navigation property</typeparam>
    /// <param name="navigationProperty">Expression representing the navigation property</param>
    /// <returns>Expression representing the foreign key property</returns>
    public static Expression<Func<T, object>>? GetForeignKeyExpression<T, TNavigation>(Expression<Func<T, TNavigation>> navigationProperty)
    {
        var memberExpression = navigationProperty.GetMemberExpression();
        var navigationPropertyInfo = typeof(T).GetProperty(memberExpression.Member.Name);
        
        if (navigationPropertyInfo == null)
        {
            return null;
        }

        var foreignKeyPropertyInfo = GetForeignKeyPropertyInfo(typeof(T), navigationPropertyInfo);
        return ConvertPropertyInfoToExpression<T>(foreignKeyPropertyInfo);
    }

    /// <summary>
    /// Gets the foreign key property info from a collection navigation property.
    /// </summary>
    private static PropertyInfo? GetForeignKeyPropertyInfoFromCollection(Type parentType, Type childType, PropertyInfo collectionPropertyInfo)
    {
        // Case 1: Check InverseProperty attribute on collection property
        var inversePropertyAttr = collectionPropertyInfo.GetCustomAttribute<InversePropertyAttribute>();
        if (inversePropertyAttr != null)
        {
            // Find the inverse navigation property in child type
            var inverseNavigation = childType.GetProperty(inversePropertyAttr.Property);
            if (inverseNavigation != null)
            {
                // Get foreign key from the inverse navigation property
                return GetForeignKeyPropertyInfo(childType, inverseNavigation);
            }
        }

        // Case 2: Look for navigation property in child type that points back to parent
        var navigationToParent = childType.GetProperties()
            .FirstOrDefault(p => p.PropertyType == parentType);
        if (navigationToParent != null)
        {
            return GetForeignKeyPropertyInfo(childType, navigationToParent);
        }

        // Case 3: Try convention-based naming
        var conventionNames = new[]
        {
            $"{parentType.Name}Id",
            $"{collectionPropertyInfo.Name.TrimEnd('s')}Id" // Remove plural 's'
        };

        foreach (var name in conventionNames)
        {
            var property = childType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the foreign key expression from a collection navigation property.
    /// </summary>
    /// <typeparam name="T">The type containing the navigation property</typeparam>
    /// <typeparam name="TNavigation">The type of items in the collection</typeparam>
    /// <param name="navigationProperty">Expression representing the collection navigation property</param>
    /// <returns>Expression representing the foreign key property in the child entity</returns>
    public static Expression<Func<TNavigation, object>>? GetForeignKeyExpression<T, TNavigation>(Expression<Func<T, ICollection<TNavigation>>> navigationProperty)
    {
        var memberExpression = navigationProperty.GetMemberExpression();
        var collectionPropertyInfo = typeof(T).GetProperty(memberExpression.Member.Name);
        
        if (collectionPropertyInfo == null)
        {
            return null;
        }

        var foreignKeyPropertyInfo = GetForeignKeyPropertyInfoFromCollection(typeof(T), typeof(TNavigation), collectionPropertyInfo);
        return ConvertPropertyInfoToExpression<TNavigation>(foreignKeyPropertyInfo);
    }
}