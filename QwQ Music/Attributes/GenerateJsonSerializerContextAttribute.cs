    using System.Text.Json.Serialization;
    using System;
    
    namespace QwQ_Music.Attributes;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class GenerateJsonSerializerContextAttribute(JsonSourceGenerationOptionsAttribute options) : Attribute
    {
        public JsonSourceGenerationOptionsAttribute Options { get; } = options;

        public GenerateJsonSerializerContextAttribute() : this(new JsonSourceGenerationOptionsAttribute
        {
            WriteIndented = true, // 默认配置
        }) { }

    }