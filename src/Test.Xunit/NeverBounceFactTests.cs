namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using Xunit;

    /// <summary>
    /// Runs every Touchstone descriptor sequentially through a single xUnit fact,
    /// honoring suite lifecycle hooks and preserving test order.
    /// </summary>
    public sealed class NeverBounceFactTests : TouchstoneFactBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return NeverBounceSuites.All; }
        }

        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
