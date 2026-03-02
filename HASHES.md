# K-Envoy Executable Verification

This file contains cryptographic hashes of the K-Envoy.exe file. Users can verify that the executable they downloaded matches the official release and hasn't been tampered with.

## Current Release Hashes

**File:** `K-Envoy.exe`

### SHA256 (Recommended)
```
C4A49FCC506D20CBAA167B92ABF320D49F4EA06B399520899FC22CDB4350AE8A
```

### MD5 (Legacy Support)
```
43AFD0DFCBEE5DD936F94AAD93FD2AE4
```

## How to Verify

### Windows PowerShell
```powershell
Get-FileHash "C:\path\to\K-Envoy.exe" -Algorithm SHA256
```

### Windows Command Prompt (certUtil)
```cmd
certutil -hashfile "C:\path\to\K-Envoy.exe" SHA256
```

### Linux/Mac (WSL)
```bash
sha256sum K-Envoy.exe
```

## Why Verification Matters

- ? **Authenticity**: Confirms the file came from the official source
- ? **Integrity**: Ensures the file hasn't been corrupted or modified
- ? **Security**: Protects against malware or tampered downloads

## Steps to Verify

1. Download `K-Envoy.exe` from the GitHub Releases page
2. Run one of the hash verification commands above
3. Compare the output with the **SHA256 hash** shown in this file
4. If they match exactly, the file is authentic and safe to run
5. If they don't match, **do not run the file** and report the issue

## Release Information

- **Build Date**: 2026-03-02
- **.NET Version**: 8.0
- **Architecture**: x64 (Windows only)
- **Release Type**: Self-contained executable

