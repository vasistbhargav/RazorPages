// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public class DefaultRazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public DefaultRazorPagesOptionsSetup(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void Configure(RazorPagesOptions options)
        {
            options.DefaultNamespace = _hostingEnvironment.ApplicationName;

            if (_hostingEnvironment.ContentRootFileProvider != null)
            {
                options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
            }

        }
    }
}
