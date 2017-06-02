# NOTE

This tool is obsolete and being replaced by https://github.com/dotnet/xliff-tasks

# xliff-converter

This is a glorified script to convert resx files, vsct files, xaml rule files, and temporary hard-coded LocalizableStrings.cs files to xliff and back.

It is an interim solution to allow creating the xliff files before we have a centralized solution for integrating the conversion in to the build.

It is not designed to be general purpose, there's barely enough here to bootstrap four specific repos, and there are assumptions baked in based on that.


# Acquisition

Clone the repo and build. (See above for rationale. This is temporary.)


# Usage

## Convert to xlf
```
XliffConverter <repository root>
```

## Convert to xlf and back
```
XliffConverter --two-way <repository root>
```

Tip: Save your work and do a full git clean to avoid time spent hunting for things to replace in .gitignored places.

This will scan the repo for resx files (ignoring bin\ and TestAssets), vsct, xaml files in a folder named "Rules", and LocalizableStrings.cs, then convert/sync them with the xliff files in xlf/ subdirectories. (If there are no xlf files yet, they will be created.)


