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
    public class FunctionsDirectiveVisitor : ChunkVisitor
    {
        private readonly CSharpSourceLoweringContext _context;

        public FunctionsDirectiveVisitor(CSharpSourceLoweringContext context)
        {
            _context = context;
        }

        protected override void Visit(TypeMemberChunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (string.IsNullOrEmpty(chunk.Code))
            {
                var documentLocation = new MappingLocation(chunk.Start, chunk.Code.Length);
                var code = new CSharpSource
                {
                    Code = chunk.Code,
                    DocumentLocation = documentLocation
                };

                _context.Builder.Add(code);
            }
        }
    }
}