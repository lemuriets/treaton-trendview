# TrendView_IVL

## Purpose
TrendView_IVL is responsible for decoding and parsing binary respirator logs.  
The parser extracts timestamped device data from cyclic binary storage files and provides efficient access to records by datetime range.

---

# Binary Log Storage Format

## General Rules
- Logs are stored in files named `00`–`07`
- All files are located in the same directory
- Total file count may be less than 8
- Each file size is fixed: `2 GB`
- Files are logically ordered and must always be processed sequentially:

```text
00 -> 01 -> 02 -> ... -> 07
```

- Log data is continuous across files
- When all files become full, recording starts overwriting old data beginning from file `00` (ring buffer behavior)

---

## Internal File Structure
Each log file is divided into fixed-size buffers:

```text
buffer_size = 2^15 bytes (32768 bytes)
```

Notes:
- Buffers may be partially filled
- Last buffer in file is not guaranteed to be complete
- Parsing logic must correctly handle partially written buffers

---

# Index File Format (v2)

## Structure

### Header Line
The first line contains:
- index format version
- source binary file name

Example:

```text
FormatVersion 2; SourceFile 00
```

---

### Data Lines
Each following line contains:

```text
(buffer_number, offset_inside_buffer, date, time)
```

Example:

```text
15 3135 18.11.2025 02:11:47
```

Meaning:
- `buffer_number` — physical buffer index inside binary storage
- `offset_inside_buffer` — byte offset inside buffer
- `date/time` — timestamp associated with synchronized packet

---

# Parsing Pipeline

## 1. Index Generation (Optional)
If index file does not exist:
- parser generates index using `IdSynchro` packages
- generated index is saved as `.txt`

Purpose:
- avoid full binary scan on every startup
- accelerate datetime lookup

---

## 2. Index Loading
Parser loads index file into memory and:
- validates format
- converts entries into internal structures
- splits entries into logical recording sessions

A session represents a continuous chronological data segment.

---

## 3. Range Query Parsing
Parser retrieves binary packets using:
- datetime range
- index offsets
- session boundaries

Goal:
- efficiently extract only required log fragments without full-file scan

---

# Known Problems / Edge Cases

## Index Problems

### 1. Ring Buffer Overwrite
Because storage is cyclic:
- chronological order inside physical files may break
- newest data may appear before older data
- parser must reconstruct logical timeline correctly

### 2. Incomplete Buffers
Last written buffer may contain:
- partially written packets
- corrupted/incomplete records

Parser must safely skip invalid data.

### 3. Session Splitting
Large time gaps or overwrite boundaries may create:
- discontinuous sessions
- timestamp rollback situations

Session detection logic must account for this.

### 4. Missing Files
Some files from `00`–`07` may be absent.

Parser must:
- handle missing files gracefully
- preserve sequential ordering of existing files

---

# Expected Parser Requirements

- High performance on large datasets
- Minimal memory allocations
- Fast datetime range lookup
- Safe handling of corrupted/incomplete binary data
- Correct reconstruction of cyclic log timeline
- Support for incremental index rebuilding