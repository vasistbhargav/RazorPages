using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation
{
    public class PageCodeGenerator : CSharpCodeGenerator
    {
        public PageCodeGenerator(CodeGeneratorContext context) 
            : base(context)
        {
        }

        protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpCodeVisitor = base.CreateCSharpCodeVisitor(writer, context);

            var attributeRenderer = new MvcTagHelperAttributeValueCodeRenderer(new GeneratedTagHelperAttributeContext()
            {
                CreateModelExpressionMethodName = "CreateModelExpression",
                ModelExpressionProviderPropertyName = "ModelExpressionProvider",
                ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression",
                ViewDataPropertyName = "ViewData",
            });
            csharpCodeVisitor.TagHelperRenderer.AttributeValueCodeRenderer = attributeRenderer;

            return csharpCodeVisitor;
        }

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            return base.BuildClassDeclaration(writer);
        }

        protected override void BuildConstructor(CSharpCodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var baseTypeVisitor = new CSharpBaseTypeVisitor(writer, Context);
            baseTypeVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            var baseTypeName = baseTypeVisitor.CurrentBaseType ?? Host.DefaultBaseClass;
            
            writer.WriteLineHiddenDirective();
            var baseType = Type.GetType($"{baseTypeName}, {Assembly.GetEntryAssembly().FullName}");
            var pageCtors = baseType.GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (pageCtors.Length != 1)
            {
                throw new InvalidOperationException("Page base type requires a single constructor");
            }
            var ctorParams = pageCtors[0].GetParameters().ToDictionary(p => p.ParameterType.Name, p => p.Name);

            writer.Write($"public {Context.ClassName}(");
            writer.Write(string.Join(", ", ctorParams.Select(p => $"{p.Key} {p.Value}")));
            writer.Write(") : base(");
            writer.Write(string.Join(", ", ctorParams.Select(p => $"{p.Value}")));
            writer.Write(")");
            writer.WriteLine();
            writer.WriteLine("{");
            writer.WriteLine("}");
            writer.WriteLine();

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, "Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute");
            injectVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            var modelVisitor = new ModelChunkVisitor(writer, Context);
            modelVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);
            if (modelVisitor.ModelType != null)
            {
                writer.WriteLine();

                // public ModelType Model => ViewData?.Model ?? default(ModelType);
                writer.Write("public ").Write(modelVisitor.ModelType).Write(" Model => ViewData?.Model ?? default(").Write(modelVisitor.ModelType).Write(");");
            }

            writer.WriteLine();

            var viewDataType = $"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<{modelVisitor.ModelType ?? Context.ClassName}>";
            writer.Write("public new ").Write(viewDataType).Write(" ViewData").WriteLine();
            writer.Write("{").WriteLine();
            writer.IncreaseIndent(4);
            writer.Write("get { return (").Write(viewDataType).Write(")base.ViewData; }").WriteLine();
            writer.DecreaseIndent(4);
            writer.Write("}").WriteLine();

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }

        private class ModelChunkVisitor : CodeVisitor<CSharpCodeWriter>
        {
            public ModelChunkVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
                : base(writer, context)
            {
            }

            public string ModelType { get; set; }
            
            public override void Accept(Chunk chunk)
            {
                if (chunk is ModelChunk)
                {
                    Visit((ModelChunk)chunk);
                }
                else
                {
                    base.Accept(chunk);
                }
            }

            private void Visit(ModelChunk chunk)
            {
                ModelType = chunk.ModelType;
            }
        }
    }
}
