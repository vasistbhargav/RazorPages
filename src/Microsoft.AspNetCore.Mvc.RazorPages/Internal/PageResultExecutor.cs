using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageResultExecutor
    {
        public static readonly string DefaultContentType = "text/html; charset=utf-8";

        private readonly HtmlEncoder _htmlEncoder;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IRazorPageActivator _razorPageActivator;

        public PageResultExecutor(
            IHttpResponseStreamWriterFactory writerFactory, 
            IRazorViewEngine razorViewEngine,
            IRazorPageActivator razorPageActivator,
            HtmlEncoder htmlEncoder)
        {
            WriterFactory = writerFactory;
            _razorViewEngine = razorViewEngine;
            _razorPageActivator = razorPageActivator;
            _htmlEncoder = htmlEncoder;
        }

        protected IHttpResponseStreamWriterFactory WriterFactory { get; }

        public Task ExecuteAsync(PageContext pageContext, PageViewResult result)
        {
            if (result.Model != null)
            {
                result.Page.PageContext.ViewData.Model = result.Model;
            }

            var view = new RazorView(_razorViewEngine, _razorPageActivator, new IRazorPage[0], result.Page, _htmlEncoder);
            return ExecuteAsync(pageContext, view, result.ContentType, result.StatusCode);
        }

        public virtual async Task ExecuteAsync(
            PageContext pageContext,
            IView view,
            string contentType,
            int? statusCode)
        {
            if (pageContext == null)
            {
                throw new ArgumentNullException(nameof(pageContext));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var response = pageContext.HttpContext.Response;

            string resolvedContentType = null;
            Encoding resolvedContentTypeEncoding = null;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                contentType,
                response.ContentType,
                DefaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (statusCode != null)
            {
                response.StatusCode = statusCode.Value;
            }

            using (var writer = WriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                pageContext.Writer = writer;

                await view.RenderAsync(pageContext);

                // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
                // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
                // response as part of the Dispose which has a perf impact.
                await writer.FlushAsync();
            }
        }
    }
}
