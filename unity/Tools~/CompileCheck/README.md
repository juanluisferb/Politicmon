# CompileCheck

Unity ignores folders ending in `~`, so this does not affect the Unity project.

It compiles every script in `Assets/Scripts` plus the EditMode tests against minimal stand-ins of
the UnityEngine API (`Stubs/`) and runs the NUnit tests with plain .NET 8. Handy for CI or for
checking changes without opening the editor:

```bash
dotnet test "unity/Tools~/CompileCheck"
# Input System code path:
dotnet build "unity/Tools~/CompileCheck" -p:ExtraDefines="ENABLE_INPUT_SYSTEM%3BPOLITICMON_INPUT_SYSTEM"
```

The stubs only implement what the tests need (e.g. `Resources.Load<TextAsset>` reads the JSON files
from `Assets/Resources`); the real behaviour is Unity's.
