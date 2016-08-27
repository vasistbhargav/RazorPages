// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasChunkToSource
{
    public class UsingDirectiveVisitor : ChunkVisitor
    {
        private readonly CSharpSourceLoweringContext _context;
        private readonly ISet<string> _addedImports;

        public UsingDirectiveVisitor(ISet<string> addedImports, CSharpSourceLoweringContext context)
        {
            _context = context;
            _addedImports = addedImports;
        }

        protected override void Visit(UsingChunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            // We don't want to attempt to add duplicate namespace imports.
            if (!_addedImports.Add(chunk.Namespace))
            {
                return;
            }

            var documentContent = ((Span)chunk.Association).Content.Trim();
            var documentLocation = new MappingLocation(chunk.Start, documentContent.Length);
            var importNamespace = new ImportNamespace
            {
                Namespace = documentContent,
                DocumentLocation = documentLocation
            };

            _context.Builder.Add(importNamespace);
        }
    }
}