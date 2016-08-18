// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Compilation.Rewriters
{
    public class AddMemberRewriter : CSharpSyntaxRewriter
    {
        private readonly MemberDeclarationSyntax _member;

        public AddMemberRewriter(MemberDeclarationSyntax member)
        {
            _member = member;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return node.AddMembers(_member);
        }
    }
}
