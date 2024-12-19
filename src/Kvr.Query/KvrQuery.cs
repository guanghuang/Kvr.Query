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

using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Kvr.Dapper;

namespace Kvr.Query
{
    /// <summary>
    /// A fluent query builder for entity relationships using Dapper.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent entity.</typeparam>
    public class KvrQuery<TParent>
    {
        private readonly SqlBuilder.SqlBuilder sqlBuilder;
        private IDbConnection dbConnection;
        private readonly Expression<Func<TParent, object>> primaryKeyProperty;
        private readonly SplitOnBuilder splitOnBuilder;
        private readonly List<LambdaExpression> navigationProperties = new();
        private readonly List<Expression<Func<TParent, object>>> navigationKeyProperties = new();
        private readonly List<Action<TParent>> distinctCallbacks = new();
        private readonly List<Action<TParent>> foreignKeyUpdateCallbacks = new();
        private LambdaExpression previousChildExpression;
        
        /// <summary>
        /// Initializes a new instance of the KvrQuery class.
        /// </summary>
        /// <param name="dbConnection">The database connection to use for queries.</param>
        /// <param name="primaryKeyProperty">Expression to specify the primary key property of the parent entity. If null, attempts to find it automatically.</param>
        /// <exception cref="KeyNotFoundException">Thrown when primary key cannot be found for the parent entity.</exception>
        private KvrQuery(IDbConnection? dbConnection, Expression<Func<TParent, object>>? primaryKeyProperty)
        {
            this.dbConnection = dbConnection;
            this.primaryKeyProperty = primaryKeyProperty ?? Utils.GetPrimaryKeyExpression<TParent>()!;

            if (this.primaryKeyProperty == null)
            {
                throw new KeyNotFoundException($"Primary key not found for the parent entity {typeof(TParent).Name}.");
            }
            sqlBuilder = SqlBuilder.SqlBuilder.Create();
            sqlBuilder.From<TParent>();
            splitOnBuilder = SplitOnBuilder.Create();
        }

        /// <summary>
        /// Sets the database connection for the query.
        /// </summary>
        /// <param name="dbConnection">The database connection</param>
        /// <returns>The current KvrQuery instance</returns>
        public KvrQuery<TParent> SetDbConnection(IDbConnection dbConnection) {
            this.dbConnection = dbConnection;
            return this;
        }
        
        /// <summary>
        /// Allows you to modify the internal SqlBuilder instance.
        /// </summary>
        /// <param name="action">The action to perform on the SqlBuilder instance for custom sql.</param>
        /// <returns>The current KvrQuery instance</returns>
        public KvrQuery<TParent> UseInternalSqlBuilder(Action<SqlBuilder.SqlBuilder> action)
        {
            action(sqlBuilder);
            return this;
        }
        
        /// <summary>
        /// Creates a new instance of the KvrQuery class.
        /// </summary>
        /// <param name="dbConnection">The database connection to use for queries.</param>
        /// <param name="primaryKeyProperty">Expression to specify the primary key property of the parent entity. If null, attempts to find it automatically.</param>
        /// <returns>A new instance of the KvrQuery class.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when primary key cannot be found for the parent entity.</exception>
        public static KvrQuery<TParent> Create(IDbConnection dbConnection, Expression<Func<TParent, object>>? primaryKeyProperty = null)
        {
            return new KvrQuery<TParent>(dbConnection, primaryKeyProperty);
        }

        /// <summary>
        /// Includes a one-to-many relationship in the query.
        /// </summary>
        /// <typeparam name="TChild">The type of the child entity.</typeparam>
        /// <param name="navigationProperty">Expression to specify the navigation property collection in the parent entity.</param>
        /// <param name="foreignKeyProperty">Expression to specify the foreign key property in the child entity.</param>
        /// <param name="childPrimaryKeyProperty">Expression to specify the primary key property of the child entity. If null, attempts to find it automatically.</param>
        /// <param name="excludeColumns">Columns to exclude from the SQL query. For then include grade child.</param>
        /// <returns>The current KvrQuery instance for method chaining.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when primary key cannot be found for the child entity.</exception>
        public KvrIncludeQuery<TParent, TChild> IncludeMany<TChild>(Expression<Func<TParent, ICollection<TChild>>> navigationProperty, Expression<Func<TChild, object>>? foreignKeyProperty = null, 
            Expression<Func<TChild, object>>? childPrimaryKeyProperty = null, Expression<Func<TChild, object>>[]? excludeColumns = null)
        {
            childPrimaryKeyProperty ??= Utils.GetPrimaryKeyExpression<TChild>()!;
            if (childPrimaryKeyProperty == null)
            {
                throw new KeyNotFoundException($"Primary key not found for the child entity {typeof(TChild).Name}.");
            }
            foreignKeyProperty ??= Utils.GetForeignKeyExpression(navigationProperty)!;
            
            sqlBuilder.SelectAll(excludeColumns, childPrimaryKeyProperty, out var prefix);
            sqlBuilder.LeftJoin(primaryKeyProperty, foreignKeyProperty, rightPrefix: prefix);
            splitOnBuilder.SplitOn(childPrimaryKeyProperty);
            navigationProperties.Add(navigationProperty);
            distinctCallbacks.Add(parent => parent.DistinctChildren(navigationProperty, childPrimaryKeyProperty.Compile()));
            previousChildExpression = navigationProperty;
            return new KvrIncludeQuery<TParent, TChild>(this);
        }

        /// <summary>
        /// Includes a one-to-one/many-to-one relationship in the query.
        /// </summary>
        /// <typeparam name="TChild">The type of the child entity.</typeparam>
        /// <param name="navigationProperty">Expression to specify the navigation property in the parent entity.</param>
        /// <param name="navigationForeignKeyProperty">Expression to specify the foreign key property in the parent entity.</param>
        /// <param name="childPrimaryKeyProperty">Expression to specify the primary key property of the child entity. If null, attempts to find it automatically.</param>
        /// <param name="includeNavigationKeyInSql">Whether to include the navigation key property from the SQL query.</param>
        /// <param name="excludeColumns">Columns to exclude from the SQL query. For then include grade child.</param>
        /// <returns>The current KvrQuery instance for method chaining.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when primary key cannot be found for the child entity.</exception>
        public KvrIncludeQuery<TParent, TChild> IncludeOne<TChild>(Expression<Func<TParent, TChild>> navigationProperty, Expression<Func<TParent, object>>? navigationForeignKeyProperty = null,
            Expression<Func<TChild, object>>? childPrimaryKeyProperty = null, bool includeNavigationKeyInSql = false, Expression<Func<TChild, object>>[]? excludeColumns = null)
        {
            childPrimaryKeyProperty ??= Utils.GetPrimaryKeyExpression<TChild>()!;
            if (childPrimaryKeyProperty == null)
            {
                throw new KeyNotFoundException($"Primary key not found for the child entity {typeof(TChild).Name}.");
            }
            navigationForeignKeyProperty ??= Utils.GetForeignKeyExpression(navigationProperty)!;

            sqlBuilder.SelectAll(excludeColumns, childPrimaryKeyProperty, out var prefix);
            sqlBuilder.LeftJoin(navigationForeignKeyProperty, childPrimaryKeyProperty, rightPrefix: prefix);
            splitOnBuilder.SplitOn(childPrimaryKeyProperty);
            navigationProperties.Add(navigationProperty);
            previousChildExpression = navigationProperty;
            if (!includeNavigationKeyInSql)
            {
                foreignKeyUpdateCallbacks.Add(p =>
                {
                    SetForeignKey(p, navigationForeignKeyProperty, navigationProperty, childPrimaryKeyProperty);
                });
                navigationKeyProperties.Add(navigationForeignKeyProperty);
            }
            return new KvrIncludeQuery<TParent, TChild>(this);
        }
        
        /// <summary>
        /// Sets the foreign key property for the parent entity.
        /// </summary>
        /// <typeparam name="TP">The type of the parent entity.</typeparam>
        /// <typeparam name="TChild">The type of the child entity.</typeparam>
        /// <param name="p">The parent entity.</param>
        /// <param name="navigationKeyProperty">The navigation key property.</param>
        /// <param name="navigationProperty">The navigation property.</param>
        /// <param name="childPrimaryKeyProperty">The primary key property of the child entity.</param>
        private static void SetForeignKey<TP, TChild>(TP p, Expression<Func<TP, object>> navigationKeyProperty, Expression<Func<TP, TChild>> navigationProperty, Expression<Func<TChild, object>> childPrimaryKeyProperty)
        {
            var v = Dapper.Utils.GetPropertyValue(p, navigationProperty);
            if (v != null)
            {
                Dapper.Utils.SetPropertyValue(p, navigationKeyProperty.GetMemberExpression(), Dapper.Utils.GetPropertyValue<TChild, object>(v, childPrimaryKeyProperty.GetMemberExpression()));
            }
        }

        /// <summary>
        /// Includes a one-to-one/many-to-one relationship in the query.
        /// </summary>
        /// <typeparam name="TChild">The type of the child entity.</typeparam>
        /// <typeparam name="TGrandChild">The type of the grandchild entity.</typeparam>
        /// <param name="childNavigationProperty">Expression to specify the navigation property in the child entity.</param>
        /// <param name="childNavigationKeyProperty">Expression to specify the foreign key property in the child entity.</param>
        /// <param name="grandChildPrimaryKeyProperty">Expression to specify the primary key property of the grandchild entity. If null, attempts to find it automatically.</param>
        /// <returns>The current KvrQuery instance for method chaining.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when primary key cannot be found for the grandchild entity.</exception>
        internal KvrQuery<TParent> ThenIncludeOne<TChild, TGrandChild>(Expression<Func<TChild, TGrandChild>> childNavigationProperty, Expression<Func<TChild, object>>? childNavigationKeyProperty = null, 
            Expression<Func<TGrandChild, object>>? grandChildPrimaryKeyProperty = null)
        {
            if (previousChildExpression == null)
            {
                throw new InvalidOperationException("Previous child expression is null. You must call IncludeOne before ThenIncludeOne.");
            }
            grandChildPrimaryKeyProperty ??= Utils.GetPrimaryKeyExpression<TGrandChild>()!;
            if (grandChildPrimaryKeyProperty == null)
            {
                throw new KeyNotFoundException($"Primary key not found for the grandchild entity {typeof(TGrandChild).Name}.");
            }
            childNavigationKeyProperty ??= Utils.GetForeignKeyExpression(childNavigationProperty)!;
            
            sqlBuilder.SelectAll(grandChildPrimaryKeyProperty, out var prefix);
            sqlBuilder.LeftJoin(childNavigationKeyProperty, grandChildPrimaryKeyProperty, rightPrefix: prefix);
            splitOnBuilder.SplitOn(grandChildPrimaryKeyProperty);
            navigationProperties.Add(childNavigationProperty);
            var tmpPreviousChildExpression = previousChildExpression;
            foreignKeyUpdateCallbacks.Add(p =>
            {
                var child = Dapper.Utils.GetPropertyValue<TParent, object>(p,
                    tmpPreviousChildExpression.GetMemberExpression());
                if (child != null)
                {
                    if (Dapper.Utils.IsCollectionType(tmpPreviousChildExpression.GetMemberExpression().Type))
                    {
                        // Handle collection type
                        if (child is IEnumerable collection)
                        {
                            foreach (TChild item in collection)
                            {
                                SetForeignKey(item, childNavigationKeyProperty, childNavigationProperty,
                                    grandChildPrimaryKeyProperty);
                            }
                        }
                    }
                    else
                    {
                        // Handle single object
                        SetForeignKey((TChild)child, childNavigationKeyProperty, childNavigationProperty,
                            grandChildPrimaryKeyProperty);
                    }
                }
            });
            return this;
        }
        
        /// <summary>
        /// Executes the query and returns the results with all included relationships.
        /// </summary>
        /// <param name="param">The parameters to pass to the query.</param>
        /// <param name="transaction">The transaction to use for the query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="commandTimeout">The command timeout in seconds.</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="callbackAfterMapRow">Optional callback to execute after each row is mapped.</param>
        /// <returns>An enumerable collection of parent entities with their related entities included.</returns>
        public async Task<IEnumerable<TParent>> QueryAsync(object? param = null,
            IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null,
            Action<object[]>? callbackAfterMapRow = null)
        {
            sqlBuilder.SelectAll(navigationKeyProperties.ToArray(), primaryKeyProperty, fromBegin:true);
            var result = (await dbConnection.ConfigMapper(primaryKeyProperty, navigationProperties.ToArray())
                .SplitOn(splitOnBuilder.Build()).QueryAsync(sqlBuilder.Build(), param, transaction, buffered, null!, commandTimeout, commandType, callbackAfterMapRow)).ToList();
            foreach (var p in result)
            {
                foreach (var callback in foreignKeyUpdateCallbacks)
                {
                    callback(p);
                }

                if (distinctCallbacks.Count > 1)
                {
                    foreach (var callback in distinctCallbacks)
                    {
                        callback(p);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Adds a WHERE clause to the query.   
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TResult">The column type</typeparam>
        /// <param name="propertyExpression">The column expression</param>
        /// <param name="value">The value to compare against</param>
        /// <param name="op">The comparison operator (defaults to "=")</param>
        /// <param name="prefix">Optional table prefix/alias</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Where<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            sqlBuilder.Where(propertyExpression, value, op, prefix);
            return this;
        }

        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL string</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Where(string rawSql)
        {
            sqlBuilder.Where(rawSql);
            return this;
        }

        /// <summary>
        /// Adds an AND condition to the WHERE clause.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TResult">The column type</typeparam>
        /// <param name="propertyExpression">The column expression</param>
        /// <param name="value">The value to compare against</param>
        /// <param name="op">The comparison operator (defaults to "=")</param>
        /// <param name="prefix">Optional table prefix/alias</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> And<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            sqlBuilder.And(propertyExpression, value, op, prefix);
            return this;
        }

        /// <summary>
        /// Adds an AND condition to the WHERE clause.
        /// </summary>
        /// <param name="rawSql">The raw SQL string</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> And(string rawSql)
        {
            sqlBuilder.And(rawSql);
            return this;
        }

        /// <summary>
        /// Adds an OR condition to the WHERE clause.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult">The column type</typeparam>
        /// <param name="propertyExpression">The column expression</param>
        /// <param name="value">The value to compare against</param>
        /// <param name="op">The comparison operator (defaults to "=")</param>
        /// <param name="prefix">Optional table prefix/alias</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Or<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            sqlBuilder.Or(propertyExpression, value, op, prefix);
            return this;
        }

        /// <summary>
        /// Adds an OR condition to the WHERE clause.
        /// </summary>
        /// <param name="rawSql">The raw SQL string</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Or(string rawSql)
        {
            sqlBuilder.Or(rawSql);
            return this;
        }

        /// <summary>
        /// Adds an ORDER BY clause to the query.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TResult">The column type</typeparam>
        /// <param name="propertyExpression">The column expression</param>
        /// <param name="ascOrder">Whether to sort in ascending order (defaults to true)</param>
        /// <param name="prefix">Optional table prefix/alias</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> OrderBy<T, TResult>(Expression<Func<T, TResult>> propertyExpression, bool ascOrder = true, string? prefix = null)
        {
            sqlBuilder.OrderBy(propertyExpression, ascOrder, prefix);
            return this;
        }

        /// <summary>
        /// Adds an ORDER BY clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL string</param>
        /// <param name="ascOrder">Whether to sort in ascending order (defaults to true)</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> OrderBy(string rawSql, bool ascOrder = true)
        {
            sqlBuilder.OrderBy(rawSql, ascOrder);
            return this;
        }

        /// <summary>
        /// Appends raw SQL to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL string</param>
        /// <param name="afterFrom">Whether to append the raw SQL after the FROM clause (defaults to true)</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> RawSql(string rawSql, bool afterFrom = true)
        {
            sqlBuilder.RawSql(rawSql, afterFrom);
            return this;
        }
    }
}