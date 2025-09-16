# Code Coverage Analyzer

## Overview

**Code Coverage Analyzer** is a simple utility that can be used to read a `.coverage` file and analyze the results to determine if code coverage is met.

The utility is a console application that can be invoked command line via the `CodeCoverageAnalyzer.exe`.

It should be noted that this tool was created as a simple approach to provide a way to analyze coverage on a per namespace basis and fail a build if coverage not met. Additionally, this tool was created to bypass the need to purchase any software or use third party libraries to accomplish this. Ideally, once Microsoft adds this support out of the box, this tool could be shelved.

## Arguments

There are a number of arguments you can pass to the `.exe`, listed below.

Short Name | Long Name | Description | Type
---------|----------|---------|---------
 -a | --action | string | Specify Analysis, DataSetToXml, or Validate, defaults to Validate if not specified
 -f | --coverage-file | string | Path to coverage file to be read
 -o | --output-file | string | Location of where to output results, if not specified results are only output to console
 -g | --targets-file | string | Path to file containing coverage targets used to validate coverage is met
 -d | --dll-paths | comma delim string | Paths to tests DLLs included in the coverage file
 -s | --symbol-paths | comma delim string | Paths to symbol files associated with the dllPaths
 -e | --error-status-code | int | Exit status code to assign to Environment.ExitCode if an Exception occurs, defaults to -1
 -x | --throw-exception | bool | If analyzer finds areas below coverage throw the exception, if False exception message is only output but not actually thrown
 -p | --pause-before-exit | bool | True to have console pause awaiting Enter key to be pressed before exiting

You can pass `--help` to get help information

```console
Capax.CodeCoverage.MSTest.Analyzer.exe --help
```

You can pass `--version` to get version information

```console
Capax.CodeCoverage.MSTest.Analyzer.exe --version
```

## Executing the analzyer

To run the analyzer just call the `.exe` and pass in the arguments, the `--coverage-file`

```console
CodeCoverageAnalyzer.exe -f "<path to your .coverage file>"
```

The exe can also be called from PowerShell, you just need to give it the path of the exe, `cd` to the directory where the `exe` resides and run the following from PowerShell:

```powershell
$currDir = (Get-Location).Path
$exePath = "$currDir\CodeCoverageAnalyzer.exe

& $exePath -f "<path to your .coverage file>"
```

## Targets File

A targets file can be used to specify the coverage threshold to use on a per namespace basis. The location of the file can be specified using the `--targets-file` argument but if not specified defaults to a file located at `App_Data\CoverageTargets.json`. Either edit that file or create a new one and specify location using `--targets-file` argument.

`NamespaceTargets` can be used in the file to indicate threshold for a specific namespace. Anything not specified in the `NamespaceTargets` will use whatever value is specified for the `DefaultTarget`.

Example `CoverageTargets.json`

```json
{
  // default for all found in coverage
  "DefaultTarget": 80,
  // override default using 
  // key: "string" - identifies the namespace
  // value: "int" - threshold for that namespace
  // example: 
  //   NamespaceTargets: { "foo.namespace.bar": 85 }
  "NamespaceTargets": {},
  // list of any modules to exclude from analysis
  "ExcludeModules": [ "moq.dll" ],
  // list of any namespaces to exclude from analysis 
  "ExcludeNamespaces": [],
  // list of any classes to exclude, a StartsWith check is done
  // so be aware of the value specified, this is different from namespace and modules
  "ExcludeClasses": ["Autofac"]
}
```

## Coverage Files

The CodeCoverageAnalyzer is only responsible for reading the `.coverage` file. It does not generate the file and does not filter the results of the file. A `.coverage` file can be generated in a number of different ways prior to calling `CodeCoverageAnalyzer.exe`.

The following links provide some details:

* [Use code coverage to determine how much code is being tested](https://docs.microsoft.com/en-us/visualstudio/test/using-code-coverage-to-determine-how-much-code-is-being-tested?view=vs-2019)
* [Analyze code coverage from the command line](https://docs.microsoft.com/en-us/visualstudio/test/using-code-coverage-to-determine-how-much-code-is-being-tested?view=vs-2019#analyze-code-coverage-from-the-command-line)
* [Customize code coverage analysis](https://docs.microsoft.com/en-us/visualstudio/test/customizing-code-coverage-analysis?view=vs-2019)
* [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21)
* [Monitor and analyze test run](https://github.com/Microsoft/vstest-docs/blob/master/docs/analyze.md)

Using CLI [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) can also be used for collecting code coverage, for example:

```powershell
$test_proj = "<path to proj>"
$run_settings_file = "<path to .runsettings>"

dotnet test $test_proj --settings:$run_settings_file --no-build --no-restore
```

or alternatively

```powershell
$test_proj = "<path to proj>"

dotnet test $test_proj --collect:"Code Coverage"
```

## .NET Framework 4.7+ Required

The utility makes use of the `Microsoft.VisualStudio.Coverage.Analysis.dll` that is located at for example:

```text
%PROGRAMFILES(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\PrivateAssemblies\Microsoft.VisualStudio.Coverage.Analysis.dll
```

The Interop for this assembly is copied to the `bin` folder so as long as you run this on a machine wiht .NET Framework 4.7+ installed this should work as expected.

It should be noted that a NuGet package containing this DLL could not be found. It is shipped with the [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.SDK) for example but adding it to a project causes other issues.

Also notable is that the [Microsoft.CodeCoverage](https://www.nuget.org/packages/Microsoft.CodeCoverage/16.3.0-preview-20190808-03) NuGet package does contain a `CodeCoverage.exe` in the NuGet packages. However, **caution** running the CodeCoverage.exe produced results that did not match the same results seen when viewing the .coverage file in Visual Studio. Using the `Microsoft.VisualStudio.Coverage.Analysis.dll` did however produce expected results which is why this route was taken.

A final note, attempts to roll this into .NET Core or .NET Standard project were attempted but since the Microsoft.VisualStudio.Coverage.Analysis.dll being used needs .NET Framework this was not possible at this time.
