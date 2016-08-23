// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly RazorProject _project;
        private readonly MvcOptions _options;

        public PageActionDescriptorProvider(
            RazorProject project,
            IOptions<MvcOptions> options)
        {
            _project = project;
            _options = options.Value;
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            foreach (var file in EnumerateFiles())
            {
                AddActionDescriptors(context.Results, file);
            }
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }

        private void AddActionDescriptors(IList<ActionDescriptor> actions, RazorProjectItem file)
        {
            var template = file.PathWithoutExtension.Substring(1);
            if (string.Equals("Index", template, StringComparison.OrdinalIgnoreCase))
            {
                template = string.Empty;
            }

            var filters = new List<FilterDescriptor>(_options.Filters.Count);
            for (var i = 0; i < _options.Filters.Count; i++)
            {
                filters.Add(new FilterDescriptor(_options.Filters[i], FilterScope.Global));
            }

            actions.Add(new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = template,
                },
                DisplayName = $"Page: {file.Path}",
                FilterDescriptors = filters,
                RelativePath = file.CominedPath,
                RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "page", file.PathWithoutExtension },
                },
                ViewEnginePath = file.Path,
            });
        }

        private IEnumerable<RazorProjectItem> EnumerateFiles()
        {
            return _project.EnumerateItems("/Pages", ".razor");
        }
    }
}
