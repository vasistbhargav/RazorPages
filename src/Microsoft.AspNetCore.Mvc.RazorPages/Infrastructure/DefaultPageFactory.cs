// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageFactory : IPageFactory
    {
        private readonly IPageActivator _activator;

        public DefaultPageFactory(IPageActivator activator)
        {
            _activator = activator;
        }

        public object CreatePage(PageContext context)
        {
            var page = (Page)_activator.Create(context);

            page.PageContext = context;

            var properties = page.GetType().GetTypeInfo().GetProperties();
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute(typeof(RazorInjectAttribute)) != null)
                {
                    var service = context.HttpContext.RequestServices.GetRequiredService(property.PropertyType);
                    (service as IViewContextAware)?.Contextualize(context);

                    property.SetValue(page, service);
                }
            }

            return page;
        }

        public void ReleasePage(PageContext context, object page)
        {
            _activator.Release(context, page);
        }
    }
}
