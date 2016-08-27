// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode
{
    public class Checksum : ICSharpSource
    {
        public string FileName { get; set; }

        public string Guid { get; set; }

        public string Bytes { get; set; }
    }
}
