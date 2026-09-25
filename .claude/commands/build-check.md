# Build Check

Run a full build and the non-repository test suite. Report results clearly.

```bash
dotnet build --no-incremental
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```

**If the build fails with MSB3027 or MSB3021 (file copy errors):** This means Visual Studio is running and has locked the output DLLs. Ask Pete to stop his VS debug session and retry — this is not a code error.

**If tests fail:** Show the failing test names and error messages. Diagnose the root cause before suggesting a fix.

**If everything passes:** Confirm with a summary: `X projects built, Y tests passed`.
