# [<img src="https://raw.githubusercontent.com/EntexInteractive/Parallel/master/Parallel.Cli/parallel-red.ico" alt="Parallel Icon" width="38" height="38">](https://github.com/EntexInteractive/Parallel) Parallel

[![.NET](https://img.shields.io/github/actions/workflow/status/EntexInteractive/Parallel/dotnet-build.yml?label=Main%20build&style=for-the-badge)](https://github.com/EntexInteractive/Parallel/actions/workflows/dotnet-build.yml) [![latest version](https://img.shields.io/github/v/release/EntexInteractive/Parallel?label=Latest%20release&style=for-the-badge)](https://github.com/EntexInteractive/Parallel/releases/latest) [![GitHub Downloads](https://img.shields.io/github/downloads/EntexInteractive/Parallel/total?style=for-the-badge)](https://github.com/EntexInteractive/Parallel/releases/latest)

#### As of 8/14/2026: Development of Parallel has been succeeded by [Vertex](https://github.com/EntexInteractive/Vertex), which expands the project into a more robust file processing and synchronization system.

## What is Parallel?

Parallel is a **snapshot‑based backup and sync engine** built for people who want real control over their data. It doesn’t assume you want everything in the cloud, and it doesn’t hide what it’s doing. It builds a local‑first, verifiable history of your files across Windows, macOS, and Linux.

Under the hood, Parallel works much more like **Git across the whole filesystem** than a typical sync tool. Every file is hashed, deduped, and stored as an immutable object. Snapshots are fast and incremental, so you can keep a long history without wasting space.

Parallel started as a way to handle **multi‑terabyte datasets** that commercial services kept choking on. The goal was simple: no limits, no lock‑in, and no guessing what the software is doing with your files.

## Why Parallel?

Parallel is free, open source, and built around the idea that your storage is your business. You point it at whatever you own, whether it's an external drive, a NAS, an SSH server, or an S3‑compatible bucket like [Storj](https://www.storj.io/) or [Wasabi](https://wasabi.com/), and Parallel handles the rest.

It’s not just a sync tool. It gives you:
- Modern file compression to save on storage.
- Content‑addressed storage for automatic dedupe.
- Integrity checks on every file.
- System‑wide versioning, not just per‑folder history.
- Cross‑platform snapshot sync.
- Tools for cleaning up old data and finding duplicates.

Your computer already has enough ways to frustrate you. Your backup system shouldn’t be one of them. Parallel keeps things straightforward, predictable, and under your control, with no silent overwrites, no surprise limits, no nonsense.

## Quick Start Guide
#### 1. Install Parallel
Download the latest [release](https://github.com/EntexInteractive/Parallel/releases/latest) or build from source:
```
git clone https://github.com/EntexInteractive/Parallel
cd Parallel
dotnet build
```

On Linux systems, you can install via:
```
curl -sSL https://raw.githubusercontent.com/EntexInteractive/Parallel/main/install.sh | sudo bash
```

#### 2. Set up your vaults
Vaults are storage targets where Parallel sends and receives files. This can be an external drive, a NAS share, an SSH server, or an S3-compatible cloud.
```
parallel vaults add
```
All vaults are saved as JSON in `%AppData%\Parallel\Configuration.json` for easier importing and exporting. Learn more [here](https://github.com/EntexInteractive/Parallel/wiki/Configuration#configuration-file).
#### 3. Back up files to vaults
Parallel can back up all changed files on the system with:
```
parallel sync
```
You can also specify a path, which can be a file, folder, or a specific vault configuration.
```
parallel sync --path "C:\Windows\System32"
parallel sync -p "C:\Windows\System32\cmd.exe"
parallel sync --config 1a2b3c4d
```
#### 4. Restore files from a vault
Parallel can restore files from a vault with:
```
parallel restore --path "C:\Windows\System32"
parallel restore -p "C:\Windows\System32\cmd.exe"
```
Parallel keeps revisions of files. To restore files to a previous version and not the latest, you can use the `--before` option and provide a valid timestamp string.
```
parallel restore --path "C:\Windows\System32" --before "2025-12-16 5:11 PM"
parallel restore -p "C:\Windows\System32\cmd.exe" --before "12/16/25"
```

## Status

Parallel is in active development and still considered early-beta. The core engine file syncing and restoring is functional on Windows and Linux, but the project is evolving quickly. Expect breaking changes, new features landing often, and some rough edges.

If you’re comfortable testing early software and giving feedback, you’re exactly the kind of person Parallel is built for right now.

## Contact

For questions and ideas, reach out via our [GitHub Issues](https://github.com/EntexInteractive/Parallel/issues).
