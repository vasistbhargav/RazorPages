// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultCSharpCompilationFactory : CSharpCompilationFactory
    {
        private readonly ReferenceManager _referenceManager;
        private readonly RazorPagesOptions _options;

        public DefaultCSharpCompilationFactory(ReferenceManager referenceManager, IOptions<RazorPagesOptions> options)
        {
            _referenceManager = referenceManager;
            _options = options.Value;
        }

        public override CSharpCompilation Create()
        {
            return CSharpCompilation.Create(
                assemblyName: Path.GetRandomFileName(),
                references: _referenceManager.References,
                options: _options.CompilationOptions);
        }
    }
}
