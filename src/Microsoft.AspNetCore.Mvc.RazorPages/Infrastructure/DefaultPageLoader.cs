// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation.Rewriters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultPageLoader : IPageLoader
    {
        private readonly CSharpCompilationFactory _compilationFactory;
        private readonly RazorEngine _engine;
        private readonly PageRazorEngineHost _host;
        private readonly RazorProject _project;
        private readonly RazorPagesOptions _options;
        private readonly ITagHelperDescriptorResolver _tagHelperDescriptorResolver;

        public DefaultPageLoader(
            IOptions<RazorPagesOptions> options,
            RazorProject project,
            CSharpCompilationFactory compilationFactory,
            PageRazorEngineHost host,
            ITagHelperDescriptorResolver tagHelperDescriptorResolver)
        {
            _options = options.Value;
            _project = project;
            _compilationFactory = compilationFactory;
            _host = host;

            _tagHelperDescriptorResolver = tagHelperDescriptorResolver;

            _engine = RazorEngineBuilder.Build(builder =>
            {
                builder.Features.Add(new TagHelperFeature(_host.TagHelperDescriptorResolver));
                builder.Features.Add(new TagHelperBinderSyntaxTreePass());
                builder.Features.Add(new DefaultChunkTreeLoweringFeature(_host));

                builder.Phases.Add(new DefaultSyntaxTreePhase());
                builder.Phases.Add(new DefaultChunkTreePhase());
                builder.Phases.Add(new DefaultCSharpSourceLoweringPhase(_host));
            });
        }

        public Type Load(PageActionDescriptor actionDescriptor)
        {
            var source = CreateSourceDocument(actionDescriptor);
            return Load(source, actionDescriptor.RelativePath);
        }

        protected virtual RazorSourceDocument CreateSourceDocument(PageActionDescriptor actionDescriptor)
        {
            var item = _project.GetItem(actionDescriptor.RelativePath);
            if (item == null)
            {
                throw new InvalidOperationException($"file {actionDescriptor.RelativePath} was not found");
            }
            
            using (var stream = item.Read())
            {
                return RazorSourceDocument.ReadFrom(stream, item.PhysicalPath);
            }
        }

        protected virtual Type Load(RazorSourceDocument source, string relativePath)
        {
            var document = _engine.CreateCodeDocument(source);

            var parsed = RazorParser.Parse(source);
            document.SetSyntaxTree(RazorParser.Parse(source));
            foreach (var error in parsed.Diagnostics)
            {
                document.ErrorSink.OnError(error);
            }
            
            var @namespace = GetNamespace(relativePath);
            var @class = "Generated_" + Path.GetFileNameWithoutExtension(Path.GetFileName(relativePath));

            document.WithClassName(@namespace, @class);

            _engine.Process(document);
            if (document.ErrorSink.Errors.Any())
            {
                throw CreateException(document);
            }

            var compilation = CreateCompilation(@class, @namespace, relativePath, document.GetGeneratedCSharpDocument().GeneratedCode);
            var text = compilation.SyntaxTrees[0].ToString();

            using (var pe = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    var emitResult = compilation.Emit(
                        peStream: pe,
                        pdbStream: pdb,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                    if (!emitResult.Success)
                    {
                        throw CreateException(document, relativePath, text, compilation.AssemblyName, emitResult.Diagnostics);
                    }

                    pe.Seek(0, SeekOrigin.Begin);
                    pdb.Seek(0, SeekOrigin.Begin);

                    var assembly = LoadStream(pe, pdb);
                    var type = assembly.GetExportedTypes().FirstOrDefault(a => !a.IsNested);
                    return type;
                }
            }
        }

        protected virtual CSharpCompilation CreateCompilation(
            string @class,
            string @namespace,
            string relativePath,
            string text)
        {
            var classFullName = @namespace + "." + @class;

            var tree = CSharpSyntaxTree.ParseText(SourceText.From(text, Encoding.UTF8));

            var compilation = _compilationFactory.Create().AddSyntaxTrees(tree);

            var classSymbol = compilation.GetTypeByMetadataName(classFullName);

            HandlerMethod onGet = null;
            HandlerMethod onPost = null;

            foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.Name.StartsWith("OnGet", StringComparison.Ordinal))
                {
                    if (onGet != null)
                    {
                        throw new InvalidOperationException("You can't have more than one OnGet method");
                    }

                    onGet = HandlerMethod.FromSymbol(method, "GET");
                }
                else if (method.Name.StartsWith("OnPost", StringComparison.Ordinal))
                {
                    if (onPost != null)
                    {
                        throw new InvalidOperationException("You can't have more than one OnPost method");
                    }

                    onPost = HandlerMethod.FromSymbol(method, "POST");
                }
            }

            GenerateExecuteAsyncMethod(ref compilation, onGet, onPost);

            GenerateCallToBaseConstructor(ref compilation, compilation.GetTypeByMetadataName(classFullName));

            return compilation;
        }

        private void GenerateCallToBaseConstructor(ref CSharpCompilation compilation, INamedTypeSymbol classSymbol)
        {
            var rewriter = new CallBaseConstructorRewriter(classSymbol, classSymbol.BaseType);

            var original = compilation.SyntaxTrees[0];
            var rewritten = CSharpSyntaxTree.Create((CSharpSyntaxNode)rewriter.Visit(original.GetRoot()));

            compilation = compilation.ReplaceSyntaxTree(original,rewritten);
        }

        private void GenerateExecuteAsyncMethod(ref CSharpCompilation compilation, HandlerMethod onGet, HandlerMethod onPost)
        {
            var builder = new StringBuilder();
            builder.AppendLine("public override async Task ExecuteAsync()");
            builder.AppendLine("{");

            if (onGet != null)
            {
                onGet.GenerateCode(builder);
            }

            if (onPost != null)
            {
                onPost.GenerateCode(builder);
            }

            builder.AppendLine("await (this.View().ExecuteResultAsync(this.PageContext));");

            builder.AppendLine("}");

            var parsed = CSharpSyntaxTree.ParseText(builder.ToString());
            var root = parsed.GetCompilationUnitRoot();
            var method = (MethodDeclarationSyntax)root.DescendantNodes(node => !(node is MethodDeclarationSyntax)).ToArray()[0];

            var original = compilation.SyntaxTrees[0];

            var tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)new AddMemberRewriter(method).Visit(original.GetRoot()));
            compilation = compilation.ReplaceSyntaxTree(original, tree);
        }

        private CompilationException CreateException(RazorCodeDocument document)
        {
            var groups = document.ErrorSink.Errors.GroupBy(e => e.Location.FilePath, StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in groups)
            {
                var filePath = group.Key;
                var fileContent = document.Source.CreateReader().ReadToEnd();
                var compilationFailure = new CompilationFailure(
                    filePath,
                    fileContent,
                    compiledContent: string.Empty,
                    messages: group.Select(e => e.ToDiagnosticMessage()));
                failures.Add(compilationFailure);
            }

            throw new CompilationException(failures);
        }

        private CompilationException CreateException(
            RazorCodeDocument document,
            string relativePath,
            string generatedCode,
            string assemblyName,
            IEnumerable<Diagnostic> diagnostics)
        {
            var diagnosticGroups = diagnostics
                .Where(IsError)
                .GroupBy(diagnostic => GetFilePath(relativePath, diagnostic), StringComparer.Ordinal);

            var source = document.Source.CreateReader().ReadToEnd();

            var failures = new List<CompilationFailure>();
            foreach (var group in diagnosticGroups)
            {
                var sourceFilePath = group.Key;
                string sourceFileContent;
                if (string.Equals(assemblyName, sourceFilePath, StringComparison.Ordinal))
                {
                    // The error is in the generated code and does not have a mapping line pragma
                    sourceFileContent = source;
                    sourceFilePath = "who cares";
                }

                var failure = new CompilationFailure(
                    sourceFilePath,
                    source,
                    generatedCode,
                    group.Select(d => d.ToDiagnosticMessage()));

                failures.Add(failure);
            }

            throw new CompilationException(failures);
        }

        private static string GetFilePath(string relativePath, Diagnostic diagnostic)
        {
            if (diagnostic.Location == Location.None)
            {
                return relativePath;
            }

            return diagnostic.Location.GetMappedLineSpan().Path;
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }

        private Assembly LoadStream(MemoryStream assemblyStream, MemoryStream pdbStream)
        {
#if NET451
            return Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
#else
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(assemblyStream, pdbStream);
#endif
        }

        private string GetNamespace(string relativePath)
        {
            var @namespace = new StringBuilder(_options.DefaultNamespace);
            var parts = Path.GetDirectoryName(relativePath).Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                @namespace.Append(".");
                @namespace.Append(part);
            }

            return @namespace.ToString();
        }

        private class HandlerMethod
        {
            public static HandlerMethod FromSymbol(IMethodSymbol symbol, string verb)
            {
                var isAsync = false;

                INamedTypeSymbol returnType = null;
                if (symbol.ReturnsVoid)
                {
                    // No return type
                }
                else
                {
                    returnType = (INamedTypeSymbol)symbol.ReturnType as INamedTypeSymbol;

                    var getAwaiters = returnType.GetMembers("GetAwaiter");
                    if (getAwaiters.Length == 0)
                    {
                        // This is a synchronous method.
                    }
                    else
                    {
                        // This is an async method.
                        IMethodSymbol getAwaiter = null;
                        for (var i = 0; i < getAwaiters.Length; i++)
                        {
                            var method = getAwaiters[i] as IMethodSymbol;
                            if (method == null)
                            {
                                continue;
                            }

                            if (method.Parameters.Length == 0)
                            {
                                getAwaiter = method;
                                break;
                            }
                        }

                        if (getAwaiter == null)
                        {
                            throw new InvalidOperationException("could not find an GetAwaiter()");
                        }

                        IMethodSymbol getResult = null;
                        var getResults = getAwaiter.ReturnType.GetMembers("GetResult");
                        for (var i = 0; i < getResults.Length; i++)
                        {
                            var method = getResults[i] as IMethodSymbol;
                            if (method == null)
                            {
                                continue;
                            }

                            if (method.Parameters.Length == 0)
                            {
                                getResult = method;
                                break;
                            }
                        }

                        if (getResult == null)
                        {
                            throw new InvalidOperationException("could not find GetResult()");
                        }

                        returnType = getResult.ReturnsVoid ? null : (INamedTypeSymbol)getResult.ReturnType;
                        isAsync = true;
                    }
                }

                return new HandlerMethod()
                {
                    IsAsync = isAsync,
                    ReturnType = returnType,
                    Symbol = symbol,
                    Verb = verb,
                };
            }

            public bool IsAsync { get; private set; }

            public INamedTypeSymbol ReturnType { get; private set; }

            public IMethodSymbol Symbol { get; private set; }

            public string Verb { get; private set; }

            public void GenerateCode(StringBuilder builder)
            {
                builder.AppendFormat(@"
    if (string.Equals(this.Context.Request.Method, ""{0}"", global::System.StringComparison.Ordinal))
    {{",
                    Verb);
                builder.AppendLine();

                for (var i = 0; i < Symbol.Parameters.Length; i++)
                {
                    var parameter = Symbol.Parameters[i];
                    var parameterTypeFullName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    builder.AppendFormat("var param{0} = await BindAsync<{1}>(\"{2}\");", i, parameterTypeFullName, parameter.Name);
                    builder.AppendLine();
                }

                if (IsAsync && ReturnType == null)
                {
                    // async Task
                    builder.AppendFormat("await {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                }
                else if (IsAsync)
                {
                    // async IActionResult
                    builder.AppendFormat("global::Microsoft.AspNetCore.Mvc.IActionResult result = await {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                    builder.AppendLine("if (result != null)");
                    builder.AppendLine("{");
                    builder.AppendLine("await result.ExecuteResultAsync(this.PageContext);");
                    builder.AppendLine("return;");
                    builder.AppendLine("}");
                }
                else if (ReturnType == null)
                {
                    // void
                    builder.AppendFormat("{0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                }
                else
                {
                    // IActionResult
                    builder.AppendFormat("global::Microsoft.AspNetCore.Mvc.IActionResult result = {0}({1});", Symbol.Name, string.Join(", ", Symbol.Parameters.Select((p, i) => "param" + i)));
                    builder.AppendLine();
                    builder.AppendLine("if (result != null)");
                    builder.AppendLine("{");
                    builder.AppendLine("await result.ExecuteResultAsync(this.PageContext);");
                    builder.AppendLine("return;");
                    builder.AppendLine("}");
                }

                builder.AppendLine(@"
        await (this.View().ExecuteResultAsync(this.PageContext));
        return;
    }");
            }
        }
    }
}
