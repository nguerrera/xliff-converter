// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XliffParser;

namespace XliffConverter
{
    internal static class Converter
    {
        private static readonly string[] s_languages = new[] {
            "cs",
            "de",
            "es",
            "fr",
            "it",
            "ja",
            "ko",
            "pl",
            "pt-BR",
            "ru",
            "tr",
            "zh-Hans",
            "zh-Hant"
        };

        private const string s_xlfDirectoryName = "xlf";

        private static string s_rootDirectory;

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: XliffConverter <root directory>");
            }

            s_rootDirectory = Path.GetFullPath(args[0]);
            ConvertDirectory(s_rootDirectory);
        }

        private static void ConvertDirectory(string directory)
        {
            string directoryName = Path.GetFileName(directory);

            if (directoryName == "bin" || directoryName == "TestAssets")
            {
                return;
            }

            string xlfDirectory = Path.Combine(directory, s_xlfDirectoryName);

            foreach (var resxFile in Directory.EnumerateFiles(directory, "*.resx"))
            {
                ConvertResx(resxFile, xlfDirectory);
            }

            foreach (var vsctFile in Directory.EnumerateFiles(directory, "*.vsct"))
            {
                ConvertVsct(vsctFile, xlfDirectory);
            }

            if (directoryName == "Rules")
            {
                foreach (var xamlFile in Directory.EnumerateFiles(directory, "*.xaml"))
                {
                    ConvertXaml(xamlFile, xlfDirectory);
                }
            }
            
            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                ConvertDirectory(subdirectory);
            }
        }

        private static void ConvertResx(string resxFile, string xlfDirectory)
        {
            ConvertResx(resxFile, xlfDirectory, resxFile);
        }

        private static void ConvertResx(string resxFile, string xlfDirectory, string originalFile)
        {
            bool madeNeutral = false;
            string originalFileName = Path.GetFileName(originalFile);

            if (Path.GetExtension(originalFileName) == ".resx")
            {
                // Make common case of resx original file implicit, but don't remove other extensions 
                // or else a .resx and .vsct with the same name will collide.
                originalFileName = Path.GetFileNameWithoutExtension(originalFileName);
            }

            if (!Directory.Exists(xlfDirectory))
            {
                Directory.CreateDirectory(xlfDirectory);
            }

            foreach (var language in s_languages)
            {
                string xlfFile = Path.Combine(xlfDirectory, $"{originalFileName}.{language}.xlf");
                if (!File.Exists(xlfFile))
                {
                    string originalFileId = MakeOriginalFileId(originalFile);

                    File.WriteAllText(xlfFile,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" version=""1.2"" xsi:schemaLocation=""urn:oasis:names:tc:xliff:document:1.2 xliff-core-1.2-transitional.xsd"">
  <file datatype=""xml"" source-language=""en"" target-language=""{language}"" original=""{originalFileId}"">
    <body>
      <group id=""{originalFileId}"" />
    </body>
  </file>
</xliff>");
                }

                try
                {
                    var xlfDocument = new XlfDocument(xlfFile);
                    xlfDocument.Update(resxFile, updatedResourceStateString: "needs-review-translation", addedResourceStateString: "new");
                    xlfDocument.Save();
                }
                catch (NullReferenceException)
                {
                    // Temp hack to unblock further development: XliffParser cannot deal with non-string values in resx (WinForms style), it casts them to string using as and null refs.
                    Console.WriteLine($"Warning: Failed to process {originalFile}");
                    File.Delete(xlfFile);
                    return;
                }

                if (!madeNeutral)
                {
                    MakeNeutral(xlfFile, Path.Combine(xlfDirectory, $"{originalFileName}.xlf"));
                    madeNeutral = true;
                }
            }
        }

        private static void MakeNeutral(string inputFile, string outputFile)
        {
            var doc = XDocument.Load(inputFile);

            // remove target-language attribute
            var fileNodes = from node in doc.Descendants()
                            where node.Name.LocalName != null && node.Name.LocalName == "file"
                            select node;

            fileNodes.ToList().ForEach(x =>
            {
                if (x.HasAttributes)
                {
                    foreach (var attrib in x.Attributes())
                    {
                        if (attrib.Name == "target-language")
                            attrib.Remove();
                    }
                }
            });

            // remove all target nodes
            var targetNodes = from node in doc.Descendants()
                              where node.Name.LocalName != null && node.Name.LocalName == "target"
                              select node;

            targetNodes.ToList().ForEach(x => x.Remove());

            // save
            var fi = new FileInfo(outputFile);

            if (fi.Exists)
            {
                fi.Delete();
            }
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            doc.Save(fi.FullName);
        }

        private static string MakeOriginalFileId(string originalFile)
        {
            if (!originalFile.StartsWith((s_rootDirectory) + Path.DirectorySeparatorChar))
            {
                Debug.Fail("originalFile should be guaranteed to be under root directory");
                throw Unreachable;
            }

            return originalFile.Substring(s_rootDirectory.Length + 1).Replace("\\", "/");
        }

        private static void ConvertVsct(string vsctFile, string xlfDirectory)
        {
            using (var resxFile = new TemporaryResxFile(new VsctFile(vsctFile)))
            {
                ConvertResx(resxFile.Path, xlfDirectory, vsctFile);
            }
        }

        private static void ConvertXaml(string xamlFile, string xlfDirectory)
        {
            using (var resxFile = new TemporaryResxFile(new XamlFile(xamlFile)))
            {
                ConvertResx(resxFile.Path, xlfDirectory, xamlFile);
            }
        }

        private static Exception Unreachable => new InvalidOperationException("This code path should not be reachable.");
    }
}
