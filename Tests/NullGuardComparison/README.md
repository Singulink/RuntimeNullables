# NullGuard Comparison

Runs the Runtime Nullables behavior tests (`Tests/RuntimeNullables.Tests/Checks`) against the test assemblies' sources woven with [NullGuard](https://github.com/Fody/NullGuard) instead of Runtime Nullables, to see what NullGuard checks differently. It isn't part of the solution or CI, so the repository's build doesn't need Fody.

To run it, set the versions of `NullGuard.Fody` and `Fody` in `Directory.Packages.props`, then run `Run.ps1`. It lists the tests that fail with NullGuard by test class:
- **Not checked**: NullGuard doesn't throw for a null that breaks the annotations.
- **False error**: NullGuard throws for a value that doesn't break them.
- **Invalid code**: NullGuard's woven code throws `InvalidProgramException`.
- **Not applicable**: the test relies on something only Runtime Nullables has, such as `[NullChecks]` attributes. `Run.ps1` lists these tests.

NullGuard is set up like the Runtime Nullables test assemblies: it checks everything (`ValidationFlags.All`), except in TestAssemblyNonPublic, where it only checks the public API's inputs (`ValidationFlags.AllPublicArguments`), and it throws in Debug builds too (`IncludeDebugAssert="false"`). The tests expect NullGuard's exceptions: `ArgumentNullException` for inputs, like Runtime Nullables, and `InvalidOperationException` for outputs. Messages aren't compared.
