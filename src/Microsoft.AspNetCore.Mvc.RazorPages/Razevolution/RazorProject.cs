// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution
{
    public abstract class RazorProject
    {
        public static readonly RazorProject Empty = new EmptyRazorProject();

        public static RazorProject Create(IFileProvider provider)
        {
            return new DefaultRazorProject(provider);
        }

        public abstract IEnumerable<RazorProjectItem> EnumerateItems(string pattern);

        public abstract RazorProjectItem GetItem(string relativePath);

        public abstract IEnumerable<RazorProjectItem> EnumerateAscending(string pattern);

        private class DefaultRazorProject : RazorProject
        {
            private readonly IFileProvider _provider;

            public DefaultRazorProject(IFileProvider provider)
            {
                _provider = provider;
            }

            public override IEnumerable<RazorProjectItem> EnumerateAscending(string pattern)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<RazorProjectItem> EnumerateItems(string pattern)
            {
                throw new NotImplementedException();
            }

            public override RazorProjectItem GetItem(string relativePath)
            {
                throw new NotImplementedException();
            }

            private IEnumerable<RazorProjectItem> EnumerateFiles(IDirectoryContents directory, string prefix)
            {
                if (directory.Exists)
                {
                    foreach (var file in directory)
                    {
                        if (file.IsDirectory)
                        {
                            var children = EnumerateFiles(_provider.GetDirectoryContents(file.PhysicalPath), prefix + file.Name + "/");
                            foreach (var child in children)
                            {
                                yield return child;
                            }
                        }
                        else
                        {
                            yield return new RazorPageFileInfo(file, prefix + file.Name);
                        }
                    }
                }
            }

            private class FileInfoRazorProjectItem : RazorProjectItem
            {
                private readonly IFileInfo _fileInfo;

                public FileInfoRazorProjectItem(IFileInfo fileInfo)
                {
                    _fileInfo = fileInfo;
                }
            }
        }

        private class EmptyRazorProject : RazorProject
        {
            public override IEnumerable<RazorProjectItem> EnumerateAscending(string pattern)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            public override IEnumerable<RazorProjectItem> EnumerateItems(string pattern)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            public override RazorProjectItem GetItem(string relativePath)
            {
                return null;
            }
        }
    }
}
