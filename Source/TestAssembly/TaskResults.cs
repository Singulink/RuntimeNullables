using System.Threading.Tasks;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace TestAssembly
{
    public static class TaskResults
    {
        public static Task<string> GoodReference()
        {
            return Task.FromResult<string>(string.Empty);
        }

        public static Task<string> BadReference()
        {
            return Task.FromResult<string>(null!);
        }

        public static Task<T> GoodGeneric<T>() where T : notnull, new()
        {
            return Task.FromResult<T>(new T());
        }

        public static Task<T> BadGeneric<T>() where T : notnull
        {
            return Task.FromResult<T>(default!);
        }

        public static async Task<string> GoodReferenceAsync()
        {
            await Task.Delay(10);
            return string.Empty;
        }

        public static async Task<string> BadReferenceAsync()
        {
            await Task.Delay(10);
            return null!;
        }

        public static async Task<string> GoodReferenceAsyncWithoutAwait()
        {
            return string.Empty;
        }

        public static async Task<string> BadReferenceAsyncWithoutAwait()
        {
            return null!;
        }

        public static async ValueTask<string> GoodReferenceValueAsync()
        {
            await Task.Delay(10);
            return string.Empty;
        }

        public static async ValueTask<string> BadReferenceValueAsync()
        {
            await Task.Delay(100);
            return null!;
        }

        public static async ValueTask<string> GoodReferenceValueAsyncWithoutAwait()
        {
            return string.Empty;
        }

        public static async ValueTask<string> BadReferenceAsyncValueWithoutAwait()
        {
            return null!;
        }

        public static async Task<T> GoodGenericAsync<T>() where T : notnull, new()
        {
            await Task.Delay(10);
            return new T();
        }

        public static async Task<T> BadGenericAsync<T>() where T : notnull
        {
            await Task.Delay(10);
            return default!;
        }

        public static Task<bool> DefaultValueType()
        {
            return Task.FromResult(false);
        }

        public static Task<bool?> DefaultNullableValueType()
        {
            return Task.FromResult((bool?)null);
        }
    }
}
