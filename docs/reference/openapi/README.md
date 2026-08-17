# Aegis OpenAPI contract baseline

`aegis-v1.json` is the reviewed machine-readable baseline for the native Aegis HTTP API. It is generated from the executable ASP.NET Core application graph, not maintained by hand.

## Reproduce the contract

From the repository root:

```powershell
./eng/export-openapi.ps1 -OutputPath docs/reference/openapi/aegis-v1.json
./eng/verify-openapi.ps1
./eng/verify-typescript-client.ps1
```

The first command intentionally updates the baseline and therefore belongs in a reviewed contract change. The second generates a candidate and a JSON diff report under ignored `artifacts/`; removed paths, operations, or schemas fail verification. The current detector is deliberately conservative but does not yet classify every breaking schema mutation, so lifecycle coverage remains a B1 requirement.

The third command restores the repository-pinned Kiota tool, generates a TypeScript client into ignored build artifacts, installs exact proof dependencies, and compiles the result in strict mode. Generated sources are not frontend source code and are not committed. This keeps contract feasibility independently reproducible while the legacy frontend remains frozen.

## Review policy

Any baseline change must explain whether it is additive, deprecated, or breaking under ADR 0006. Reviewers inspect the JSON diff report, generated-client compilation, authorization and tenant-isolation tests, and updated integration documentation. A new baseline alone is not evidence that a breaking change is safe.
