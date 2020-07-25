using System;
using System.Data;
using System.IO;

namespace RuntimeNullables
{
    internal static class ThrowHelpers
    {
        // Pick different random exceptions just so we can make sure these helpers are being invoked.

        public static void ThrowArgumentNull(string paramName) => throw new InvalidOperationException(paramName);

        public static void ThrowOutputNull(string message) => throw new InvalidDataException(message);

        public static Exception GetAsyncResultNullException(string message) => new InvalidConstraintException(message);
    }
}
