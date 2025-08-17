using System;
using System.Collections.Generic;

namespace QwQ_Music.Common.Interfaces;

/// <summary>
///     数据库仓储接口，提供对泛型类型 <typeparamref name="T" /> 的基本数据操作
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
/// <remarks>
///     该接口定义了常见的CRUD操作，包括查询、插入、更新、删除等基本数据库操作
///     实现类需要负责管理数据库连接和事务处理
/// </remarks>
public interface IDatabaseRepository<T> : IDisposable
{
    /// <summary>
    ///     根据主键值获取单个实体对象
    /// </summary>
    /// <param name="primaryKey">实体的主键值</param>
    /// <returns>返回指定ID的实体对象，如果不存在则返回null</returns>
    /// <remarks>该方法执行精确匹配查询，返回单个实体或null</remarks>
    public T? Get(string primaryKey);

    /// <summary>
    ///     获取所有实体对象的集合
    /// </summary>
    /// <returns>返回包含所有实体的可枚举集合</returns>
    /// <remarks>该方法会返回表中的所有记录，请注意数据量较大的情况</remarks>
    public IEnumerable<T> GetAll();

    /// <summary>
    ///     获取实体表中的记录总数
    /// </summary>
    /// <returns>返回实体表中的记录数量</returns>
    public int Count();

    /// <summary>
    ///     插入新的实体数据到数据库
    /// </summary>
    /// <param name="item">要插入的实体对象</param>
    /// <exception cref="ArgumentNullException">当<paramref name="item" />为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当实体已存在或违反约束时抛出</exception>
    public void Insert(T item);

    /// <summary>
    ///     通过主键值更新整个 <see cref="T" /> 实体对象
    /// </summary>
    /// <param name="item"><see cref="T" />类型的实体实例</param>
    /// <exception cref="ArgumentNullException">当<paramref name="item" />为null时抛出</exception>
    /// <exception cref="ArgumentException">当实体不存在时抛出</exception>
    /// <remarks>该方法会完全替换原有记录，建议使用前先获取完整实体对象</remarks>
    public void Update(T item);

    /// <summary>
    ///     通过主键值更新指定字段的值
    /// </summary>
    /// <param name="primaryKey">实体的主键值</param>
    /// <param name="fields">要更新的字段名称数组</param>
    /// <param name="values">对应字段的新值数组</param>
    /// <exception cref="ArgumentException">当<paramref name="fields" />和<paramref name="values" />长度不匹配时抛出</exception>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <remarks>
    ///     字段数组和值数组必须一一对应，该方法只更新指定的字段
    ///     适用于部分字段更新的场景，提高性能
    /// </remarks>
    public void Update(string primaryKey, string[] fields, string?[] values);

    /// <summary>
    ///     通过主键值更新指定字段的值
    /// </summary>
    /// <param name="primaryKey">实体的主键值</param>
    /// <param name="fieldValues">要更新的字段名称和值的字典</param>
    public void Update(string primaryKey, Dictionary<string, object?> fieldValues);

    /// <summary>
    ///     根据主键值删除指定实体
    /// </summary>
    /// <param name="primaryKey">要删除实体的主键值</param>
    /// <exception cref="ArgumentException">当<paramref name="primaryKey" />为空或null时抛出</exception>
    /// <remarks>该操作不可逆，请谨慎使用</remarks>
    public void Delete(string primaryKey);

    /// <summary>
    ///     检查指定主键值的实体是否存在
    /// </summary>
    /// <param name="primaryKey">要检查的实体主键值</param>
    /// <returns>如果实体存在返回true，否则返回false</returns>
    /// <remarks>该方法比先Get再判断null更高效</remarks>
    public bool Exists(string primaryKey);
}
