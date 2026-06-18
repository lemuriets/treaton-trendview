# Plan: YAML-config-based CAN packages

Branch: `yaml-config-packages` (from `main`).

Goal: replace the 50 hand-written `IdXxx : BasePackageParsed` classes with a single
generic interpreter driven by per-package YAML definitions, loaded at runtime from a
`config/` folder next to the executable. Output contract (`PackageData` =
`NumericData` + `Messages`, plus `TechStatus`) stays byte-identical so the indexer,
CSV export, and Trends GUI are unaffected.

## Decisions (locked)

- **Full migration** — every package interpreted from YAML, including the structural
  ones (synchro datetime, mode→context, conditional clbr_err).
- **Build copies** the protocol YAMLs into the output `config/` folder
  (`CopyToOutputDirectory`), propagating to GUI + CLI bins.
- **One active machine family** per `config/` (folder-per-machine layout; exactly one
  active at a time).
- **Folder missing → localized GUI dialog**, OK button exits the app.
- **1193 (clbr_err)** via a declarative conditional (`when`) schema.
- **1409/1410 (SPO v2)** reverse-engineered from the existing C# classes (C# is the spec).
- **Datetime + context** declarative: new `DateTime` field kind + `setsContext`/`requiresContext`.
- The 50 `IdXxx` classes are **kept in the repo** (project convention: don't delete
  unused/dead code) but are no longer registered.

## Runtime layout

```
<exe dir>/
  config/
    mv200_300_350/
      protocol.yaml          # family manifest (canIdBits, endianness, packageFiles)
      1120_id_synhro.yaml
      ...
```

- `configRoot = Path.Combine(AppContext.BaseDirectory, "config")`.
- Active family = the single subfolder under `config/`.
  - 0 subfolders / no `config/`  → `ConfigFolderMissingException` → GUI dialog (exit).
  - >1 subfolder (no selection)  → `AmbiguousConfigException` → critical-error dialog.
    (A persisted "active family" setting can be added later if wanted.)

## YAML schema (additions on top of current files)

Existing files already carry: `id`, `hex`, `name`, `module`, `length`, `description`,
`aliases`, and `fields[]` with `kind` (`Value|Bit|BitRange|Raw`), `byte`/`bytes[]`,
`bit`, `bitRange:{from,to}`, `signed`, `scale`, `unit`, `values`.

Rules and additions:

- **Message vs numeric**: a `Value`/`BitRange` field with `values:` emits a message;
  otherwise it emits a `NumericDataItem` (applying `signed`, `scale`, `unit`).
- **`values` accepts both shapes** (deserializer-normalized):
  ```yaml
  values: { 0: "ОЖИДАНИЕ" }                     # scalar shorthand => status Ok
  values: { 1: { message: "...", status: Error } }
  ```
- **`Bit`** uses `messageWhenZero` / `messageWhenOne` (mapped from current `values: {0,1}`),
  status escalation per state.
- **New `kind: DateTime`** (for synchro):
  ```yaml
  - name: Timestamp
    kind: DateTime
    bytes: [0, 1, 2, 3, 4, 5]   # year, month, day, hour, minute, second
    yearOffset: 2000
    # emits formatted Messages[0] using CanConfig.TimeFormat; invalid date => package null
  ```
- **`setsContext` / `requiresContext`** (replaces factory hardcode of `IdModeCivl.Id`):
  ```yaml
  # 1184_id_mode_civl.yaml
  setsContext: { key: civlMode, byte: 0 }
  # 1193_id_clbr_err_civl.yaml
  requiresContext: civlMode
  ```
- **Conditional (`when`)** for 1193 — equality match, omitted key = wildcard:
  ```yaml
  fields:
    - name: Response
      kind: Value
      byte: 0
      cases:                       # message table chosen by mode
        - when: { mode: 2 }
          values: { 0: "начало калибровки", 1: { message: "...", status: Ok }, ... }
        - when: { mode: 4 }
          values: { ... }
    - name: ResponseValue
      kind: Value
      byte: 1
      emitNumeric:                 # numeric item chosen by (mode, code=byte0)
        - when: { mode: 5, code: 1 } => { name: Resource }
        - when: { mode: 5, code: 6 } => { name: Voltage, scale: 0.1 }
        - when: { mode: 2 }          => { name: Progress }
        - when: { mode: 14, code: 18 } => { name: MaxError }
  ```
  (Reproduces `IdClbrErrCivl` exactly, including its existing mode→table mapping.)
- **Optional per-field `endianness`** (defaults to protocol-level `Little`).
- Fix the Cyrillic typo in `1120`'s alias `ID_SYNCHRО` → `ID_SYNCHRO`.
- `parser: config|code` key is tolerated/ignored (obsolete under full migration).

## Components

### LogDecoder.Can (protocol layer — where packages belong)

1. **`Protocol/Definitions/`** model classes (deserialization targets):
   - `ProtocolDefinition`, `PackageDefinition`, `FieldDefinition`, `WhenClause`,
     `NumericEmit`, `FieldCase`; extend `FieldKind` with `DateTime`; normalize
     `ValueMessageDefinition` (scalar + object).
2. **`Protocol/ProtocolLoader.cs`**:
   - Locate `config/`, select the single family, read `protocol.yaml`, glob `*.yaml`
     (excluding manifest), deserialize via **YamlDotNet** (new package ref).
   - Validate: unique ids, byte indices within `length`, kinds, context references.
   - Throws `ConfigFolderMissingException` / `AmbiguousConfigException` / `ConfigValidationException`.
3. **`Protocol/ConfigCanPackage.cs : BasePackageParsed`**:
   - One interpreter for all packages. `ParseData()` iterates `fields`, extracts via
     `BitUtil`, resolves `values`/`cases`/`emitNumeric` (honoring `when` vs
     `ParseContext`), handles `DateTime`, escalates `TechStatus`. Min-length guard from
     `length`.
4. **`CanPackages/Factory.cs`**:
   - Replace `RegisterBuiltIn()` with `LoadFrom(IReadOnlyList<PackageDefinition>)`:
     register id → `ConfigCanPackage`. Generalize the `CivlMode` capture to any
     `setsContext` package.

### LogDecoder.Can.csproj — deploy configs

- Copy `Protocol/Packages/<family>/**/*.yaml` (+ profile) to output `config/<family>/`
  via `<None ... CopyToOutputDirectory="PreserveNewest" Link="config/...">`; propagates
  transitively to GUI + CLI output.

### LogDecoder.GUI.Avalonia — bootstrap + dialog

- New `Services/ProtocolBootstrap` (GUI-services-first): runs the loader, returns the
  factory or surfaces the failure.
- `App.OnFrameworkInitializationCompleted`: load **before** `new MainWindow()`.
  - `ConfigFolderMissingException` → localized modal (new resx keys ru/en) telling the
    user to add `config/` next to the exe and restart; **OK → `Environment.Exit`**.
  - success → build factory from definitions, inject into `MainWindow`.
- `MainWindow` ctor takes the prebuilt factory (it already feeds `IndexingService`,
  `PackageCatalog`).

### LogDecoder.CLI

- Load via the same loader; missing/invalid → log + nonzero exit (no dialog).

## Tests (LogDecoder.Can.Tests)

- **Parity tests** (primary safety net): run representative byte payloads through both
  the legacy `IdXxx` class and `ConfigCanPackage`; assert identical `NumericData`,
  `Messages`, `TechStatus`. Coverage:
  - `IdOxy` (single byte), `IdMMvCivl` (multibyte), `IdStatusCivl` (status bits + values),
    `IdPar1Civl` (bit + bitRange + multibyte), `IdWaveCivl` (signed + scale),
    `IdSynchro` (datetime, incl. invalid-date → null), `IdModeCivl`→`IdClbrErrCivl`
    (context + conditional, multiple modes/codes), `IdStatusSpo_v2_1/2`.
- **Loader tests**: missing folder throws; valid family loads expected id set;
  duplicate id rejected; byte-out-of-range rejected.
- **DateTime kind**: boundary/invalid inputs.

## Sequencing

1. Add YamlDotNet; build out `Protocol/Definitions/` model + `FieldKind.DateTime`.
2. `ProtocolLoader` + exceptions + validation.
3. `ConfigCanPackage` interpreter.
4. Author missing/new YAML: 1409/1410 (from C#), `setsContext`/`requiresContext`,
   `cases`/`emitNumeric` for 1193, `DateTime` for 1120; fix alias typo.
5. Factory swap (`LoadFrom`), generalize context capture.
6. Parity + loader tests; iterate until green.
7. csproj build-copy to `config/`.
8. GUI `ProtocolBootstrap` + missing-folder dialog + resx; wire `App`/`MainWindow`.
9. CLI loader wiring.
10. Full build + test pass.

## Risks / notes

- Indexing depends on synchro emitting `Messages[0]` = timestamp in `CanConfig.TimeFormat`
  — covered by a dedicated parity test.
- Reverse-engineered 1409/1410 reproduce current C# behavior (incl. quirks); if a real
  device spec exists, reconcile against it.
- `Profiles/mv300.yaml` is currently empty — left as-is (potential future active-family marker).
- Windows-only deployment: `AppContext.BaseDirectory` is cross-platform, no OS branching needed.
