// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultPageCompilationServiceTest
    {
        [Fact]
        public void Compile_EmptyPage_InheritsFromPage()
        {
            // Arrange
            var compiler = CreateCompiler();

            // Act
            var type = compiler.Compile("");
            
            // Assert
            Assert.Same(typeof(Page), type.GetTypeInfo().BaseType);
        }

        private static TestPageCompilationService CreateCompiler()
        {
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(DefaultPageCompilationServiceTest).GetTypeInfo().Assembly));
            partManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

            var referenceManager = new ApplicationPartManagerReferenceManager(partManager);

            var options = new OptionsManager<RazorPagesOptions>(Enumerable.Empty<IConfigureOptions<RazorPagesOptions>>());

            var compilationFactory = new DefaultCSharpCompilationFactory(referenceManager, options);

            return new TestPageCompilationService(partManager, compilationFactory, new PageRazorEngineHost(), new TestPageFileProviderAccessor());

        }

        private class TestPageFileProviderAccessor : IPageFileProviderAccessor
        {
            public IFileProvider FileProvider => null;
        }

        private class TestPageCompilationService : DefaultPageCompilationService
        {
            public TestPageCompilationService(
                ApplicationPartManager partManager,
                CSharpCompilationFactory compilationFactory,
                PageRazorEngineHost host,
                IPageFileProviderAccessor fileProvider)
                : base(partManager, compilationFactory, host, fileProvider)
            {
            }

            public Type Compile(string text)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(text);
                stream.Seek(0L, SeekOrigin.Begin);

                var relativePath = "/TestPage";
                var baseClass = "TestPage";
                var @class = "Generated_" + baseClass;
                var @namespace = "Microsoft.AspNetCore.Mvc.RazorPages.Compilation";

                var code = GenerateCode(stream, baseClass, @class, @namespace, relativePath);
                if (!code.Success)
                {
                    throw null;
                }

                var compilation = CreateCompilation(stream, baseClass, @class, @namespace, relativePath, code.GeneratedCode);
                return Load(compilation, stream, relativePath, code.GeneratedCode);
            }
        }
    }
}
