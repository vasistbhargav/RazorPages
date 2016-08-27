// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode;
using Microsoft.AspNetCore.Razor.Chunks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasChunkToSource
{
    public class BaseTypeVisitor : ChunkVisitor
    {
        private readonly ClassDeclaration _classDeclaration;

        public BaseTypeVisitor(ClassDeclaration classDeclaration)
        {
            _classDeclaration = classDeclaration;
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            _classDeclaration.BaseTypeName = chunk.TypeName;
        }
    }
}