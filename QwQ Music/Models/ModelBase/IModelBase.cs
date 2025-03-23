using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace QwQ_Music.Models.ModelBase;

public interface IModelBase<out TConfig>
    where TConfig : IModelBase<TConfig>
{
    bool IsInitialized { get; }
    bool IsError { get; }
    abstract static TConfig Parse(in SqliteDataReader config);
    Dictionary<string, string> Dump();
}
