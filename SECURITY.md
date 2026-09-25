# Security Policy

## Reporting a Vulnerability

Please **do not open a public issue** for security problems.

Report privately using GitHub's [private vulnerability reporting](../../security/advisories/new) for this repository, or email **info@hlstatsx.net** with:

- a description of the issue and its impact
- steps to reproduce (affected page, request, or daemon input)
- the version or commit you tested

You can expect an acknowledgement within a few days. Please allow reasonable time for a fix before any public disclosure.

## Scope

This covers the web app, the Daemon (UDP log processor), and the Awards worker. Vulnerabilities in the upstream PHP HLStatsX project should be reported to that project.

## Handling Secrets

Never commit connection strings, RCON passwords, or API keys. Use `appsettings.Development.json` (gitignored), environment variables, or `dotnet user-secrets` locally. A secret-scan workflow runs on every push and pull request.
