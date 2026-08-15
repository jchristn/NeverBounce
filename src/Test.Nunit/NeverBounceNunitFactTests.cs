namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Runs every Touchstone descriptor sequentially through a single NUnit test.
    /// </summary>
    [TestFixture]
    public sealed class NeverBounceNunitFactTests : TouchstoneNunitBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return NeverBounceSuites.All; }
        }

        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
