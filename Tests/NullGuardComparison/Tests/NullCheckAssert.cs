using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Replaces the Runtime Nullables tests' assertions with ones for the exceptions NullGuard throws: <see cref="ArgumentNullException"/> for inputs, like
/// Runtime Nullables, and <see cref="InvalidOperationException"/> for outputs. Messages aren't compared.
/// </summary>
internal static class NullCheckAssert
{
    public static void ThrowsArgumentNull(string parameterName, Action action)
    {
        var exception = Assert.ThrowsException<ArgumentNullException>(action);
        Assert.AreEqual(parameterName, exception.ParamName);
    }

    public static void ThrowsOutputNull(string message, Action action) => Assert.ThrowsException<InvalidOperationException>(action);

    public static async Task ThrowsOutputNullAsync(string message, Func<Task> action)
    {
        Task task;

        try
        {
            task = action();
        }
        catch (Exception ex)
        {
            Assert.Fail($"The call threw instead of returning a task that fails: {ex}");
            return;
        }

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => task).ConfigureAwait(false);
    }
}

/// <summary>
/// The messages the Runtime Nullables tests pass to the assertions, which aren't compared.
/// </summary>
internal static class NullCheckMessages
{
    public const string ReturnValue = "";

    public const string TaskResult = "";

    public const string AsyncTaskResult = "";

    public const string EnumeratorResult = "";

    public const string AsyncEnumeratorResult = "";

    public static string OutputParameter(string parameterName) => string.Empty;

    public static string Parameter(string parameterName) => string.Empty;
}
