// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageHandlerMethodSelector : IPageHandlerMethodSelector
    {
        public HandlerMethodDescriptor Select(PageContext context)
        {
            foreach (var handler in context.ActionDescriptor.HandlerMethods)
            {
                if (handler.Method.Name.StartsWith("OnGet", StringComparison.Ordinal) && 
                    string.Equals("GET", context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    return handler;
                }

                if (handler.Method.Name.StartsWith("OnPost", StringComparison.Ordinal) &&
                    string.Equals("POST", context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
