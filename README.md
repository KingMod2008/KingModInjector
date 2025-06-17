# KingModInjector 🛠️

A modern, feature-rich DLL Injector with a sleek interface designed for Windows applications. 🎨

## 🚀 Features

- 🎨 Modern dark theme UI with clean and intuitive design
- 🎯 Process selection with application icons for easy identification
- 📁 DLL file selection with file browser
- 🔍 Process refresh functionality to update running processes
- 💬 Status messages for operation feedback
- 📱 Compact and efficient window layout
- 🔐 Secure injection using Windows API functions
- 🔄 Auto-detection of running processes
- 🎮 Compatible with most Windows applications

## 🛠️ Technical Details

### 📊 Project Structure
- `Form1.cs`: Main application window and UI logic
- `Program.cs`: Application entry point
- `Injector.csproj`: Project configuration
- `KingModInjector.sln`: Solution file

### 📦 Dependencies
- .NET 6.0 or later
- Windows operating system
- Windows Forms
- Windows API (kernel32.dll)

## 📥 Installation

1. 📥 Clone the repository:
```bash
git clone https://github.com/KingMod2008/KingModInjector.git
```

2. 📂 Open the solution in Visual Studio 2022 or later
3. 🔧 Build the solution
4. 🚀 Run the application

## 📝 Usage Guide

1. 🎯 Select a process:
   - Choose from the dropdown menu (with icons)
   - Use the "Refresh" button to update the process list
   - Browse for executable files to see related processes

2. 📁 Select a DLL:
   - Click the "Browse" button
   - Select your DLL file
   - Verify the file path in the text box

3. 🚀 Inject the DLL:
   - Click the "Inject" button
   - Check the status message for operation result

## 🛠️ Building

The project uses .NET 6.0. You can build it using:

### Visual Studio
1. Open `KingModInjector.sln`
2. Click Build > Build Solution

### .NET CLI
```bash
dotnet build
```

## 📝 Code Snippets

### Main Window Initialization
```csharp
public Form1()
{
    InitializeComponent();
    InitializeCustomUI();
}
```

### Process Injection
```csharp
private void InjectButton_Click(object sender, EventArgs e)
{
    try
    {
        var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            throw new Exception("Could not open process.");
        }

        var hKernel32 = GetModuleHandle("kernel32.dll");
        var hLoadLibrary = GetProcAddress(hKernel32, "LoadLibraryA");
        
        // Inject DLL
        var pLoadLibrary = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)256, 0x1000, 0x40);
        WriteProcessMemory(hProcess, pLoadLibrary, System.Text.Encoding.ASCII.GetBytes(dllTextBox.Text), (uint)dllTextBox.Text.Length, out _);
        CreateRemoteThread(hProcess, IntPtr.Zero, 0, hLoadLibrary, pLoadLibrary, 0, IntPtr.Zero);
        CloseHandle(hProcess);
    }
    catch (Exception ex)
    {
        statusLabel.Text = $"Error: {ex.Message}";
    }
}
```

## 🔐 Security Note

This tool is intended for educational and legitimate software development purposes only. Use responsibly and within legal boundaries.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Support

If you find this project useful, please consider:
- 🌟 Starring the repository
- 🎯 Opening issues for bugs or feature requests
- 🤝 Contributing code or documentation improvements

## 📝 Changelog

### v1.0.0 - Initial Release
- Modern UI implementation
- Process selection with icons
- DLL injection functionality
- Status message system
- Process refresh feature

## 📢 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📖 Documentation

For more detailed documentation, please refer to the code comments and the Windows API documentation.

## 🎯 Future Plans

- Add more process information display
- Implement process memory scanning
- Add more injection methods
- Improve error handling
- Add more customization options

## 📢 Credits

- Special thanks to the .NET community
- Windows API documentation
- All contributors who helped improve this project

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📞 Contact

For questions or support, please open an issue on the GitHub repository.
