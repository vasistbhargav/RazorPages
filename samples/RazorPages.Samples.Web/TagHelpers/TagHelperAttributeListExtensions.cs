
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorPages.Samples.Web.TagHelpers
{
    public static class TagHelperAttributeListExtensions
    {
        public static CssClassAttributeManager GetCssClassManager(this TagHelperAttributeList attributes)
        {
            return new CssClassAttributeManager(attributes);
        }

        public static CssClassAttributeManager GetCssClassManager(this TagHelperAttributeList attributes, HtmlEncoder htmlEncoder)
        {
            return new CssClassAttributeManager(attributes, htmlEncoder);
        }
    }
}
