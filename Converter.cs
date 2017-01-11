// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XliffParser;

namespace XliffConverter
{
    internal static class Converter
    {
        private static HashSet<string> s_validLanguages = new HashSet<String>(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase);

        private static readonly string[] s_languages = new[]
        {
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
        private static bool s_twoWay;

        private static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "--two-way")
            {
                s_twoWay = true;
                args = args.Skip(1).ToArray();
            }

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: XliffConverter [--two-way] <root directory>");
                return 1;
            }

            s_rootDirectory = Path.GetFullPath(args[0]);
            ConvertDirectory(s_rootDirectory);
            return 0;
        }

        private static void ConvertDirectory(string directory)
        {
            string directoryName = Path.GetFileName(directory);

            if (directoryName == "bin" || directoryName == "TestAssets")
            {
                return;
            }

            string xlfDirectory = Path.Combine(directory, s_xlfDirectoryName);
            foreach (var resxFile in EnumerateNeutralFiles(directory, "*.resx"))
            {
                ConvertResx(resxFile, xlfDirectory);
            }

            foreach (var vsctFile in EnumerateNeutralFiles(directory, "*.vsct"))
            {
                ConvertVsct(vsctFile, xlfDirectory);
            }

            foreach (var csFile in EnumerateAllFiles(directory, "*LocalizableStrings.cs"))
            {
                ConvertCSharp(csFile, xlfDirectory);
            }

            if (directoryName == "Rules")
            {
                foreach (var xamlFile in EnumerateAllFiles(directory, "*.xaml"))
                {
                    ConvertXaml(xamlFile, xlfDirectory);
                }
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                ConvertDirectory(subdirectory);
            }
        }

        private static IEnumerable<string> EnumerateAllFiles(string directory, string searchPattern)
        {
            return Directory.EnumerateFiles(directory, searchPattern);
        }

        private static IEnumerable<string> EnumerateNeutralFiles(string directory, string searchPattern)
        {
            return Directory.EnumerateFiles(directory, searchPattern).Where(f => IsNeutral(f));
        }

        private static IEnumerable<string> EnumerateLocalizedFiles(string directory, string searchPattern)
        {
            return Directory.EnumerateFiles(directory, searchPattern).Where(f => !IsNeutral(f));
        }

        private static void ConvertResxToXlf(string resxFile, string xlfDirectory, string originalFile)
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

                var xlfDocument = new XlfDocument(xlfFile);
                xlfDocument.Update(resxFile, updatedResourceStateString: "needs-review-translation", addedResourceStateString: "new");
                xlfDocument.Save();

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

            foreach (var fileNode in fileNodes.ToList())
            {
                if (fileNode.HasAttributes)
                {
                    foreach (var attribute in fileNode.Attributes())
                    {
                        if (attribute.Name == "target-language")
                            attribute.Remove();
                    }
                }
            }

            // remove all target nodes
            var targetNodes = from node in doc.Descendants()
                              where node.Name.LocalName != null && node.Name.LocalName == "target"
                              select node;

            foreach (var targetNode in targetNodes.ToList())
            {
                targetNode.Remove();
            }

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

        private static bool IsNeutral(string file)
        {
            var withoutExtension = Path.GetFileNameWithoutExtension(file);
            var possibleLanguage = Path.GetExtension(withoutExtension)?.Trim('.');

            if (string.IsNullOrEmpty(possibleLanguage))
            {
                return true;
            }

            return !s_validLanguages.Contains(possibleLanguage);
        }

        private static void ConvertResx(string resxFile, string xlfDirectory)
        {
            using (var temporaryResxFile = new ResxFile(resxFile))
            {
                if (temporaryResxFile.HasStrings)
                {
                    ConvertResxToXlf(temporaryResxFile.Path, xlfDirectory, resxFile);
                    if (s_twoWay)
                    {
                        ConvertXlfToResx(xlfDirectory, resxFile);
                    }
                }
            }
        }

        private static void ConvertXlfToResx(string xlfDirectory, string resxFile)
        {
            foreach (var xlfFile in EnumerateLocalizedFiles(xlfDirectory, $"{Path.GetFileNameWithoutExtension(resxFile)}.*.xlf"))
            {
                if (xlfFile.Contains(".vsct.") != resxFile.Contains(".vsct."))
                {
                    // quick fix to deal with vsct and resx with same base name
                    continue;
                }

                var xlfDocument = new XlfDocument(xlfFile);

                var translatedResxFile =
                    Path.ChangeExtension(
                        Path.Combine(
                            Path.GetDirectoryName(xlfDirectory),
                            Path.GetFileName(xlfFile)),
                        ".resx");

                xlfDocument.SaveAsResX(translatedResxFile);
            }
        }

        private static void ConvertVsct(string vsctFile, string xlfDirectory)
        {
            // convert vsct -> xlf
            using (var resxFile = new ResxFile(new VsctFile(vsctFile)))
            {
                if (resxFile.HasStrings)
                {
                    ConvertResxToXlf(resxFile.Path, xlfDirectory, vsctFile);
                }

                if (s_twoWay)
                {
                    ConvertXlfToVsct(xlfDirectory, vsctFile);
                }
            }
        }

        private static void ConvertXlfToVsct(string xlfDirectory, string vsctFilePath)
        {
            var vsctFile = new VsctFile(vsctFilePath);

            foreach (var xlfFile in EnumerateAllFiles(xlfDirectory, $"{Path.GetFileName(vsctFilePath)}.*.xlf"))
            {
                var language = Path.GetExtension(Path.GetFileNameWithoutExtension(xlfFile)).TrimStart('.');

                var translatedVsctFilePath =
                    Path.ChangeExtension(
                        Path.Combine(
                            Path.GetDirectoryName(xlfDirectory),
                            Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(xlfFile))),
                        $"{language}.vsct");

                vsctFile.SaveAsTranslated(translatedVsctFilePath, XlfFile.GetTranslations(xlfFile));
            }
        }

        private static void ConvertXaml(string xamlFile, string xlfDirectory)
        {
            using (var resxFile = new ResxFile(new XamlFile(xamlFile)))
            {
                if (resxFile.HasStrings)
                {
                    ConvertResxToXlf(resxFile.Path, xlfDirectory, xamlFile);
                }

                if (s_twoWay)
                {
                    ConvertXlfToXaml(xlfDirectory, xamlFile);
                }
            }
        }

        private static void ConvertXlfToXaml(string xlfDirectory, string xamlFilePath)
        {
            var xamlFile = new XamlFile(xamlFilePath);

            foreach (var xlfFile in EnumerateAllFiles(xlfDirectory, $"{Path.GetFileName(xamlFilePath)}.*.xlf"))
            {
                var language = Path.GetExtension(Path.GetFileNameWithoutExtension(xlfFile)).TrimStart('.');
                var translatedDirectory = Path.Combine(Path.GetDirectoryName(xlfDirectory), language);
                var translatedXamlFilePath = Path.Combine(translatedDirectory, Path.GetFileName(xamlFilePath));

                Directory.CreateDirectory(translatedDirectory);
                xamlFile.SaveAsTranslated(translatedXamlFilePath, XlfFile.GetTranslations(xlfFile));
            }
        }

        private static void ConvertCSharp(string csharpFile, string xlfDirectory)
        {
            using (var resxFile = new ResxFile(new CSharpFile(csharpFile)))
            {
                if (resxFile.HasStrings)
                {
                    // NOTE: Putting a File.Copy of resxFile.Path here will give us the initial resx files 
                    // once CLI is ready to build with them instead of the temporary LocalizableStrings.cs files. :)

                    // Fake the original file name as resx since these files will be converted to resx
                    // in source and we want to avoid churn in xlf file when that happens. It also avoids
                    // confusing .cs.cs.xlf files for cs locale.
                    string originalFile = Path.ChangeExtension(csharpFile, ".resx");

                    ConvertResxToXlf(resxFile.Path, xlfDirectory, originalFile);
                }
            }
        }

        private static Exception Unreachable => new InvalidOperationException("This code path should not be reachable.");
    }
}
