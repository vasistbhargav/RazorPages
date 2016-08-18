// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public interface ISyntaxTreePass : IRazorEngineFeature
    {
        int Order { get; }

        RazorSyntaxTree Execute(RazorCodeDocument document, RazorSyntaxTree syntaxTree);
    }
}
