# Features Structure

Feature-first operation slices:
- `Features/<Feature>/<Operation>/`

Type conventions:
- Commands: `XxxCommand`, `XxxCommandHandler`
- Queries: `XxxQuery`, `XxxQueryHandler`
- Keep operation-specific request/response models in the same operation folder.
- Use `Contracts/` inside a feature for shared cross-operation contracts.
