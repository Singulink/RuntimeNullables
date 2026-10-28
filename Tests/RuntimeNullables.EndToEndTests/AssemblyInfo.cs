using Microsoft.VisualStudio.TestTools.UnitTesting;

// Every test builds its own project in its own directory, and most of the time is spent waiting for builds.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
