using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class PageCodeGenerator : CSharpCodeGenerator
    {
        // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
        private const string Sha1AlgorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";
        private const int DisableAsyncWarning = 1998;

        private HashSet<string> _pageUsings;

        public PageCodeGenerator(CodeGeneratorContext context) 
            : base(context)
        {
        }

        protected ChunkTree Tree => Context.ChunkTreeBuilder.Root;

        protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpCodeVisitor = base.CreateCSharpCodeVisitor(writer, context);

            var attributeRenderer = new MvcTagHelperAttributeValueCodeRenderer(new GeneratedTagHelperAttributeContext()
            {
                CreateModelExpressionMethodName = "CreateModelExpression",
                ModelExpressionProviderPropertyName = "ModelExpressionProvider",
                ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression",
                ViewDataPropertyName = "ViewData",
            });
            csharpCodeVisitor.TagHelperRenderer.AttributeValueCodeRenderer = attributeRenderer;

            return csharpCodeVisitor;
        }

        public override CodeGeneratorResult Generate()
        {
            var writer = CreateCodeWriter();

            if (!Host.DesignTimeMode && !string.IsNullOrEmpty(Context.Checksum))
            {
                writer.Write("#pragma checksum \"")
                      .Write(Context.SourceFile)
                      .Write("\" \"")
                      .Write(Sha1AlgorithmId)
                      .Write("\" \"")
                      .Write(Context.Checksum)
                      .WriteLine("\"");
            }

            using (writer.BuildNamespace(Context.RootNamespace))
            {
                // Write out using directives
                AddImports(Tree, writer, Host.NamespaceImports);
                // Separate the usings and the class
                writer.WriteLine();

                using (BuildClassDeclaration(writer))
                {
                    if (Host.DesignTimeMode)
                    {
                        writer.WriteLine("private static object @__o;");
                    }

                    var csharpCodeVisitor = CreateCSharpCodeVisitor(writer, Context);

                    new CSharpTypeMemberVisitor(csharpCodeVisitor, writer, Context).Accept(Tree.Children);
                    CreateCSharpDesignTimeCodeVisitor(csharpCodeVisitor, writer, Context)
                        .AcceptTree(Tree);
                    new CSharpTagHelperFieldDeclarationVisitor(writer, Context).Accept(Tree.Children);

                    BuildConstructor(writer);

                    // Add space in-between constructor and method body
                    writer.WriteLine();

                    using (writer.BuildDisableWarningScope(DisableAsyncWarning))
                    {
                        using (writer.BuildMethodDeclaration("public override async", "Task", Host.GeneratedClassContext.ExecuteMethodName))
                        {
                            new CSharpTagHelperPropertyInitializationVisitor(writer, Context).Accept(Tree.Children);
                            csharpCodeVisitor.Accept(Tree.Children);
                        }
                    }
                }
            }

            return new CodeGeneratorResult(writer.GenerateCode(), writer.LineMappingManager.Mappings);
        }

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            return base.BuildClassDeclaration(writer);
        }

        protected override void BuildConstructor(CSharpCodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            // Check if there's a base type specified and if so generate a matching constructor
            var baseTypeVisitor = new CSharpBaseTypeVisitor(writer, Context);
            baseTypeVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            if (baseTypeVisitor.CurrentBaseType != null)
            {
                var baseTypeName = baseTypeVisitor.CurrentBaseType;
                var baseType = Type.GetType($"{baseTypeName}, {Assembly.GetEntryAssembly().FullName}");
                if (baseType == null)
                {
                    // HACK: Just loop through all the page's usings and try to load the base type each time.
                    //       In real life we'd need to support finding it in other assemblies too.
                    var assemblyName = Assembly.GetEntryAssembly().FullName;
                    foreach (var ns in _pageUsings)
                    {
                        baseType = Type.GetType($"{ns}.{baseTypeName}, {assemblyName}");
                        if (baseType != null)
                        {
                            break;
                        }
                    }

                    if (baseType == null)
                    {
                        throw new InvalidOperationException($"Specified base type '{baseTypeName}' was not found.");
                    }
                }
                var pageCtors = baseType.GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (pageCtors.Length != 1)
                {
                    throw new InvalidOperationException("Page base type requires a single constructor");
                }
                var ctorParams = pageCtors[0].GetParameters().ToDictionary(p => p.ParameterType.Name, p => p.Name);

                if (ctorParams.Count > 0)
                {
                    // public ClassName(Type name) : base(Type name)
                    // {
                    // }
                    writer.WriteLineHiddenDirective();
                    writer.Write($"public {Context.ClassName}(");
                    writer.Write(string.Join(", ", ctorParams.Select(p => $"{p.Key} {p.Value}")));
                    writer.Write(") : base(");
                    writer.Write(string.Join(", ", ctorParams.Select(p => $"{p.Value}")));
                    writer.Write(")");
                    writer.WriteLine();
                    writer.WriteLine("{");
                    writer.WriteLine("}");
                    writer.WriteLine();
                }
            }

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, "Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute");
            injectVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            var modelVisitor = new ModelChunkVisitor(writer, Context);
            modelVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);
            if (modelVisitor.ModelType != null)
            {
                writer.WriteLine();

                // public ModelType Model => ViewData?.Model ?? default(ModelType);
                writer.Write("public ").Write(modelVisitor.ModelType).Write(" Model => ViewData?.Model ?? default(").Write(modelVisitor.ModelType).Write(");");
            }

            writer.WriteLine();
            var modelType = modelVisitor.ModelType ?? Context.ClassName;

            // public new ViewDataDictionary<Model> ViewData => (ViewDataDictionary<Model>)base.ViewData;
            var viewDataType = $"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<{modelType}>";
            writer.Write("public new ").Write(viewDataType).Write($" ViewData => ({viewDataType})base.ViewData;");

            // [RazorInject]
            // public IHtmlHelper<Model> Html { get; private set; }
            writer.WriteLine();
            writer.Write("[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]").WriteLine();
            writer.Write($"public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<{modelType}> Html {{ get; private set; }}").WriteLine();

            // [RazorInject]
            // public ILogger<PageClass> Logger { get; private set; }
            writer.WriteLine();
            writer.Write("[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]").WriteLine();
            writer.Write($"public global::Microsoft.Extensions.Logging.ILogger<{Context.ClassName}> Logger {{ get; private set; }}").WriteLine();

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }

        private void AddImports(ChunkTree chunkTree, CSharpCodeWriter writer, IEnumerable<string> defaultImports)
        {
            // Write out using directives
            var usingVisitor = new CSharpUsingVisitor(writer, Context);
            foreach (var chunk in Tree.Children)
            {
                usingVisitor.Accept(chunk);
            }

            _pageUsings = new HashSet<string>(usingVisitor.ImportedUsings);
            foreach (var ns in defaultImports)
            {
                _pageUsings.Add(ns);
            }

            defaultImports = defaultImports.Except(usingVisitor.ImportedUsings);

            foreach (string import in defaultImports)
            {
                writer.WriteUsing(import);
            }

            var taskNamespace = typeof(Task).Namespace;

            // We need to add the task namespace but ONLY if it hasn't been added by the default imports or using imports yet.
            if (!defaultImports.Contains(taskNamespace) && !usingVisitor.ImportedUsings.Contains(taskNamespace))
            {
                writer.WriteUsing(taskNamespace);
            }
        }

        private class ModelChunkVisitor : CodeVisitor<CSharpCodeWriter>
        {
            public ModelChunkVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
                : base(writer, context)
            {
            }

            public string ModelType { get; set; }
            
            public override void Accept(Chunk chunk)
            {
                if (chunk is ModelChunk)
                {
                    Visit((ModelChunk)chunk);
                }
                else
                {
                    base.Accept(chunk);
                }
            }

            private void Visit(ModelChunk chunk)
            {
                ModelType = chunk.ModelType;
            }
        }
    }
}
