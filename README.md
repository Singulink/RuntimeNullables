# Runtime Nullables

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.RuntimeNullables.svg)](https://www.nuget.org/packages/Singulink.RuntimeNullables/)
[![Downloads](https://img.shields.io/endpoint?url=https%3A%2F%2Fwww.singulink.com%2Fbadges%2Fnuget-downloads.php%3Fid%3Druntimenullables)](https://www.nuget.org/packages?q=packageid%3ASingulink.RuntimeNullables+packageid%3ARuntimeNullables.Fody)
[![Build and Test](https://github.com/Singulink/RuntimeNullables/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/RuntimeNullables/actions?query=workflow%3A%22build+and+test%22)

> [!IMPORTANT]
> **`RuntimeNullables.Fody` is now `Singulink.RuntimeNullables`.** Version 3 is published under a new package name and works on its own, without Fody. If you use `RuntimeNullables.Fody`, switch to the new package: see [Migrating from RuntimeNullables.Fody](#migrating-from-runtimenullablesfody).

**Runtime Nullables** turns your C# nullable annotations into runtime null checks. Add the package, keep using nullable annotations as you normally would, and leave out the manual null checks: they are injected into the compiled assembly for you.

It is designed to be "set it and forget it". The defaults suit nearly every project, so adding the package is all the setup there is, and we use it across our own libraries and applications with the default settings. The [settings](#configuration) are there for the rare cases where you need them.

- **Library best practices without the boilerplate:** your public API validates its arguments like the .NET libraries do, without writing any `ThrowIfNull` calls. Getting your annotations right is all it takes.
- **Errors where the bug is:** a null that breaks your annotations throws right away, e.g. `ArgumentNullException` with the parameter's name at the call that passed it, instead of a `NullReferenceException` somewhere else, much later.
- **Annotations you can rely on:** nulls can't slip past them from code without nullable annotations, other .NET languages, reflection, deserialization or a misplaced `!`. In Debug builds, your own return values and `out` parameters are checked too, so your tests catch your code breaking its own annotations.

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

Add the package to your project:

```xml
<PackageReference Include="Singulink.RuntimeNullables" Version="3.0.0" PrivateAssets="all" />
```

That's it. Null checks are injected into the assembly every time the project is compiled.

`PrivateAssets="all"` keeps the package from becoming a dependency of projects and packages that use yours, since it is only needed at build time (you get warning `RN0003` without it). Once the checks are injected, your assembly doesn't reference `RuntimeNullables.dll`, so no extra files are added to your build output.

**Requirements**: MSBuild 17 or later (Visual Studio 2022, or the .NET 6 SDK or later), and a C# project that compiles with the standard `CoreCompile` target.

If you add the package to every project with central package management's `GlobalPackageReference`, NuGet leaves out its compile assets, so null checks are injected but the `[NullChecks]` attribute isn't available. To use the attribute, also add this to `Directory.Build.targets`:

```xml
<ItemGroup>
  <PackageReference Update="Singulink.RuntimeNullables" IncludeAssets="all" />
</ItemGroup>
```

## What Gets Checked

By default:

| | Debug builds (`DEBUG` defined) | Release builds (no `DEBUG`) |
|---|---|---|
| **Inputs**: parameters, including property setter values | All members | Public API only |
| **Outputs**: return values, `out` and `ref` parameters, task results, iterator items, and arguments that attributes say aren't null afterwards | All members | Not checked |

The public API is what code in other assemblies can call: the public members (and protected members, unless the type is sealed) of types they can access, and members that override or implement them, such as an internal class's implementation of a public interface. Release builds guard your code's entry points against bad input from callers, while debug builds, where you develop and run your tests, also catch your own code breaking its annotations.

Outputs include:
- `out` and `ref` parameters, checked when the method returns
- the results of `Task<T>` and `ValueTask<T>`, both from `async` methods and from tasks that are returned already completed (e.g. by `Task.FromResult`)
- the items produced by iterators (`IEnumerable<T>`, `IEnumerator<T>`) and async iterators (`IAsyncEnumerable<T>`, `IAsyncEnumerator<T>`)
- arguments that `[NotNull]` or `[NotNullWhen]` promise aren't null when the method returns, like the argument of `void ThrowIfNull([NotNull] object? value)`. The caller's argument is checked, even if the method assigns to the parameter.

Null inputs throw `ArgumentNullException` with the parameter's name. Null outputs throw `NullReferenceException` with a message that says which output was null, e.g. "Output parameter 'value' nullability contract was broken."

`async` methods are checked the same way whether the compiler turns them into state machines or into .NET 11 runtime async methods, which are currently opt-in (`<Features>$(Features);runtime-async=on</Features>` with the .NET 11 SDK). One thing differs: a state machine method has a stub that runs the input checks before the task is created, but a runtime async method doesn't, so a null input doesn't throw from the call. Like any other exception in an `async` method, it goes in the returned task and is thrown when the task is awaited.

C# 14 extension members are checked like extension methods: the parameter of an `extension` block is an input of each of its members.

The checks follow the standard annotations:
- `?` on types, and the `notnull` and `class` constraints on generic type parameters. A value of an unconstrained generic type `T` can be null, so it is only checked if an attribute disallows null.
- the `[AllowNull]`, `[DisallowNull]`, `[MaybeNull]` and `[NotNull]` attributes.
- the conditional `[MaybeNullWhen]` and `[NotNullWhen]` attributes, which are checked against the method's `bool` return value. For example, the `out` parameter of `bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)` is checked when the method returns `true`. Like the compiler, Runtime Nullables ignores them on methods that don't return `bool`.
- `[NotNullIfNotNull]`: a return value or `out` or `ref` parameter with it is checked when the named argument isn't null (the caller's argument, even if the method assigns to the parameter).

In code compiled with nullable reference types disabled, types don't say whether values can be null, so only the attributes above lead to checks there. Generated code isn't checked at all: types, methods and properties marked `[GeneratedCode]` or `[CompilerGenerated]` (and the types nested in them), such as source generator output, and lambdas and local functions, which the compiler marks as generated. Neither are types in the `RuntimeNullables`, `System.Runtime.CompilerServices` and `System.Diagnostics.CodeAnalysis` namespaces, where projects put their own throw helpers and copies of the nullable attributes.

## Examples

With the default settings, the output checks below only happen in debug builds.

```c#
using System.Diagnostics.CodeAnalysis;
using RuntimeNullables;

// Throws ArgumentNullException if 'name' is null. The return value is nullable, so it isn't checked.
public string? FindMessage(string name);

// 'value' is checked when the method is called and again when it returns, since it's a ref parameter.
public void UpdateMessage(ref string value);

// [MaybeNull] allows the method to set 'value' to null, so it is only checked when the method is called.
public void ResetMessage([MaybeNull] ref string value);

// 'value' may only be null when the method returns false, so it is checked when the method returns true.
public bool TryGetMessage(string name, [MaybeNullWhen(false)] out string value);

// The same contract with a nullable type: 'value' is checked when the method returns true.
public bool TryFindMessage(string name, [NotNullWhen(true)] out string? value);

// [NotNull] promises the caller that 'value' isn't null when the method returns, so it is checked then.
public void EnsureMessage([NotNull] string? value);

// The return value is checked when 'value' isn't null.
[return: NotNullIfNotNull(nameof(value))]
public string? NormalizeMessage(string? value);

// Each item is checked as it is produced.
public IEnumerable<string> GetMessages()
{
    yield return "some value";
    yield return null!; // NullReferenceException
}

// The result of the task is checked.
public async Task<string> GetMessageAsync()
{
    return await MessageService.GetMessageAsync();
}

// So is the result of a task that is returned already completed.
public Task<T> GetValueAsync<T>() where T : notnull
{
    return Task.FromResult<T>(default!); // NullReferenceException if T is a reference type
}

// [return: NullChecks(false)] turns off the check for this return value.
[return: NullChecks(false)]
public Task<T> GetValueOrDefaultAsync<T>() where T : notnull
{
    return Task.FromResult<T>(default!); // Not checked
}

// Each item of an async iterator is checked too.
public async IAsyncEnumerable<string> GetMessagesAsync()
{
    await Task.Delay(100);
    yield return "some value";
    await Task.Delay(100);
    yield return null!; // NullReferenceException
}
```

## System.Text.Json and ASP.NET Core

System.Text.Json calls your DTOs' constructors and property setters when it deserializes them, and Runtime Nullables checks those like any other code. So by default, a JSON payload with `null` for a non-nullable member throws `ArgumentNullException` in the middle of deserialization. System.Text.Json doesn't turn that into a `JsonException`, so an ASP.NET Core endpoint answers a bad request body with a 500 server error instead of a 400.

Set up System.Text.Json to enforce the annotations itself, so it rejects bad input with a `JsonException` (a 400 from ASP.NET Core) before any of your code runs:
- `RespectNullableAnnotations` rejects `null` for non-nullable properties and constructor parameters.
- `RespectRequiredConstructorParameters` rejects JSON without a value for a constructor parameter, which would otherwise be passed `null` (or its default value).

Both options need System.Text.Json 9 or later, which is part of .NET 9 and is also available as a package for older targets.

```c#
// Minimal APIs:
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

// MVC controllers:
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.RespectNullableAnnotations = true;
    options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
});

// Source-generated contexts used on their own (e.g. AppJsonContext.Default.CreateUser):
[JsonSourceGenerationOptions(RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(CreateUser))]
internal partial class AppJsonContext : JsonSerializerContext;

// A DTO that works well with these options:
public class CreateUser(string username)
{
    // Constructor parameters and required properties must be in the JSON, and non-nullable ones can't be null.
    public string Username { get; } = username;
    public required string Email { get; init; }

    // Optional: a default value or a nullable type.
    public bool IsEnabled { get; init; } = true;
    public string? Nickname { get; init; }
}
```

With these options, a nullable constructor parameter or `required` property must still be in the JSON, even though it can be `null`. To let JSON leave such members out (they're then `null`), add a modifier that makes them optional:

```c#
static void MakeNullableMembersOptional(JsonTypeInfo typeInfo)
{
    foreach (var property in typeInfo.Properties)
    {
        if (property.IsRequired && property.IsSetNullable)
            property.IsRequired = false;
    }
}

options.TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(MakeNullableMembersOptional);

// Or, with a source-generated context:
options.TypeInfoResolver = AppJsonContext.Default.WithAddedModifier(MakeNullableMembersOptional);
```

Don't silence the compiler's warning about a non-nullable property with `= null!`: System.Text.Json doesn't treat the property as required, so JSON that leaves it out leaves it `null` without calling its setter. Make it `required` instead.

Neither System.Text.Json nor Runtime Nullables checks the elements of collections: `["a", null]` deserializes into a `List<string>`.

## Runtime Nullables vs. NullGuard

[NullGuard](https://github.com/Fody/NullGuard) predates nullable reference types and added support for them on top of its own attribute model. Runtime Nullables was written from the ground up for nullable annotations only, which keeps it simple and lets it go further. Running this repository's tests against NullGuard 3.1.1 ([see how](https://github.com/Singulink/RuntimeNullables/tree/master/Tests/NullGuardComparison)) shows the differences:

| | Runtime Nullables | NullGuard |
|---|---|---|
| Parameters, return values, `out` and `ref` parameters, and properties | ✅ | ✅ |
| `Task<T>` results of `async` methods | ✅ | ✅ |
| `ValueTask<T>` results, and the results of tasks returned already completed (e.g. by `Task.FromResult`) | ✅ | ❌ |
| Items of iterators and async iterators | ✅ | ❌ |
| `[MaybeNullWhen]` and `[NotNullWhen]` outputs, checked against the method's `bool` result | ✅ | ❌ |
| `[NotNullIfNotNull]` outputs | ✅ | ❌ |
| Arguments that `[NotNull]` or `[NotNullWhen]` promise aren't null, e.g. by `ThrowIfNull([NotNull] object? value)` | ✅ | ❌ |
| `in` parameters of virtual methods | ✅ | ❌ (invalid code for generic ones) |
| `ref readonly` return values, by-value `[Out]` parameters and `ref T[]` parameters | ✅ | ❌ |
| Protected members count as public API | ✅ | ❌ |
| `out` parameters in methods with `try`/`finally` | ✅ | ❌ (invalid code) |
| .NET 11 runtime async methods | ✅ | ❌ (invalid code, and false errors for results like `0`) |
| Throws in Debug builds by default | ✅ | ❌ (a failed `Debug.Assert` ends the process first) |
| Exception for null outputs | `NullReferenceException` | `InvalidOperationException` |
| [Custom throw helpers](#custom-throw-helpers) | ✅ | ❌ |
| License | MIT | MIT, but Fody [expects every developer using it to become a Patron](https://github.com/Fody/Home/blob/master/pages/licensing-patron-faq.md) |

"Invalid code" means the code NullGuard injects makes the method throw `InvalidProgramException` when it's called. In Debug builds, NullGuard calls `Debug.Assert` before it throws unless `IncludeDebugAssert` is turned off, and a failed assertion ends the process on .NET when no debugger is attached, so tests that pass null on purpose can't run. Runtime Nullables throws `NullReferenceException` for null outputs because normal code often throws and catches `InvalidOperationException`, so broken annotations could go unnoticed in tests or exception handling code.

Also:
- A single `[NullChecks(bool)]` attribute instead of a set of modes and attributes, and warnings when conflicting annotations (e.g. `[AllowNull, DisallowNull]`) cause checks to be skipped.
- Leaner code: throw helpers instead of throwing directly (smaller IL), and no forced `.locals init`, which would override `[SkipLocalsInit]`.

## Configuration

You'll rarely need anything in this section. Across our own libraries and applications, we only use `[NullChecks(false)]` on code copied from elsewhere that already checks for null itself (such as a polyfill of a .NET type), and in tests that need to pass nulls that the annotations don't allow.

### Settings

The settings are MSBuild properties, set in your project file (or in `Directory.Build.props` for several projects):

```xml
<PropertyGroup>
  <RuntimeNullablesCheckOutputs>true</RuntimeNullablesCheckOutputs>
  <RuntimeNullablesCheckNonPublic>true</RuntimeNullablesCheckNonPublic>
</PropertyGroup>
```

| Property | Default | Description |
|---|---|---|
| `RuntimeNullablesCheckOutputs` | `true` if `DEBUG` is defined, otherwise `false` | Check outputs (see [What Gets Checked](#what-gets-checked)). |
| `RuntimeNullablesCheckNonPublic` | `true` if `DEBUG` is defined, otherwise `false` | Check all members, not just the public API. |
| `RuntimeNullablesEnabled` | `true` | Inject null checks.\* |
| `RuntimeNullablesTreatWarningsAsErrors` | `$(TreatWarningsAsErrors)` | Report the warnings `RN0001`, `RN0004` and `RN0005` (see [Diagnostics](#diagnostics)) as errors. |
| `RuntimeNullablesUseTaskHost` | `false` | Inject null checks in a separate process. Use this if Runtime Nullables conflicts with another MSBuild task that loads a different version of Mono.Cecil into the build process (e.g. in Visual Studio). |

\* With null check injection turned off, the assembly keeps its reference to `RuntimeNullables.dll`, which is then copied to the build output (`PrivateAssets="all"` still keeps it out of packages that depend on yours).

Like any MSBuild property, a setting can have a condition (e.g. on `$(Configuration)`) or be set on the command line (e.g. `dotnet build -p:RuntimeNullablesCheckOutputs=true`).

### The `[NullChecks]` Attribute

`[NullChecks(false)]` turns off null checks for an assembly, module, type, constructor, method, property or return value, and `[NullChecks(true)]` turns them back on for types or members inside it:

```c#
using RuntimeNullables;

[NullChecks(false)]
public class Example
{
    public void NotChecked(string value) { }

    [NullChecks(true)]
    public void Checked(string value) { }
}
```

The settings take precedence, even over `[NullChecks(true)]`: if `RuntimeNullablesCheckOutputs` is `false`, outputs are never checked, and if `RuntimeNullablesCheckNonPublic` is `false`, members outside the public API are never checked. This is intentional, so that the settings alone tell you what a build checks. In the rare case that something should always be checked, such as an output in release builds or an internal method that receives untrusted data, check it in code, where it's clear that the check is always there.

`[NullChecks(true)]` doesn't apply to [generated code](#what-gets-checked) either; putting it there is reported as warning `RN0001`. A type marked as generated counts as generated as a whole, so if a source generator marks its part of one of your partial classes, your own members in that class aren't checked. Generators usually mark only the members they add.

### Custom Throw Helpers

To throw your own exceptions, add an internal `ThrowHelpers` class to your project in the `RuntimeNullables` namespace, with any of the static methods below (same names and signatures). The ones you define are used instead of these defaults:

```c#
using System;

namespace RuntimeNullables;

internal static class ThrowHelpers
{
    internal static void ThrowArgumentNull(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    internal static void ThrowOutputNull(string message)
    {
        throw new NullReferenceException(message);
    }

    internal static Exception GetAsyncResultNullException(string message)
    {
        return new NullReferenceException(message);
    }
}
```

The `Throw...` methods must throw, since the code after the check runs if they return. `GetAsyncResultNullException` creates the exception for null results of `async` methods and async iterators, which ends up in the returned task or async enumerator rather than being thrown to the caller. The `message` parameter says which output was null (see [What Gets Checked](#what-gets-checked)).

## Diagnostics

| Code | Kind | Meaning |
|------|------|---------|
| `RN0001` | Warning | Null checks were skipped, e.g. because of conflicting annotations like `[AllowNull, DisallowNull]`, or `[NullChecks(true)]` is on code that's never checked (generated code and the namespaces listed in [What Gets Checked](#what-gets-checked)). |
| `RN0002` | Warning | Runtime Nullables is configured with legacy Fody configuration that should be migrated (see [Migrating](#migrating-from-runtimenullablesfody)). |
| `RN0003` | Warning | The package reference is missing `PrivateAssets="all"`. |
| `RN0004` | Warning | The assembly's debug symbols couldn't be read, so it has none after the null checks are injected. |
| `RN0005` | Warning | Fody configuration can't be read, so any Runtime Nullables settings in it are ignored. |
| `RN1001` | Error | Null checks couldn't be injected, e.g. because of an invalid custom throw helper. |
| `RN1002` | Error | A setting has an invalid value. |
| `RN1003` | Error | MSBuild is too old. |
| `RN1004` | Error | The project's Fody setup doesn't work after migrating (see [Migrating](#migrating-from-runtimenullablesfody)). |

Like compiler warnings, the warnings can be suppressed with `NoWarn` and reported as errors with `WarningsAsErrors`. `TreatWarningsAsErrors` (or `RuntimeNullablesTreatWarningsAsErrors`) applies to `RN0001`, `RN0004` and `RN0005`, and `-warnaserror` to all of them except `RN0002`.

## Limitations

- Null checks are injected right after compiling, as part of MSBuild's `CoreCompile` target, so they're only injected when the assembly is compiled. Builds that compile without that target aren't supported.
- `[NotNullIfNotNull]` only follows by-value arguments, not `out`, `ref` or `in` ones. Arguments that `[NotNull]` or `[NotNullWhen]` promise aren't null aren't checked for `async` methods and iterators, which return before their code runs.
- Tasks returned by methods that aren't `async` are only checked if they are already completed when they are returned.
- Assemblies signed with an enhanced strong name key (with a hash algorithm other than SHA-1) can't be signed again after the null checks are injected, which is reported as error `RN1001`.
- Like Fody, Runtime Nullables writes the debug symbols again and records their local path in the assembly, so a `PathMap` that the compiler applied to that path (e.g. with `ContinuousIntegrationBuild`) isn't kept. Source file paths in the symbols keep their mapping.

## Migrating from RuntimeNullables.Fody

Version 3 doesn't use [Fody](https://github.com/Fody/Fody) anymore and is published as the `Singulink.RuntimeNullables` package. To upgrade:

1. Replace the `RuntimeNullables.Fody` package reference with `Singulink.RuntimeNullables` (see [Installation](#installation)). Your code doesn't change: the namespace is still `RuntimeNullables`. If you don't use other Fody add-ins, also remove the `Fody` package reference and the `FodyWeavers.xml` and `FodyWeavers.xsd` files.
2. Move your settings to the [MSBuild properties](#settings): `CheckOutputs` becomes `RuntimeNullablesCheckOutputs` and `CheckNonPublic` becomes `RuntimeNullablesCheckNonPublic`. Then remove the `<RuntimeNullables>` element from `FodyWeavers.xml` or the `WeaverConfiguration` property.
3. If you used `DisableFody` or `FodyTreatWarningsAsErrors` to control Runtime Nullables, use `RuntimeNullablesEnabled` and `RuntimeNullablesTreatWarningsAsErrors` instead.

Until you move them, the settings in the old `<RuntimeNullables>` element are still used for settings that aren't set as properties, with warning `RN0002` on every build (for each target framework) that lists the properties to use instead. The warning doesn't fail builds that treat warnings as errors (with `TreatWarningsAsErrors` or `-warnaserror`) unless you list `RN0002` in `WarningsAsErrors`.

If you still use Fody for other add-ins, the element has to go from the project's `FodyWeavers.xml` right away, since Fody fails the build while that file lists an add-in that isn't installed. Runtime Nullables reports this as error `RN1004` before Fody runs, like the other setups that don't work after migrating: the `RuntimeNullables.Fody` package still being installed (the null checks would be injected twice), and the `Fody` package being installed without any add-ins (Fody fails). Otherwise, Fody and Runtime Nullables work together in the same project: both process the compiled assembly, one after the other (in the order NuGet imports their build files).

Other changes:

- Fody skipped add-ins that `FodyWeavers.xml` didn't list, so a project with that file only got null checks if the file listed `<RuntimeNullables>`. Now every project with the package gets them unless it sets `RuntimeNullablesEnabled` to `false`.
- More is checked, so upgrading can surface null bugs that were missed before.
- `[NullChecks(true)]` on code that's never checked, such as generated code, is reported as warning `RN0001`, since it has no effect.
- .NET 11 runtime async methods are supported.
- C# 14 extension members are fully supported.
- Code that uses `NullChecksAttribute` other than as an attribute (e.g. `typeof(NullChecksAttribute)`, which would fail at run time since the assembly doesn't reference `RuntimeNullables.dll` once the checks are injected) is now reported as error `RN1001`.
- Strong-named assemblies are signed again with the same key the compiler used: the key file or key container from the project or the assembly's attributes, as the compiler finds them. Delay-signed and public-signed assemblies are left for you to sign.
