// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public class DefaultSyntaxTreePhase : ISyntaxTreePhase
    {
        public RazorEngine Engine { get; set; }

        public void Execute(RazorCodeDocument document)
        {
            var passes = Engine.Features.OfType<ISyntaxTreePass>().OrderBy(p => p.Order).ToArray();

            var syntaxTree = document.GetSyntaxTree();
            if (syntaxTree == null)
            {
                throw new InvalidOperationException("Need a syntax tree");
            }

            foreach (var pass in passes)
            {
                syntaxTree = pass.Execute(document, syntaxTree);
            }

            document.SetSyntaxTree(syntaxTree);
        }
    }
}
