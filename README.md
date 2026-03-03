# K-Envoy

A Windows desktop application for managing system configurations and your game library.

<p align="center">
  <img width="400" src="https://github.com/user-attachments/assets/546ccce4-01f2-4056-bf14-b96420533692" />
  <img width="400" src="https://github.com/user-attachments/assets/8067cdaa-5b4c-458b-b7b5-d6406d781bee" />
</p>

## What is K-Envoy?

K-Envoy helps you:
- **Manage Games** - Keep track of your games with custom names, paths, and notes
- **Add Game Covers** - Display game cover images in your library (recommended for better visuals instead of exe icons)
- **Configure System Settings** - Modify critical Windows configurations with an easy-to-use interface
- **Monitor Status** - View real-time system configuration status

## System Requirements

- **OS:** Windows 10/11
- **Architecture:** x64
- **Admin Rights:** Required for system configuration changes

## Installation & Usage

### Option 1: Download Pre-built Executable (Easiest)

1. Go to **Releases** and download `K-Envoy.exe`
2. *(Optional)* Verify the file hash - see [HASHES.md](HASHES.md) for SHA256 verification
3. Run the executable - no installation needed!
4. Administrator permissions will be requested on first run

### Option 2: Build from Source

#### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Visual Studio 2022 or VS Code

#### Build Steps
1. Clone this repository
   ```
   git clone https://github.com/iruel403/K-Envoy.git
   cd K-Envoy
   ```

2. Build the solution
   ```
   dotnet build
   ```

3. Run the application
   ```
   dotnet run
   ```

4. Or publish as a standalone executable
   ```
   dotnet publish -c Release -p:PublishProfile=FolderProfile
   ```
   The executable will be in `bin/Release/publish/K-Envoy.exe`

## Verifying Downloads

For security, you can verify that your downloaded executable matches the official release by comparing file hashes.

See [HASHES.md](HASHES.md) for detailed verification instructions and current release hashes.

## Tips

- **Game Icons**: For better visual appearance, use image covers (PNG, JPG) instead of extracting icons from exe files
- **Configuration**: All settings are saved to `%LocalAppData%\KEnvoy\config.json`
- **Transparency**: All source code is available here - build it yourself to ensure authenticity!

## License

MIT License

