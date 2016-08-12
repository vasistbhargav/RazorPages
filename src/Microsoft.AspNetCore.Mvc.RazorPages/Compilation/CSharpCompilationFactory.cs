using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public abstract class CSharpCompilationFactory
    {
        public abstract CSharpCompilation Create();
    }
}
