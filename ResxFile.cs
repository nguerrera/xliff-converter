// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;

namespace XliffConverter
{
    internal sealed class ResxFile : IDisposable
    {
        private bool _deleteOnDispose;

        public string Path { get; }

        public bool HasStrings { get; }

        public ResxFile(ITranslatable source)
        {
            _deleteOnDispose = true;
            Path = System.IO.Path.GetTempFileName();

            using (var writer = new ResXResourceWriter(Path))
            {
                foreach (var unit in source.GetTranslationUnits())
                {
                    HasStrings = true;
                    writer.AddResource(new ResXDataNode(unit.Id, unit.Source) { Comment = unit.Note });
                }
            }
        }

        public ResxFile(string originalPath)
        {
            bool changed = false;
            var document = XDocument.Load(originalPath);

            foreach (var node in GetDescendants(document, "data"))
            {
                // remove non-string data (XliffParser will crash on it)
                if (node.Attribute("type") != null || node.Attribute("mimetype") != null)
                {
                    node.Remove();
                    changed = true;
                    continue;
                }

                // remove designer goo that should not be translated
                if (node.Attribute("name").Value.StartsWith(">>") || node.Attribute("name").Value.EndsWith(".LayoutSettings"))
                {
                    node.Remove();
                    changed = true;
                    continue;
                }

                // remove empty strings
                if (string.IsNullOrWhiteSpace(node.Element("value").Value))
                {
                    node.Remove();
                    changed = true;
                    continue;
                }

                HasStrings = true;
            }

            // remove design-time metadata
            foreach (var node in GetDescendants(document, "metadata"))
            {
                node.Remove();
                changed = true;
            }

            if (changed)
            {
                _deleteOnDispose = true;
                Path = System.IO.Path.GetTempFileName();
                document.Save(Path);
            }
            else
            {
                Debug.Assert(!_deleteOnDispose);
                Path = originalPath;
            }
        }

        private static List<XElement> GetDescendants(XDocument document, string d)
        {
            return document.Descendants(d)?.ToList() ?? new List<XElement>(0);
        }

        public void Dispose()
        {
            if (_deleteOnDispose)
            {
                File.Delete(Path);
            }
        }
    }
}