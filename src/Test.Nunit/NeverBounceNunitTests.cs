namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Exposes each Touchstone descriptor as an individual NUnit test case.
    /// </summary>
    [TestFixture]
    public sealed class NeverBounceNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(NeverBounceSuites.All);
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
