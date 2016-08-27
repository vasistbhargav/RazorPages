// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode
{
    public class ClassDeclaration : CSharpBlock
    {
        public string Accessor { get; set; }

        public string Name { get; set; }

        public string BaseTypeName { get; set; }

        public IEnumerable<string> ImplementedInterfaceNames { get; set; }
    }
}
