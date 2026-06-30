# Lumify – Frontend Reference

Code reference for the Lumify frontend (`lumify.app`), generated from the source
code and its TSDoc comments by TypeDoc.

## Scope

This reference covers the frontend's **public API surface**, not every UI detail:

- **services** – API client services (`services/api`) and shared utilities (`services/utils`)
- **hooks** – the SignalR / presence React hooks
- **models** – TypeScript data types (the frontend mirror of the backend DTOs)
- **providers** – the "intelligent" state/context components (`*Provider`)

For the backend (ASP.NET Core) reference, see the DocFX site under
`0_Documents/Code/docFx/`.

## Regenerate

Run from the **`lumify.app`** directory:

```
npx typedoc --options ../0_Documents/Code/typedoc/typedoc.json
```

Then open `0_Documents/Code/typedoc/_site/index.html`.
