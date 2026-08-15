namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Xunit;
    using global::Xunit.Abstractions;

    /// <summary>
    /// Exposes each non-skipped Touchstone descriptor as an individual xUnit theory row
    /// for per-test visibility in the IDE Test Explorer.
    /// </summary>
    public sealed class NeverBounceTheoryTests
    {
        private readonly ITestOutputHelper _Output;

        public NeverBounceTheoryTests(ITestOutputHelper output)
        {
            _Output = output;
        }

        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in NeverBounceSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip)
                        data.Add(testCase);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            _Output.WriteLine($"Running: {testCase.DisplayName}");
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
