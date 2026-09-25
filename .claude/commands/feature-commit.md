# Feature Commit

Create a git commit for the current completed feature.

1. Run `git status` and `git diff` to review all changes
2. Run `git log --oneline -5` to match the existing commit message style
3. Stage only the files relevant to this feature (be explicit — avoid `git add .` to prevent accidentally committing appsettings with credentials)
4. Write a concise commit message:
   - Start with an imperative verb: `Add`, `Implement`, `Fix`, `Update`
   - One line summary, optionally a blank line then bullet points for detail
   - Do NOT commit `appsettings.Development.json` — it contains the local DB password
5. Confirm the staged files with Pete before committing

Never force push. Never amend published commits.
