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

namespace Kvr.Query;

/// <summary>
/// Extension methods for KvrQuery
/// </summary>
public static class KvrQueryExtension
{
    /// <summary>
    /// Creates a new KvrQuery instance.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type</typeparam>
    /// <param name="dbConnection">The database connection</param>
    /// <param name="primaryKeyProperty">The primary key property</param>
    /// <returns>A new KvrQuery instance</returns>
    public static KvrQuery<TParent> Select<TParent>(this IDbConnection dbConnection, Expression<Func<TParent, object>>? primaryKeyProperty = null)
    {
        return KvrQuery<TParent>.Create(dbConnection, primaryKeyProperty);
    }
}
