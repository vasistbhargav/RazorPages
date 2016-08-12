using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public abstract class ReferenceManager
    {
        public abstract IReadOnlyList<MetadataReference> References { get; }
    }
}
