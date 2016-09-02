using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorPages.Samples.Web.TagHelpers
{
    public class CssClassAttributeManager
    {
        private readonly TagHelperAttributeList _attributes;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly string[] _existingClasses;
        private readonly TagHelperAttribute _classAttribute;

        private HashSet<string> _classNames;

        public CssClassAttributeManager(TagHelperAttributeList attributes)
            : this(attributes, HtmlEncoder.Default)
        {

        }

        public CssClassAttributeManager(TagHelperAttributeList attributes, HtmlEncoder htmlEncoder)
        {
            _attributes = attributes;
            _htmlEncoder = htmlEncoder;
            _classAttribute = _attributes["class"];

            var classAttributeHtml = _classAttribute?.Value as IHtmlContent;
            if (classAttributeHtml != null)
            {
                var writer = new StringWriter();
                classAttributeHtml.WriteTo(writer, _htmlEncoder);
                _existingClasses = new[] { writer.ToString() };
            }
            else
            {
                _existingClasses = _classAttribute?.Value?.ToString().Split(' ');
            }
        }

        public ICollection<string> ClassNames
        {
            get
            {
                if (_classNames == null)
                {
                    _classNames = _existingClasses != null ? new HashSet<string>(_existingClasses) : new HashSet<string>();
                }

                return _classNames;
            }
        }

        public CssClassAttributeManager Add(string className)
        {
            ClassNames.Add(className);
            return this;
        }

        public CssClassAttributeManager Add(params string[] classNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                ClassNames.Add(classNames[i]);
            }
            return this;
        }

        public CssClassAttributeManager Remove(string className)
        {
            ClassNames.Remove(className);
            return this;
        }

        public CssClassAttributeManager Remove(params string[] classNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                ClassNames.Remove(classNames[i]);
            }
            return this;
        }

        public void Merge()
        {
            if (_classAttribute != null)
            {
                _attributes.Remove(_classAttribute);
            }
            _attributes.Add("class", new CssClassNames(ClassNames));
        }

        private class CssClassNames : IHtmlContent
        {
            private readonly IEnumerable<string> _classNames;

            public CssClassNames(IEnumerable<string> classNames)
            {
                _classNames = classNames;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                var isFirst = true;
                foreach (var name in _classNames)
                {
                    if (!isFirst)
                    {
                        writer.Write(' ');
                    }
                    isFirst = false;
                    writer.Write(name);
                }
            }
        }
    }
}
