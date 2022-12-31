using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class AsyncEnumeratorTests
    {
        [TestMethod]
        public async Task GoodGetReferencesAsync()
        {
            var enumerator = AsyncEnumerators.GoodGetReferencesAsync();
            while (await enumerator.MoveNextAsync()) { }
        }

        [TestMethod]
        public Task BadGetReferencesAsync()
        {
            return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
                var enumerator = AsyncEnumerators.BadGetReferencesAsync();
                while (await enumerator.MoveNextAsync()) { }
            });
        }

        [TestMethod]
        public async Task GoodGetReferencesAsyncWithoutAwait()
        {
            var enumerator = AsyncEnumerators.GoodGetReferencesAsyncWithoutAwait();
            while (await enumerator.MoveNextAsync()) { }
        }

        [TestMethod]
        public Task BadGetReferencesAsyncWithoutAwait()
        {
            return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
                var enumerator = AsyncEnumerators.BadGetReferencesAsyncWithoutAwait();
                while (await enumerator.MoveNextAsync()) { }
            });
        }

        [TestMethod]
        public async Task GoodGetGenericsAsync()
        {
            var enumerator = AsyncEnumerators.GoodGetGenericsAsync<object>();
            while (await enumerator.MoveNextAsync()) { }
        }

        [TestMethod]
        public Task BadGetGenericsAsync()
        {
            return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
                var enumerator = AsyncEnumerators.BadGetGenericsAsync<object>();
                while (await enumerator.MoveNextAsync()) { }
            });
        }
    }
}