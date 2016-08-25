// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
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
            foreach (var item in EnumerateItems())
            {
                if (item.Filename.StartsWith("_"))
                {
                    // Pages like _PageImports should not be routable.
                    continue;
                }

                AddActionDescriptors(context.Results, item);
            }
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }

        private void AddActionDescriptors(IList<ActionDescriptor> actions, RazorProjectItem item)
        {
            var template = GetRouteTemplate(item);

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
                DisplayName = $"Page: {item.Path}",
                FilterDescriptors = filters,
                RelativePath = item.CominedPath,
                RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "page", item.PathWithoutExtension },
                },
                ViewEnginePath = item.Path,
            });
        }

        private IEnumerable<RazorProjectItem> EnumerateItems()
        {
            return _project.EnumerateItems("/Pages", ".razor");
        }

        private string GetRouteTemplate(RazorProjectItem item)
        {
            var source = item.ToSourceDocument();
            var syntaxTree = RazorParser.Parse(source);

            var template = PageDirectiveFeature.GetRouteTemplate(syntaxTree);

            if (template != null && template.Length > 0 && template[0] == '/')
            {
                return template.Substring(1);
            }

            if (template != null && template.Length > 1 && template[0] == '~' && template[1] == '/')
            {
                return template.Substring(1);
            }

            var @base = item.PathWithoutExtension.Substring(1);
            if (string.Equals("Index", @base, StringComparison.OrdinalIgnoreCase))
            {
                @base = string.Empty;
            }

            if (@base == string.Empty && string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }
            else if (string.IsNullOrEmpty(template))
            {
                return @base;
            }
            else if (@base == string.Empty)
            {
                return template;
            }
            else
            {
                return @base + "/" + template;
            }
        }
    }
}
