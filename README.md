# xliff-converter

This is a glorified script to convert resx files, vsct files, xaml rule files, and temporary hard-coded LocalizableStrings.cs files to xliff to handoff to the localization team.

It is an interim solution to allow creating the xliff files before we have a centralized solution for integrating the conversion in to the build.

It is not designed to be general purpose, there's barely enough here to bootstrap four specific repos, and there are assumptions baked in based on that.


# Acquisition

Clone the repo and build. (See above for rationale. This is temporary.)


# Usage
```
XliffConverter <repository root>
```

This will scan the repo for resx files (ignoring bin\ and TestAssets), vsct, xaml files in a folder named "Rules", and LocalizableStrings.cs, then convert/sync them with the xliff files in xlf/ subdirectories. (If there are no xlf files yet, they will be created.)


