using NUnit.Framework;
using Politicmon.Core;

namespace Politicmon.Tests
{
    /// <summary>Loads the JSON database from Resources once for all tests.</summary>
    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void LoadData() => GameData.Load();
    }
}
