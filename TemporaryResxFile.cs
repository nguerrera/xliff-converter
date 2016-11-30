// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Resources;

namespace XliffConverter
{
    internal sealed class TemporaryResxFile : IDisposable
    {
        public string Path { get; }

        public TemporaryResxFile(ITranslatable source)
        {
            Path = System.IO.Path.GetTempFileName();

            using (var writer = new ResXResourceWriter(Path))
            {
                foreach (var unit in source.GetTranslationUnits())
                {
                    writer.AddResource(new ResXDataNode(unit.Id, unit.Source) { Comment = unit.Note });
                }
            }
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}