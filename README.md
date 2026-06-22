# Smart File Organizer

A .NET 8 WPF desktop app that organizes files in a selected folder using extension-based rules.

## What it does

1. **Select folder** — choose the folder to scan.
2. **Define rules** — map file extensions to destination subfolders (e.g. `.pdf` → `Documents`).
3. **Preview** — see where each file will be moved before applying changes.
4. **Organize** — move matching files into their destination folders.
5. **History** — log each move; rules and history are saved as JSON.

Built with **WPF**, **MVVM**, and **.NET 8**.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows

## Run

```powershell
cd SmartFileOrganizer
dotnet run
```

Or open `SmartFileOrganizer/SmartFileOrganizer.csproj` in Visual Studio and press **F5**.
