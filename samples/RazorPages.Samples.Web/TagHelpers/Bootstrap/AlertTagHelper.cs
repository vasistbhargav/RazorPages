using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorPages.Samples.Web.TagHelpers.Bootstrap
{
    [HtmlTargetElement("b:alert")]
    public class AlertTagHelper : TagHelper
    {
        private static readonly IHtmlContent _dismissButtonHtml = new HtmlString("<button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-label=\"close\"><span aria-hidden=\"true\">&times;</span></button>");
        private readonly HtmlEncoder _htmlEncoder;

        public AlertTagHelper(HtmlEncoder htmlEncoder)
        {
            _htmlEncoder = htmlEncoder;
        }

        public bool Visible { get; set; } = true;

        public Emphasis Emphasis { get; set; } = Emphasis.Info;

        public bool Dismissible { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            /*
            <div class="alert alert-info alert-dismissable" role="alert">
                <button type="button" class="close" data-dismiss="alert" aria-label="close"><span aria-hidden="true">&times;</span></button>
                Alert message here
            </div>
            */

            if (!Visible)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";

            var cssClasses = output.Attributes.GetCssClassManager(_htmlEncoder);
            cssClasses.Add("alert");
            // PERF: Allocatey
            cssClasses.Add("alert-" + Emphasis.ToString("G").ToLowerInvariant());

            if (Dismissible)
            {
                cssClasses.Add("alert-dismissable");
                output.Content.AppendHtml(_dismissButtonHtml);
            }

            cssClasses.Merge();
            output.Attributes.Add("role", "alert");

            output.Content.AppendHtml(await output.GetChildContentAsync());
        }
    }
}
