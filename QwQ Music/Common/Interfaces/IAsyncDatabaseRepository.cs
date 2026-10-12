using System.Diagnostics.Contracts;
using QwQ_Music.Common.Services.Databases;

namespace QwQ_Music.Common.Interfaces;

public interface IAsyncReadonlyDatabaseRepository<in TPrimaryKey, TOut> : IAsyncDisposable where TPrimaryKey : notnull {
    /// <summary>
    ///     根据主键值获取单个实体对象
    /// </summary>
    /// <param name="key">实体的主键值</param>
    /// <returns>返回指定 ID的实体对象，如果不存在则返回null</returns>
    /// <remarks>该方法执行精确匹配查询，返回单个实体或null</remarks>
    [Pure]
    Task<TOut?> SingleAsync(TPrimaryKey key);

    /// <summary>
    ///     获取所有实体对象的集合
    /// </summary>
    /// <returns>包含所有目标实体的集合</returns>
    /// <remarks>请注意数据量较大的情况</remarks>
    [Pure]
    Task<IEnumerable<TOut>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1);

    /// <summary>
    ///     获取实体表中的记录总数
    /// </summary>
    /// <returns>返回实体表中的记录数量</returns>
    [Pure]
    Task<int> CountAsync();

    /// <summary>
    ///     检查指定主键值的实体是否存在
    /// </summary>
    /// <param name="key">要检查的实体主键值</param>
    /// <returns>如果实体存在返回true，否则返回false</returns>
    /// <remarks>该方法比先Get再判断null更高效</remarks>
    [Pure]
    Task<bool> ExistsAsync(TPrimaryKey key);
}

public interface IAsyncDatabaseRepository<in TPrimaryKey, in TIn, TOut> : IAsyncReadonlyDatabaseRepository<TPrimaryKey, TOut>
    where TPrimaryKey : notnull
    where TIn : notnull {
    /// <summary>
    ///     插入新的实体数据到数据库
    /// </summary>
    /// <param name="item">要插入的实体对象</param>
    /// <param name="onInsertExist">实体冲突时的行为</param>
    /// <exception cref="ArgumentNullException">当<paramref name="item" />为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当实体已存在或违反约束时抛出</exception>
    Task InsertAsync(TIn item, InsertExist onInsertExist);


    /// <summary>
    ///     通过主键值更新整个 <see cref="TIn" /> 实体对象
    /// </summary>
    /// <param name="item"><see cref="TIn" />类型的实体实例</param>
    /// <exception cref="ArgumentNullException">当<paramref name="item" />为null时抛出</exception>
    /// <exception cref="ArgumentException">当实体不存在时抛出</exception>
    /// <remarks>该方法会完全替换原有记录，建议使用前先获取完整实体对象</remarks>
    Task UpdateAsync(TIn item);

    /// <summary>
    ///     通过主键值更新指定字段的值
    /// </summary>
    /// <param name="key">实体的主键值</param>
    /// <param name="fieldValues">要更新的字段名称和值的字典</param>
    Task UpdateAsync(TPrimaryKey key, Dictionary<string, object?> fieldValues);

    /// <summary>
    ///     根据主键值删除指定实体
    /// </summary>
    /// <param name="key">要删除的实体</param>
    /// <remarks>该操作不可逆，请谨慎使用</remarks>
    Task DeleteAsync(TPrimaryKey key);
}

/// <summary>
///     数据库仓储接口，提供对泛型类型 <typeparamref name="T" /> 的基本数据操作
/// </summary>
/// <typeparam name="TPrimaryKey">键类型</typeparam>
/// <typeparam name="T">实体类型</typeparam>
/// <remarks>
///     该接口定义了常见的CRUD操作，包括查询、插入、更新、删除等基本数据库操作
///     实现类需要负责管理数据库连接和事务处理
/// </remarks>
public interface IAsyncDatabaseRepository<in TPrimaryKey, T> : IAsyncDatabaseRepository<TPrimaryKey, T, T>
    where TPrimaryKey : notnull
    where T : notnull { }

public interface IAsyncDatabaseRepository<T> : IAsyncDatabaseRepository<string, T, T> where T : notnull;