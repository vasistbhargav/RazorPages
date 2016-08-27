// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasChunkToSource
{
    public class DefaultCSharpSourceLoweringFeature : ICSharpSourceLoweringFeature
    {
        // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
        private const string Sha1AlgorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";
        private readonly RazorEngineHost _host;

        public DefaultCSharpSourceLoweringFeature(RazorEngineHost host)
        {
            _host = host;
        }

        public RazorEngine Engine { get; set; }

        public CSharpBlock Execute(RazorCodeDocument document, RazorChunkTree chunkTree)
        {
            var builder = new SourceTreeBuilder();
            var context = new CSharpSourceLoweringContext
            {
                Builder = builder
            };
            var checksum = new Checksum
            {
                FileName = document.Source.Filename,
                Guid = Sha1AlgorithmId,
                Bytes = document.GetChecksumBytes()
            };
            builder.Add(checksum);

            var classInfo = document.GetClassName();
            using (builder.BuildBlock<NamespaceDeclaration>(declaration => declaration.Namespace = classInfo.Namespace))
            {
                context.Builder.Add(new CSharpSource { Code = "#line hidden" });
                AddNamespaceImports(chunkTree, context);

                using (builder.BuildBlock<ClassDeclaration>(
                    declaration =>
                    {
                        declaration.Accessor = "public";
                        declaration.Name = classInfo.Class;
                        new BaseTypeVisitor(declaration).Accept(chunkTree.Root);
                    }))
                {
                    new FunctionsDirectiveVisitor(context).Accept(chunkTree.Root);

                    // TODO: Render design time helpers? This could potentially be placed inside of the execute method as well

                    context.Builder.Add(new CSharpSource { Code = "#pragma warning disable 1998" });

                    using (builder.BuildBlock<ExecuteMethodDeclaration>(
                        declaration =>
                        {
                            declaration.Accessor = "public";
                            declaration.Modifiers = new[] { "override", "async" };
                            declaration.ReturnTypeName = typeof(Task).FullName;
                            declaration.Name = _host.GeneratedClassContext.ExecuteMethodName;
                        }))
                    {
                        builder.Add(new TagHelperFieldDependencyInitialization());


                    }

                        context.Builder.Add(new CSharpSource { Code = "#pragma warning restore 1998" });
                }
            }

            return builder.Root;
        }

        private void AddNamespaceImports(RazorChunkTree chunkTree, CSharpSourceLoweringContext context)
        {
            var defaultImports = _host.NamespaceImports;

            foreach (var import in defaultImports)
            {
                var importSource = new ImportNamespace
                {
                    Namespace = import
                };
                context.Builder.Add(importSource);
            }

            new UsingDirectiveVisitor(defaultImports, context).Accept(chunkTree.Root);
        }
    }
}