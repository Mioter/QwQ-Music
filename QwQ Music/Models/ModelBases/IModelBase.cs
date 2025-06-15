using System.Collections.Generic;

namespace QwQ_Music.Models.ModelBases;

public interface IModelBase<out TConfig>
    where TConfig : IModelBase<TConfig>
{
    bool IsInitialized { get; }
    bool IsError { get; }
    static abstract TConfig FromDictionary(Dictionary<string, object> data);
    Dictionary<string, string?> Dump();
}
