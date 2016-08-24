// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public class VirtualDocumentSyntaxTreePass : ISyntaxTreePass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 0;

        public RazorSyntaxTree Execute(RazorCodeDocument document, RazorSyntaxTree syntaxTree)
        {
            var trees = document.GetVirtualSyntaxTrees();
            if (trees.Count == 0)
            {
                return syntaxTree;
            }

            var errors = new List<RazorError>(syntaxTree.Diagnostics);
            var blockBuilder = new BlockBuilder(syntaxTree.Root);
            
            for (var i = 0; i < trees.Count; i++)
            {
                var tree = trees[i];

                foreach (var node in tree.Root.Children)
                {
                    blockBuilder.Children.Insert(i, tree.Root);
                }
                
                errors.AddRange(tree.Diagnostics);
                foreach (var error in tree.Diagnostics)
                {
                    document.ErrorSink.OnError(error);
                }
            }

            return RazorSyntaxTree.Create(blockBuilder.Build(), errors);
        }
    }
}
