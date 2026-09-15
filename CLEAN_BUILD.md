# Clean rebuild

This branch (`clean-release`) is a rebuild profile intended to reduce antivirus false positives caused by the original self-extracting compressed single-file publish format.

## Changes

- Uses the existing open-source CreamInstaller source tree.
- Removes `PublishSingleFile` packaging from the project profile.
- Removes the single-file compression/self-extraction settings.
- Publishes a framework-dependent Win-x64 application instead of a compressed self-contained single executable.
- Does **not** alter the application's unlocker implementation or embedded unlocker resources.

## Security note

The original project has documented generic heuristic antivirus detections. A different packaging format can reduce those detections, but it cannot prove the application is malware-free and this build is not code-signed.

## Verification

The release workflow builds directly from this branch and attaches:

- `CreamInstaller.exe`
- `CreamInstaller-Clean-v<version>-win-x64.zip`

Before running the executable, verify its SHA-256 hash and scan the exact downloaded file with your antivirus/VirusTotal. A remaining detection should be treated as a security finding to investigate, not automatically dismissed as a false positive.
