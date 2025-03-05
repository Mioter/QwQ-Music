using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace QwQ.Generators;

[Generator]
public class JsonSerializerContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 添加生成器初始化标记 <button class="citation-flag" data-index="5">
        context.RegisterPostInitializationOutput(ctx => 
            ctx.AddSource("GeneratorDebugInfo.g.cs", "// Generator Initialized"));

        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static syntax => syntax != null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) =>
        {
            try
            {
                var (compilation, classes) = source;
                var jsonOptionsAttributeSymbol = compilation.GetTypeByMetadataName(
                    "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute");
                var jsonSerializableAttributeSymbol = compilation.GetTypeByMetadataName(
                    "System.Text.Json.Serialization.JsonSerializableAttribute");

                // 验证依赖和版本 <button class="citation-flag" data-index="1"><button class="citation-flag" data-index="8">
                var jsonAssembly = compilation.ReferencedAssemblyNames
                    .FirstOrDefault(a => a.Name == "System.Text.Json");
                if (jsonOptionsAttributeSymbol == null || jsonSerializableAttributeSymbol == null || 
                    jsonAssembly?.Version < new Version(6, 0, 0))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("JSCG001", "Missing Dependency",
                            "System.Text.Json >= 6.0 is required", "Generator",
                            DiagnosticSeverity.Error, true),
                        Location.None));
                    return;
                }

                var typesToGenerate = new List<(INamedTypeSymbol Type, string OptionsCode)>();

                foreach (var classDecl in classes)
                {
                    if (classDecl is null) continue;

                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol) continue;
                    
                    var generateAttribute = typeSymbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.Name == "GenerateJsonSerializerContextAttribute");

                    if (generateAttribute is null) continue;

                    string optionsCode = "WriteIndented = true";
                    // 处理构造函数参数 <button class="citation-flag" data-index="4">
                    foreach (var ctorArg in generateAttribute.ConstructorArguments)
                    {
                        if (ctorArg.Type?.Equals(jsonOptionsAttributeSymbol, SymbolEqualityComparer.Default) == true &&
                            ctorArg.Value is AttributeData ctorOptions)
                        {
                            optionsCode = ParseOptions(ctorOptions);
                        }
                    }

                    // 处理命名参数 <button class="citation-flag" data-index="3">
                    foreach (var arg in generateAttribute.NamedArguments)
                    {
                        if (arg is { Key: "Options", Value.Value: AttributeData namedOptions })
                        {
                            optionsCode = ParseOptions(namedOptions);
                        }
                    }

                    typesToGenerate.Add((typeSymbol, optionsCode));
                }

                if (!typesToGenerate.Any()) return;

                // 生成最终代码 <button class="citation-flag" data-index="7">
                string generatedCode = GenerateContextClasses(typesToGenerate);
                spc.AddSource("GeneratedJsonSerializerContexts.g.cs", SourceText.From(generatedCode, Encoding.UTF8));

                foreach (var type in typesToGenerate)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("JSCG002", "Code Generated",
                            $"Generated JsonSerializerContext for {type.Type.Name}", "Generator",
                            DiagnosticSeverity.Info, true),
                        Location.None));
                }
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("JSCG003", "Generator Error",
                        $"Error generating code: {ex.Message}", "Generator",
                        DiagnosticSeverity.Error, true),
                    Location.None));
            }
        });
    }

    private static string ParseOptions(AttributeData optionsAttr)
    {
        var sb = new StringBuilder();
        foreach (var arg in optionsAttr.NamedArguments)
        {
            string? value = arg.Value.Value switch
            {
                bool b => b.ToString().ToLower(),
                string s => $"\"{s}\"",
                Enum e => $"JsonSourceGenerationOptionsAttribute.{e.ToString()}",
                _ => arg.Value.Value?.ToString(),
            };
            sb.Append($"{arg.Key} = {value}, ");
        }
        return sb.ToString().TrimEnd(' ', ',');
    }

    private static string GenerateContextClasses(List<(INamedTypeSymbol Type, string OptionsCode)> types)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json.Serialization;");

        foreach ((var type, string options) in types)
        {
            // 添加分部类声明 <button class="citation-flag" data-index="4">
            sb.AppendLine($$"""

                            [JsonSourceGenerationOptions({{options}})]
                            [JsonSerializable(typeof({{type.ToDisplayString()}}))]
                            internal partial class {{type.Name}}_JsonSerializerContext : JsonSerializerContext
                            {
                                // 无需手动实现，由System.Text.Json源生成器填充
                            }
                            """);
        }

        return sb.ToString();
    }
}