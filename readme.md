# SmartWaste App

A **.NET 9** desktop application with **Avalonia UI**, **Entity Framework Core**, and **Stored Procedure** backends, following a **clean architecture** approach.

---

## Project Structure
```
App
│
├─ App.UI                // Avalonia frontend
│   ├─ Views/            // All UI pages go here
│   │   ├─ MainWindow.axaml     // Main window (UI structure only)
│   │   └─ MainWindow.axaml.cs  // Code-behind for MainWindow (UI-specific event logic)
│   └─ ViewModels/       // ViewModels for corresponding Views (Data + behavior)
│       └─ MainWindowViewModel.cs
│
├─ App.Core              // Shared interfaces and models
│   ├─ IService.cs       // Common interface for all BLLs
│   └─ Models.cs         // Data models (e.g., User)
│
├─ App.BLL.EF            // EF Core LINQ business logic
│   ├─ AppDbContext.cs   // EF Core DbContext
│   └─ EfService.cs      // Implementation of IService using LINQ
│
├─ App.BLL.SP            // Stored Procedure business logic
│   └─ SpService.cs      // Implementation of IService using SPs
│
├─ App.Factory           // Factory to choose between BLL implementations
│   └─ ServiceFactory.cs // Returns EF or SP service instance
│
└─ sql                   // Database scripts
    └─ db.sql            // Database creation and SPs
```

---

## Adding New UI Pages

1. **Create a new XAML page** in the App.UI/Views/ folder:
```
App.UI/
├─ Views/
│   └─ NewPage.axaml
│   └─ NewPage.axaml.cs
```

2. **Create a corresponding ViewModel** in App.UI/ViewModels/:
```
App.UI/
├─ ViewModels/
│   └─ NewPageViewModel.cs
```

3. **Bind the View to the ViewModel** in XAML using x:DataType:
```
<Window xmlns="https://github.com/avaloniaui"           
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:App.UI.ViewModels"
        x:Class="App.UI.Views.NewPage"
        Width="400" Height="300"
        Title="New Page">;
        

    <Design.DataContext>
        <vm:NewPageViewModel />
    </Design.DataContext>

    <>;!-- Page content here -->;

</Window>
```

## Running Instructions

### Setup
1. Add your SQLServer password to the ```_connectionString``` in ```MainWindowViewModel.cs```.
2. Run ```sql/db.sql``` to create the db.

### Build and Run
In the root folder, run:
```
dotnet clean
dotnet restore
dotnet build
dotnet run
```