// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace XliffConverter
{
    internal sealed class VsctFile : ITranslatable
    {
        private static readonly XNamespace s_namespace = @"http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable";
        private static readonly XName s_strings = s_namespace + "Strings";
        private static readonly XName s_canonicalName = s_namespace + "CanonicalName";

        public string Path { get; }

        public VsctFile(string path)
        {
            Path = path;
        }

        public IEnumerable<TranslationUnit> GetTranslationUnits()
        {
            var document = XDocument.Load(Path);

            foreach (var strings in document.Descendants(s_strings))
            {
                string id = strings.Parent.Attribute("id").Value;

                foreach (var child in strings.Elements())
                {
                    XName name = child.Name;

                    if (name == s_canonicalName)
                    {
                        // See https://msdn.microsoft.com/en-us/library/bb491712.aspx
                        // LocCanonicalName can be used to specify a localized alternative.
                        continue;
                    }

                    yield return new TranslationUnit($"{id}|{name.LocalName}", child.Value);
                }
            }
        }

        public void SaveAsTranslated(string translatedPath, IReadOnlyDictionary<string, string> translations)
        {
            var document = XDocument.Load(Path);

            foreach (var strings in document.Descendants(s_strings))
            {
                string id = strings.Parent.Attribute("id").Value;

                foreach (var child in strings.Elements())
                {
                    XName name = child.Name;

                    if (name == s_canonicalName)
                    {
                        // See https://msdn.microsoft.com/en-us/library/bb491712.aspx
                        // LocCanonicalName can be used to specify a localized alternative.
                        continue;
                    }

                    child.Value = translations[$"{id}|{name.LocalName}"];
                }
            }

            document.Save(translatedPath);
        }
    }
}
