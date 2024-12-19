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

using System.Data;
using System.Linq.Expressions;

namespace Kvr.Query
{
    /// <summary>
    /// A fluent query builder for entity relationships that tracks the child type for ThenInclude operations.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent entity</typeparam>
    /// <typeparam name="TChild">The type of the child entity from the previous Include</typeparam>
    public class KvrIncludeQuery<TParent, TChild>
    {
        /// <summary>
        /// The query builder for the parent entity.
        /// </summary>
        private readonly KvrQuery<TParent> query;

        /// <summary>
        /// Initializes a new instance of the KvrIncludeQuery class.
        /// </summary>
        internal KvrIncludeQuery(
            KvrQuery<TParent> query)
        {
            this.query = query;
        }

        /// <summary>
        /// Includes a one-to-one relationship for the previously included entity.
        /// </summary>
        /// <typeparam name="TGrandChild">The type of the grandchild entity</typeparam>
        /// <param name="navigationProperty">The navigation property to include</param>
        /// <param name="navigationKeyProperty">The navigation key property</param>
        /// <param name="grandChildPrimaryKeyProperty">The grandchild primary key property</param>
        /// <returns>The current KvrIncludeQuery instance for method chaining</returns>
        public KvrIncludeQuery<TParent, TChild> ThenIncludeOne<TGrandChild>(
            Expression<Func<TChild, TGrandChild>> navigationProperty,
            Expression<Func<TChild, object>>? navigationKeyProperty = null,
            Expression<Func<TGrandChild, object>>? grandChildPrimaryKeyProperty = null)
        {
            query.ThenIncludeOne(navigationProperty, navigationKeyProperty, grandChildPrimaryKeyProperty);
            return this;
        }
        
        /// <summary>
        /// Sets the database connection for the query.
        /// </summary>
        /// <param name="dbConnection">The database connection</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> SetDbConnection(IDbConnection dbConnection)
        {
            return query.SetDbConnection(dbConnection);
        }

        /// <summary>
        /// Uses the internal SQL builder for the query.
        /// </summary>
        /// <param name="action">The action to perform on the SQL builder</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> UseInternalSqlBuilder(Action<SqlBuilder.SqlBuilder> action)
        {
            return query.UseInternalSqlBuilder(action);
        }

        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="propertyExpression">The property expression</param>
        /// <param name="value">The value to compare</param>
        /// <param name="op">The operator</param>
        /// <param name="prefix">The prefix</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Where<T, TResult>(
            Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            return query.Where(propertyExpression, value, op, prefix);
        }

        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL to add</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Where(string rawSql)
        {
            return query.Where(rawSql);
        }

        /// <summary>
        /// Adds an AND clause to the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="propertyExpression">The property expression</param>
        /// <param name="value">The value to compare</param>
        /// <param name="op">The operator</param>
        /// <param name="prefix">The prefix</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> And<T, TResult>(
            Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            return query.And(propertyExpression, value, op, prefix);
        }

        /// <summary>
        /// Adds an AND clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL to add</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> And(string rawSql)
        {
            return query.And(rawSql);
        }

        /// <summary>
        /// Adds an OR clause to the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="propertyExpression">The property expression</param>
        /// <param name="value">The value to compare</param>
        /// <param name="op">The operator</param>
        /// <param name="prefix">The prefix</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Or<T, TResult>(
            Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
        {
            return query.Or(propertyExpression, value, op, prefix);
        }

        /// <summary>
        /// Adds an OR clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL to add</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> Or(string rawSql)
        {
            return query.Or(rawSql);
        }

        /// <summary>
        /// Adds an ORDER BY clause to the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="propertyExpression">The property expression</param>
        /// <param name="ascOrder">Whether to order in ascending order</param>
        /// <param name="prefix">The prefix</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> OrderBy<T, TResult>(
            Expression<Func<T, TResult>> propertyExpression, bool ascOrder = true, string? prefix = null)
        {
            return query.OrderBy(propertyExpression, ascOrder, prefix);
        }

        /// <summary>
        /// Adds an ORDER BY clause to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL to add</param>
        /// <param name="ascOrder">Whether to order in ascending order</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> OrderBy(string rawSql, bool ascOrder = true)
        {
            return query.OrderBy(rawSql, ascOrder);
        }

        /// <summary>
        /// Adds raw SQL to the query.
        /// </summary>
        /// <param name="rawSql">The raw SQL to add</param>
        /// <param name="afterFrom">Whether to add the SQL after the FROM clause</param>
        /// <returns>The current KvrQuery instance for method chaining</returns>
        public KvrQuery<TParent> RawSql(string rawSql, bool afterFrom = true)
        {
            return query.RawSql(rawSql, afterFrom);
        }

        /// <summary>
        /// Executes the query asynchronously and returns the results.
        /// </summary>
        /// <param name="param">The parameters for the query</param>
        /// <param name="transaction">The database transaction</param>
        /// <param name="buffered">Whether to buffer the results</param>
        /// <param name="commandTimeout">The command timeout</param>
        /// <param name="commandType">The command type</param>
        /// <param name="callbackAfterMapRow">The callback to execute after mapping each row</param>
        /// <returns>The results of the query</returns>
        public Task<IEnumerable<TParent>> QueryAsync(
            object? param = null,
            IDbTransaction? transaction = null,
            bool buffered = true,
            int? commandTimeout = null,
            CommandType? commandType = null,
            Action<object[]>? callbackAfterMapRow = null)
        {
            return query.QueryAsync(param, transaction, buffered, commandTimeout, commandType, callbackAfterMapRow);
        }

        /// <summary>
        /// Includes a one-to-many relationship in the query.
        /// </summary>
        public KvrIncludeQuery<TParent, TOther> IncludeMany<TOther>(
            Expression<Func<TParent, ICollection<TOther>>> navigationProperty,
            Expression<Func<TOther, object>>? foreignKeyProperty = null,
            Expression<Func<TOther, object>>? childPrimaryKeyProperty = null,
            Expression<Func<TOther, object>>[]? excludeColumns = null)
        {
            return query.IncludeMany(navigationProperty, foreignKeyProperty, childPrimaryKeyProperty, excludeColumns);
        }

        /// <summary>
        /// Includes a one-to-one/many-to-one relationship in the query.
        /// </summary>
        /// <typeparam name="TOther">The type of the other entity</typeparam>
        /// <param name="navigationProperty">The navigation property to include</param>
        /// <param name="navigationForeignKeyProperty">The navigation foreign key property</param>
        /// <param name="childPrimaryKeyProperty">The child primary key property</param>
        /// <param name="includeNavigationKeyInSql">Whether to include the navigation key in the SQL</param>
        /// <param name="excludeColumns">The columns to exclude</param>
        /// <returns>The current KvrIncludeQuery instance for method chaining</returns>
        public KvrIncludeQuery<TParent, TOther> IncludeOne<TOther>(
            Expression<Func<TParent, TOther>> navigationProperty,
            Expression<Func<TParent, object>>? navigationForeignKeyProperty = null,
            Expression<Func<TOther, object>>? childPrimaryKeyProperty = null,
            bool includeNavigationKeyInSql = false,
            Expression<Func<TOther, object>>[]? excludeColumns = null)
        {
            return query.IncludeOne(navigationProperty, navigationForeignKeyProperty, childPrimaryKeyProperty, includeNavigationKeyInSql, excludeColumns);
        }
    }
} 