// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;

namespace XliffConverter
{
    internal class CSharpFile : ITranslatable
    {
        private IEnumerable<TranslationUnit> _units;

        public CSharpFile(string path)
        {
            var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.OpenRead(path)));
            var visitor = new Visitor();
            visitor.Visit(tree.GetRoot());
            _units = visitor.Units.AsReadOnly();
        }

        public IEnumerable<TranslationUnit> GetTranslationUnits() => _units;

        private sealed class Visitor : CSharpSyntaxWalker
        {
            public List<TranslationUnit> Units { get; } = new List<TranslationUnit>();
            
            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                var initializer = ((LiteralExpressionSyntax)(node.Initializer.Value));
                var id = node.Identifier.Text;
                var text = (string)initializer.Token.Value;

                Units.Add(new TranslationUnit(id, text));
                base.VisitVariableDeclarator(node);
            }
        }
    }
}