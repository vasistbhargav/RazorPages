// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public abstract class RazorProjectItem
    {
        public abstract string BasePath { get; }
        
        public abstract string Path { get; }

        public string CominedPath
        {
            get
            {
                if (BasePath == "/")
                {
                    return Path;
                }
                else
                {
                    return BasePath + Path;
                }
            }
        }

        public string Extension
        {
            get
            {
                var index = Path.LastIndexOf('.');
                if (index == -1)
                {
                    return null;
                }
                else
                {
                    return Path.Substring(index);
                }
            }
        }
        
        public string PathWithoutExtension
        {
            get
            {
                var index = Path.LastIndexOf('.');
                if (index == -1)
                {
                    return Path;
                }
                else
                {
                    return Path.Substring(0, index);
                }
            }
        }
    }
}
