using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Collects the events an MSBuild task logs, so tasks can be tested without running MSBuild.
/// </summary>
internal sealed class BuildEngineStub : IBuildEngine
{
    public List<BuildErrorEventArgs> Errors { get; } = [];

    public List<BuildWarningEventArgs> Warnings { get; } = [];

    public List<BuildMessageEventArgs> Messages { get; } = [];

    public bool ContinueOnError => false;

    public int LineNumberOfTaskNode => 0;

    public int ColumnNumberOfTaskNode => 0;

    public string ProjectFileOfTaskNode => "Test.targets";

    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);

    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);

    public void LogCustomEvent(CustomBuildEventArgs e) { }

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) =>
        throw new NotSupportedException();
}
