# SmartWaste Management System

A waste management system with role-based dashboards for Citizens, Operators, and Government officials. Built with .NET 9, Avalonia UI, and SQL Server.

## 🏗️ Project Structure

```
SmartWaste/
├── App.Core/              # Shared models, DTOs, interfaces
├── App.BLL.EF/           # Entity Framework implementation
├── App.BLL.SP/           # Stored Procedures implementation
├── App.Factory/          # Service factory (switches between EF/SP)
├── App.UI/               # Avalonia UI (MVVM)
│   ├── ViewModels/       # View Models
│   ├── Views/            # XAML Views
│   └── Services/         # UI Services (Navigation, etc.)
└── sql/                  # Database scripts
```

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (localhost)
- Visual Studio 2022 or VS Code

### Database Setup

1. **Run database creation script:**
   ```sql
   -- In SQL Server Management Studio
   -- File: sql/db.sql
   ```

2. **Run additional stored procedures:**
   ```sql
   -- File: sql/additional-stored-procedures.sql
   ```

### Run the Application

```bash
cd App.UI
dotnet run
```

**Test Credentials:**
- Citizen: `35201-0000001-0` / `password1`
- Operator: `42000-0300001-0` / `oppass1`

## 👥 Team Guide

### Working on Features

#### 🟦 Citizen Dashboard
**files:**
- `App.UI/ViewModels/CitizenDashboardViewModel.cs`
- `App.UI/Views/CitizenDashboardWindow.axaml`
- `App.UI/Views/CitizenDashboardWindow.axaml.cs`
- `App.Core/ICitizenService.cs`
- `App.BLL.EF/EfCitizenService.cs`
- `App.BLL.SP/SpCitizenService.cs`

**Tasks:**
- View waste listings
- Create new waste listings
- View transaction history
- File complaints
- View profile

#### 🟩 Operator Dashboard
**files:**
- `App.UI/ViewModels/OperatorDashboardViewModel.cs`
- `App.UI/Views/OperatorDashboardWindow.axaml`
- `App.UI/Views/OperatorDashboardWindow.axaml.cs`
- `App.Core/IOperatorService.cs`
- `App.BLL.EF/EfOperatorService.cs`
- `App.BLL.SP/SpOperatorService.cs`

**tasks:**
- View assigned collection points
- Perform waste collection
- Update collection status
- View collection history
- View route information

#### 🟨 Government Dashboard
**files:**
- `App.UI/ViewModels/GovernmentDashboardViewModel.cs`
- `App.UI/Views/GovernmentDashboardWindow.axaml`
- `App.UI/Views/GovernmentDashboardWindow.axaml.cs`
- `App.Core/IGovernmentService.cs`
- `App.BLL.EF/EfGovernmentService.cs`
- `App.BLL.SP/SpGovernmentService.cs`

**tasks:**
- View analytics and reports
- Manage operators
- Manage categories and prices
- View warehouse inventory
- Handle complaints

### Common Files (Don't Modify Without Discussion)
- `App.Core/Models.cs` - Database models
- `App.Core/DTOs.cs` - Data transfer objects
- `App.Factory/ServiceFactory.cs` - Service creation
- `App.UI/App.axaml.cs` - Application startup
- `App.UI/Services/NavigationService.cs` - Navigation logic

## 📝 Coding Standards

### 1. Naming Conventions

**ViewModels:**
```csharp
public class CitizenDashboardViewModel : ViewModelBase
{
    private string _propertyName;

    public string PropertyName
    {
        get => _propertyName;
        set => this.RaiseAndSetIfChanged(ref _propertyName, value);
    }

    public ReactiveCommand<Unit, Unit> CommandName { get; }
}
```

**Services (Interface):**
```csharp
public interface ICitizenService
{
    Task<List<WasteListing>> GetListingsAsync(string citizenID);
    Task<bool> CreateListingAsync(CreateListingDto dto);
}
```

**Services (EF Implementation):**
```csharp
public class EfCitizenService : ICitizenService
{
    private readonly AppDbContext _db;

    public EfCitizenService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WasteListing>> GetListingsAsync(string citizenID)
    {
        return await _db.WasteListings
            .Where(w => w.CitizenID == citizenID)
            .ToListAsync();
    }
}
```

**Services (SP Implementation):**
```csharp
public class SpCitizenService : ICitizenService
{
    private readonly string _connectionString;

    public SpCitizenService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<WasteListing>> GetListingsAsync(string citizenID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetCitizenListings", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CitizenID", citizenID);

        // Execute and map results
    }
}
```

### 2. XAML Conventions

**Window Structure:**
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:App.UI.ViewModels"
        x:Class="App.UI.Views.CitizenDashboardWindow"
        Width="1200" Height="800"
        Title="SmartWaste - Citizen Dashboard"
        WindowStartupLocation="CenterScreen">

    <Design.DataContext>
        <vm:CitizenDashboardViewModel />
    </Design.DataContext>

    <DockPanel x:DataType="vm:CitizenDashboardViewModel">
        <!-- Your UI here -->
    </DockPanel>
</Window>
```

**Binding:**
- Use `x:DataType` for compiled bindings
- Bind commands: `Command="{Binding CommandName}"`
- Bind properties: `Text="{Binding PropertyName}"`

### 3. Error Handling

Always use try-catch in ViewModels:
```csharp
private async Task LoadDataAsync()
{
    try
    {
        IsBusy = true;
        ErrorMessage = null;

        // Your code here
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Error: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```


1. **Build successfully:**
   ```bash
   dotnet build
   ```

2. **Test both EF and SP modes:**
   - Toggle the radio buttons in login
   - Test your feature with both options

3. **Test error scenarios:**
   - What if API fails?
   - What if user enters invalid data?
   - What if network is slow?

4. **Check UI responsiveness:**
   - Does it look good?
   - Are loading states shown?
   - Are errors displayed properly?

## 📊 Current Status

### ✅ Completed
- [x] Database schema
- [x] Authentication (EF + SP)
- [x] Login functionality
- [x] Navigation service
- [x] Base ViewModels

### 🚧 In Progress
- [x] Citizen Dashboard
- [ ] Operator Dashboard
- [ ] Government Dashboard

### 📋 To Do
- [ ] Citizen waste listing management
- [ ] Operator collection workflow
- [ ] Government analytics
- [ ] Complaint system
- [ ] Transaction processing

## 🎨 UI Guidelines

### Colors
- Primary: `#2E7D32` (Green)
- Success: `#4CAF50`
- Error: `#EF5350`
- Warning: `#FFA726`
- Info: `#2196F3`

### Common UI Components

**Button:**
```xml
<Button Content="Submit"
        Command="{Binding SubmitCommand}"
        Height="40"
        Padding="20,0"
        Background="#2E7D32"
        Foreground="White"/>
```

**DataGrid:**
```xml
<DataGrid ItemsSource="{Binding Items}"
          IsReadOnly="True"
          AutoGenerateColumns="False"
          GridLinesVisibility="All">
    <DataGrid.Columns>
        <DataGridTextColumn Header="ID" Binding="{Binding ListingID}"/>
        <DataGridTextColumn Header="Weight" Binding="{Binding Weight}"/>
    </DataGrid.Columns>
</DataGrid>
```

**Error Display:**
```xml
<Border Background="#FFEBEE"
        BorderBrush="#EF5350"
        BorderThickness="1"
        CornerRadius="5"
        Padding="10"
        IsVisible="{Binding ErrorMessage, Converter={x:Static ObjectConverters.IsNotNull}}">
    <TextBlock Text="{Binding ErrorMessage}"
               Foreground="#D32F2F"
               TextWrapping="Wrap"/>
</Border>
```

## Common Issues

### Issue: Build fails with XAML errors
**Solution:** Make sure your `x:DataType` matches your ViewModel class name

### Issue: Binding doesn't work
**Solution:** Check:
1. Is `x:DataType` set?
2. Does property exist in ViewModel?
3. Is `RaiseAndSetIfChanged` called in setter?

### Issue: Database connection fails
**Solution:** Check connection string in `appsettings.json`

### Issue: Stored procedure not found
**Solution:** Run `sql/additional-stored-procedures.sql`



