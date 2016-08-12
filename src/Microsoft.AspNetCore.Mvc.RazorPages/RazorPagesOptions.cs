using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class RazorPagesOptions
    {
        public CSharpCompilationOptions CompilationOptions { get; } = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();
    }
}
