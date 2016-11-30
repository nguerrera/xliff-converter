// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XliffConverter
{
    internal struct TranslationUnit
    {
        public string Id { get; }

        public string Source { get; }

        public string Note { get; }

        public TranslationUnit(string id, string source, string note = "")
        {
            Id = id;
            Source = source;
            Note = note;
        }
    }
}
