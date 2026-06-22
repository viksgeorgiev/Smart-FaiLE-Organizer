# Smart File Organizer

## Project Overview

Smart File Organizer is a Windows desktop application built with .NET 8 and WPF. It helps users organize files by scanning a selected folder, matching files against extension-based rules, previewing the planned moves, and then moving matching files into their configured destination folders.

The project is designed as a simple, maintainable MVVM application suitable for learning, demonstration, and extension. It includes JSON persistence for rules and history, service-based file operations, and unit tests for the main application logic.

## Features

- Select a folder to scan using a desktop folder picker.
- Create custom organization rules based on file extensions.
- Preview files before moving them.
- Automatically create destination folders when needed.
- Move matching files into rule-based subfolders.
- Track organization history and log file operations.
- Persist rules and history data as JSON.
- Clear organization history from the application.
- Unit-tested services and ViewModel command behavior.

## Technologies Used

- C#
- .NET 8
- WPF
- MVVM
- CommunityToolkit.Mvvm
- Windows Forms folder picker integration
- System.Text.Json
- xUnit
- Moq

## Architecture

The application follows the MVVM pattern and keeps responsibilities separated across models, services, ViewModels, and XAML views.

- `Models` define application data such as file items, organization rules, preview items, history entries, log entries, and operation results.
- `Services` handle scanning folders, organizing files, saving and loading JSON data, managing rules, logging activity, and displaying rule input dialogs.
- `ViewModels` expose bindable state and commands for the WPF interface.
- `Views` are implemented with XAML and bind to the ViewModel for user interaction.
- `SmartFileOrganizer.Tests` contains unit tests for scanner, organizer, JSON persistence, and ViewModel command behavior.

The service layer uses small interfaces to keep the ViewModel testable without requiring real UI dialogs or file operations during command tests.

## Installation

### Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022, Visual Studio Code, or another editor that supports .NET projects

### Clone the Repository

```powershell
git clone https://github.com/viksgeorgiev/Smart-FaiLE-Organizer
cd "Smart FaiLE Organizer"
```

### Restore Dependencies

```powershell
dotnet restore .\SmartFileOrganizer\SmartFileOrganizer.csproj
dotnet restore .\SmartFileOrganizer.Tests\SmartFileOrganizer.Tests.csproj
```

### Run the Application

```powershell
dotnet run --project .\SmartFileOrganizer\SmartFileOrganizer.csproj
```

### Run Tests

```powershell
dotnet test .\SmartFileOrganizer.Tests\SmartFileOrganizer.Tests.csproj
```

## Usage

1. Launch the application.
2. Click **Select Folder** and choose the folder you want to organize.
3. Add rules that map file extensions to destination folders, such as `.pdf` to `Documents`.
4. Click **Scan Folder** to preview matching files.
5. Review the preview list to confirm where files will be moved.
6. Click **Organize Files** to move files into their destination folders.
7. Review the history section to see completed organization actions.

Rules and history are saved locally, so they remain available the next time the application starts.


## Future Improvements

- Add support for advanced rule conditions such as file size, date created, or file name patterns.
- Add a dry-run export report before moving files.
- Add undo support for recent organization actions.
- Improve duplicate file handling with rename options.
- Add configurable default rule templates.
- Add more UI customization and theme options.
- Add continuous integration for automated test runs.

## GitHub Repository Link Placeholder

Repository: https://github.com/viksgeorgiev/Smart-FaiLE-Organizer
