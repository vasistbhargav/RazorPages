using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages.Compilation;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageActivator : IPageActivator
    {
        private readonly IPageCompilationService _compilationService;
        private readonly IFileProvider _fileProvider;

        public DefaultPageActivator(
            IPageCompilationService compilationService,
            IPageFileProviderAccessor fileProvider)
        {
            _compilationService = compilationService;
            _fileProvider = fileProvider.FileProvider;
        }

        public object Create(PageContext context)
        {
            var pageCtors = context.ActionDescriptor.PageType.AsType().GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (pageCtors.Length != 1)
            {
                throw new InvalidOperationException("Page requires a single constructor");
            }
            var ctorParams = pageCtors[0].GetParameters();
            var args = new List<object>();
            foreach (var param in ctorParams)
            {
                args.Add(context.HttpContext.RequestServices.GetService(param.ParameterType));
            }
            
            return Activator.CreateInstance(context.ActionDescriptor.PageType.AsType(), args.ToArray());
        }

        public void Release(PageContext context, object page)
        {
            (page as IDisposable)?.Dispose();
        }
    }
}
