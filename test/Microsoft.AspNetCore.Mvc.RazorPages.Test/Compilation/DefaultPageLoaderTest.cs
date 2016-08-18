// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class DefaultPageCompilationServiceTest
    {
        [Fact]
        public void Load_EmptyPage_InheritsFromPage()
        {
            // Arrange
            var loader = CreateLoader();

            // Act
            var type = loader.Load("");
            
            // Assert
            Assert.Same(typeof(Page), type.GetTypeInfo().BaseType);
        }

        [Fact]
        public void Load_WithInheritsDirective_InheritsFromSpecifiedClass()
        {
            // Arrange
            var loader = CreateLoader();

            // Act
            var type = loader.Load($"@inherits {typeof(DefaultPageCompilationServiceTest) + "." + typeof(MyBaseClass).Name}");

            // Assert
            Assert.Same(typeof(MyBaseClass), type.GetTypeInfo().BaseType);
        }

        [Fact]
        public void Load_WithInheritsDirective_WithGeneratedConstructor()
        {
            // Arrange
            var compiler = CreateLoader();

            // Act
            var type = compiler.Load($"@inherits {typeof(DefaultPageCompilationServiceTest) + "." + typeof(MyBaseClassWithConstuctor).Name}");

            // Assert
            Assert.Same(typeof(MyBaseClassWithConstuctor), type.GetTypeInfo().BaseType);

            var constructor = Assert.Single(type.GetTypeInfo().DeclaredConstructors);
            Assert.Same(typeof(string), Assert.Single(constructor.GetParameters()).ParameterType);
        }

        // Test for @inherits
        public class MyBaseClass : Page
        {
        }

        public class MyBaseClassWithConstuctor : Page
        {
            public MyBaseClassWithConstuctor(string s)
            {
            }
        }

        private static TestPageLoader CreateLoader()
        {
            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(DefaultPageCompilationServiceTest).GetTypeInfo().Assembly));
            partManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

            var referenceManager = new ApplicationPartManagerReferenceManager(partManager);

            var options = Options.Create(new RazorPagesOptions()
            {
                DefaultNamespace = "TestNamespace",
            });

            var compilationFactory = new DefaultCSharpCompilationFactory(referenceManager, options);

            return new TestPageLoader(options, compilationFactory, new PageRazorEngineHost());

        }

        private class TestPageLoader : DefaultPageLoader
        {
            public TestPageLoader(
                IOptions<RazorPagesOptions> options,
                CSharpCompilationFactory compilationFactory,
                PageRazorEngineHost host)
                : base(options, compilationFactory, host)
            {
            }

            public Type Load(string text)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(text);
                writer.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                var relativePath = "/TestPage";
                var @class = "Generated_TestPage";
                var @namespace = "Microsoft.AspNetCore.Mvc.RazorPages.Compilation";

                var code = GenerateCode(stream, @class, @namespace, relativePath);
                if (!code.Success)
                {
                    throw null;
                }

                var compilation = CreateCompilation(stream, @class, @namespace, relativePath, code.GeneratedCode);
                return Load(compilation, stream, relativePath, code.GeneratedCode);
            }
        }
    }
}
