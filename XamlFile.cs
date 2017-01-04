// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace XliffConverter
{
    internal sealed class XamlFile : ITranslatable
    {
        public string Path { get; }

        public XamlFile(string path)
        {
            Path = path;
        }

        public IEnumerable<TranslationUnit> GetTranslationUnits()
        {
            var document = XDocument.Load(Path);

            foreach (var element in document.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (XmlName(attribute) != "DisplayName" && XmlName(attribute) != "Description")
                    {
                        continue;
                    }

                    yield return new TranslationUnit(GenerateId(attribute), attribute.Value);
                }
            }
        }

        public void SaveAsTranslated(string translatedPath, IReadOnlyDictionary<string, string> translations)
        {
            var document = XDocument.Load(Path);

            foreach (var element in document.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (XmlName(attribute) != "DisplayName" && XmlName(attribute) != "Description")
                    {
                        continue;
                    }

                    attribute.Value = translations[GenerateId(attribute)];
                }
            }

            document.Save(translatedPath);
        }

        private string GenerateId(XAttribute attribute)
        {
            XElement parent = attribute.Parent;

            if (XmlName(parent) == "EnumValue")
            {
                XElement grandparent = parent.Parent;
                return $"{XmlName(parent)}|{AttributedName(grandparent)}.{AttributedName(parent)}|{XmlName(attribute)}";
            }

            return $"{XmlName(parent)}|{AttributedName(parent)}|{XmlName(attribute)}";
        }

        private static string XmlName(XElement element) => element.Name.LocalName;

        private static string XmlName(XAttribute attribute) => attribute.Name.LocalName;

        private static string AttributedName(XElement element) => element.Attribute("Name").Value;
    }
}
