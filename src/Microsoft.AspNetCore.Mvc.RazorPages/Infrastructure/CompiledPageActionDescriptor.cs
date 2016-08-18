﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class CompiledPageActionDescriptor : PageActionDescriptor
    {
        public CompiledPageActionDescriptor(PageActionDescriptor other)
        {
            ActionConstraints = other.ActionConstraints;
            AttributeRouteInfo = other.AttributeRouteInfo;
            BoundProperties = other.BoundProperties;
            DisplayName = other.DisplayName;
            FilterDescriptors = other.FilterDescriptors;
            Parameters = other.Parameters;
            Properties = other.Properties;
            RelativePath = other.RelativePath;
            RouteValues = other.RouteValues;
            ViewEnginePath = other.ViewEnginePath;
        }

        public TypeInfo PageType { get; set; }
    }
}
