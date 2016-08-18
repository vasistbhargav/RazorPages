// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public class DefaultChunkTreePhase : IChunkTreePhase
    {
        public RazorEngine Engine { get; set; }

        public void Execute(RazorCodeDocument document)
        {
            var chunkTree = document.GetChunkTree();
            if (chunkTree == null)
            {
                var loweringFeature = Engine.Features.OfType<IChunkTreeLoweringFeature>().FirstOrDefault();
                if (loweringFeature == null)
                {
                    throw new InvalidOperationException("Need to create the chunk tree");
                }

                var syntaxTree = document.GetSyntaxTree();
                if (syntaxTree == null)
                {
                    var syntaxTreePhase = Engine.Phases.OfType<ISyntaxTreePhase>().FirstOrDefault();
                    if (syntaxTreePhase == null)
                    {
                        throw new InvalidOperationException("Need to create the chunk tree");
                    }

                    Engine.ExecutePhase(syntaxTreePhase, document);
                }

                chunkTree = loweringFeature.Execute(document, syntaxTree);
            }

            var passes = Engine.Features.OfType<IChunkTreePass>().OrderBy(p => p.Order).ToArray();
            foreach (var pass in passes)
            {
                chunkTree = pass.Execute(document, chunkTree);
            }

            document.SetChunkTree(chunkTree);
        }
    }
}
