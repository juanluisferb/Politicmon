using NUnit.Framework;

/// <summary>Points the Resources stub at the real Assets/Resources folder before any test runs.</summary>
[SetUpFixture]
public class ResourcesRoot
{
    [OneTimeSetUp]
    public void Init()
    {
        string dir = System.AppContext.BaseDirectory;
        while (dir != null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir, "Assets", "Resources")))
            dir = System.IO.Path.GetDirectoryName(dir);
        UnityEngine.Resources.Root = System.IO.Path.Combine(dir, "Assets", "Resources");
    }
}
