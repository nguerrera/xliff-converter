// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XliffConverter
{
    internal static class XlfFile
    {
        public static IReadOnlyDictionary<string, string> GetTranslations(string path)
        {
            var document = XDocument.Load(path);
            var dictionary = new Dictionary<string, string>();

            foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "trans-unit"))
            {
                dictionary.Add(
                    element.Attributes().Single(a => a.Name.LocalName == "id").Value,
                    element.Elements().Single(e => e.Name.LocalName == "target").Value);
            }

            return dictionary;
        }
    }
}
