using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Asserts that an injected null check fails with the exception the standard throw helpers create.
/// </summary>
internal static class NullCheckAssert
{
    /// <summary>
    /// Asserts that the action throws an <see cref="ArgumentNullException"/> for the parameter.
    /// </summary>
    public static void ThrowsArgumentNull(string parameterName, Action action)
    {
        var exception = Assert.ThrowsException<ArgumentNullException>(action);
        Assert.AreEqual(parameterName, exception.ParamName);
    }

    /// <summary>
    /// Asserts that the action throws a <see cref="NullReferenceException"/> with the message (see <see cref="NullCheckMessages"/>).
    /// </summary>
    public static void ThrowsOutputNull(string message, Action action)
    {
        var exception = Assert.ThrowsException<NullReferenceException>(action);
        Assert.AreEqual(message, exception.Message);
    }

    /// <summary>
    /// Asserts that the action returns a task that fails with a <see cref="NullReferenceException"/> with the message (see <see
    /// cref="NullCheckMessages"/>). Async results are checked when the task completes, so the call itself must not throw.
    /// </summary>
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

        var exception = await Assert.ThrowsExceptionAsync<NullReferenceException>(() => task).ConfigureAwait(false);
        Assert.AreEqual(message, exception.Message);
    }
}

/// <summary>
/// The messages of the exceptions that output checks throw with the standard throw helpers.
/// </summary>
internal static class NullCheckMessages
{
    public const string ReturnValue = "Return value nullability contract was broken.";

    public const string TaskResult = "Task result nullability contract was broken.";

    public const string AsyncTaskResult = "Async task result nullability contract was broken.";

    public const string EnumeratorResult = "Enumerator result nullability contract was broken.";

    public const string AsyncEnumeratorResult = "Async enumerator result nullability contract was broken.";

    public static string OutputParameter(string parameterName) => $"Output parameter '{parameterName}' nullability contract was broken.";

    /// <summary>
    /// Gets the message for a by-value argument that a postcondition says isn't null when the method returns.
    /// </summary>
    public static string Parameter(string parameterName) => $"Parameter '{parameterName}' nullability contract was broken.";
}
