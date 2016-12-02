using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                string id = node.Identifier.Text;
                string text = ((LiteralExpressionSyntax)(node.Initializer.Value)).Token.Text.Trim('"');
                Units.Add(new TranslationUnit(id, text));
                base.VisitVariableDeclarator(node);
            }
        }
    }
}