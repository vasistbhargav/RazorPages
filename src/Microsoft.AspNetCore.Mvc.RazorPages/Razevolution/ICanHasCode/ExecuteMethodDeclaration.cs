// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode
{
    public class ExecuteMethodDeclaration : CSharpBlock
    {
        public string Accessor { get; set; }

        public IEnumerable<string> Modifiers { get; set; }

        public string Name { get; set; }

        public string ReturnTypeName { get; set; }

        // Does not currently support modification of parameters.
    }
}
