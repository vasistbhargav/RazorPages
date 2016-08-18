// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public class DefaultCSharpSourceLoweringPhase : ICSharpSourceLoweringPhase
    {
        private readonly PageRazorEngineHost _host;

        public DefaultCSharpSourceLoweringPhase(PageRazorEngineHost host)
        {
            _host = host;
        }

        public RazorEngine Engine { get; set; }

        public void Execute(RazorCodeDocument document)
        {
            var chunkTree = document.GetChunkTree();

            var classInfo = document.GetClassName();

            var chunkGeneratorContext = new ChunkGeneratorContext(_host, classInfo.Class, classInfo.Namespace, document.Source.Filename, shouldGenerateLinePragmas: true);
            chunkGeneratorContext.ChunkTreeBuilder = new AspNetCore.Razor.Chunks.ChunkTreeBuilder();
            chunkGeneratorContext.ChunkTreeBuilder.Root.Association = chunkTree.Root.Association;
            chunkGeneratorContext.ChunkTreeBuilder.Root.Start = chunkTree.Root.Start;

            foreach (var chunk in chunkTree.Root.Children)
            {
                chunkGeneratorContext.ChunkTreeBuilder.Root.Children.Add(chunk);
            }

            var codeGeneratorContext = new CodeGeneratorContext(chunkGeneratorContext, document.ErrorSink);

            var codeGenerator = new PageCodeGenerator(codeGeneratorContext);
            var codeGeneratorResult = codeGenerator.Generate();

            document.SetGeneratedCSharpDocument(new GeneratedCSharpDocument()
            {
                GeneratedCode = codeGeneratorResult.Code,
            });
        }
    }
}
