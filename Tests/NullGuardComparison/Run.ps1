<#
.SYNOPSIS
Runs the Runtime Nullables behavior tests against NullGuard builds of the test assemblies and lists the tests that NullGuard fails, by test class.
#>

$ErrorActionPreference = 'Stop'

$testProject = Join-Path $PSScriptRoot 'Tests\NullGuardComparison.Tests.csproj'
$resultsDirectory = Join-Path $PSScriptRoot 'Tests\TestResults'

# Tests that fail with NullGuard because they use something only Runtime Nullables has, rather than because of what NullGuard checks.
$notApplicable = [ordered]@{
    'NullChecksAttributesTests' = '[NullChecks] attributes'
    'NestedContainerTests.Unchecked' = '[NullChecks] attributes'
    'InputParametersTests.NoNullChecksReference' = '[NullChecks] attributes'
    'ReturnsTests.NoNullChecksBadReference' = '[NullChecks] attributes'
    'ExtensionMembersTests.UncheckedProperty' = '[NullChecks] attributes'
    'ExtensionMembersTests.UncheckedGenericProperty' = '[NullChecks] attributes'
    'RuntimeAsyncMethodsTests.UncheckedBadReferenceAsync' = '[NullChecks] attributes'
    'ExcludedNamespacesTests' = 'namespaces that Runtime Nullables skips'
    'GeneratedCodeTests.NestedMethod' = 'Runtime Nullables treats types nested in generated types as generated, like the compiler'
}

if (Test-Path $resultsDirectory) {
    Remove-Item $resultsDirectory -Recurse -Force
}

$buildOutput = dotnet build $testProject --configuration Release --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    $buildOutput | Write-Host
    throw 'The build failed.'
}

# Failing tests are expected, so the exit code doesn't matter.
dotnet test $testProject --configuration Release --no-build --nologo --verbosity quiet --logger trx --results-directory $resultsDirectory | Out-Null

$resultsFile = Get-ChildItem $resultsDirectory -Filter '*.trx' | Select-Object -First 1

if ($null -eq $resultsFile) {
    throw "The tests didn't run."
}

$testRun = ([xml](Get-Content $resultsFile.FullName -Raw)).TestRun
$classNames = @{}

foreach ($test in $testRun.TestDefinitions.UnitTest) {
    $classNames[$test.id] = $test.TestMethod.className.Split('.')[-1]
}

$results = foreach ($result in $testRun.Results.UnitTestResult) {
    $class = $classNames[$result.testId]
    $name = "$class.$($result.testName)"
    $message = [string]$result.Output.ErrorInfo.Message

    $reason = $null

    $outcome =
        if ($result.outcome -eq 'Passed') { 'Passed' }
        elseif ($notApplicable.Contains($class)) { 'Not applicable'; $reason = $notApplicable[$class] }
        elseif ($notApplicable.Contains($name)) { 'Not applicable'; $reason = $notApplicable[$name] }
        elseif ($message -match 'No exception thrown') { 'Not checked' }
        elseif ($message -match 'InvalidProgramException') { 'Invalid code' }
        elseif ($message -match 'threw exception:\s+System\.(ArgumentNull|InvalidOperation)Exception') { 'False error' }
        else { 'Other'; $reason = ($message -replace '\s+', ' ').Trim() }

    [pscustomobject]@{ Class = $class; Test = $result.testName; Outcome = $outcome; Reason = $reason }
}

$packages = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Packages.props') -Raw)).Project.ItemGroup.PackageVersion
$nullGuardVersion = ($packages | Where-Object Include -eq 'NullGuard.Fody').Version
$passedCount = @($results | Where-Object Outcome -eq 'Passed').Count
$notApplicableCount = @($results | Where-Object Outcome -eq 'Not applicable').Count

Write-Host "NullGuard.Fody $nullGuardVersion passes $passedCount of $($results.Count) tests ($notApplicableCount aren't applicable)."
Write-Host 'Not checked: no exception for a null that breaks the annotations. False error: an exception without one. Invalid code: InvalidProgramException.'

foreach ($group in $results | Group-Object Class | Sort-Object Name) {
    $failures = @($group.Group | Where-Object Outcome -ne 'Passed' | Sort-Object Outcome, Test)

    if ($failures.Count -eq 0) {
        continue
    }

    Write-Host ''
    Write-Host "$($group.Name): $($group.Count - $failures.Count) of $($group.Count) passed"

    foreach ($failure in $failures) {
        $suffix = if ($failure.Reason) { " ($($failure.Reason))" } else { '' }
        Write-Host "  $($failure.Outcome): $($failure.Test)$suffix"
    }
}

# dotnet test failed, but that's expected.
exit 0
