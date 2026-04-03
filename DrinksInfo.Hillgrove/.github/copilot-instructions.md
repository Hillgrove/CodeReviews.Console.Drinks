# Copilot Instructions

## Project Guidelines
- Wants suggestions and refactors to stay YAGNI and lightweight (avoid over-engineering).
- Prefers more readable, explicit code over terse modern shorthand syntax (e.g., avoid collection spread expressions for clarity).
- Keep changes DRY, KISS, and YAGNI, with code written for human readability for readers unfamiliar with the source code.
- Wants SRP refactor to fully separate data retrieval from menu rendering; methods should not both fetch data and show prompts.

## Interaction Preferences
- Discuss solutions only unless explicitly asked for code changes; do not modify files proactively.