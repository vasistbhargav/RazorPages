// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode
{
    public class SetTagHelperProperty : ICSharpSource, IProjection
    {
        public string TagHelperTypeName { get; set; }

        public string PropertyName { get; set; }

        public string Value { get; set; }

        public MappingLocation DocumentLocation { get; set; }
    }
}
