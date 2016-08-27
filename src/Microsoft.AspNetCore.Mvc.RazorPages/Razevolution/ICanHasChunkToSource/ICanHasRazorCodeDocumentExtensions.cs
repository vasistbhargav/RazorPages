using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasChunkToSource
{
    // TODO: Incorporate into normal RazorDocumentExtensions
    public static class ICanHasRazorCodeDocumentExtensions
    {
        public static string GetChecksumBytes(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (string)document.Items[typeof(Checksum)];
        }

        // TODO: This needs to be set somewhere
        public static void SetChecksumBytes(this RazorCodeDocument document, string bytes)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            document.Items[typeof(Checksum)] = bytes;
        }
    }
}
