using App.Core;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace App.UI.ViewModels;

public class GovernmentDashboardViewModel : ViewModelBase
{
    private readonly IGovernmentService _service;

    // Warehouse Inventory
    public ObservableCollection<WarehouseInventoryView> WarehouseInventory { get; } = new();

    // High Yield Areas
    public ObservableCollection<HighYieldAreaReport> HighYieldAreas { get; } = new();

    // Operator Performance
    public ObservableCollection<OperatorPerformanceReport> OperatorPerformance { get; } = new();

    // Categories
    public ObservableCollection<Category> Categories { get; } = new();

    // Operators
    public ObservableCollection<Operator> Operators { get; } = new();

    // Routes and Areas
    public ObservableCollection<Route> Routes { get; } = new();
    public ObservableCollection<Area> Areas { get; } = new();

    // Warehouses
    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    // Category Management Form
    private string _newCategoryName = string.Empty;
    private decimal _newBasePricePerKg;
    private string? _newCategoryDescription;
    private int _selectedCategoryID;

    public string NewCategoryName
    {
        get => _newCategoryName;
        set => this.RaiseAndSetIfChanged(ref _newCategoryName, value);
    }

    public decimal NewBasePricePerKg
    {
        get => _newBasePricePerKg;
        set => this.RaiseAndSetIfChanged(ref _newBasePricePerKg, value);
    }

    public string? NewCategoryDescription
    {
        get => _newCategoryDescription;
        set => this.RaiseAndSetIfChanged(ref _newCategoryDescription, value);
    }

    public int SelectedCategoryID
    {
        get => _selectedCategoryID;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryID, value);
    }

    // Operator Management Form
    private string _newOperatorName = string.Empty;
    private string _newOperatorPhone = string.Empty;
    private string _newOperatorCNIC = string.Empty;
    private int? _selectedRouteID;
    private int? _selectedWarehouseID;
    private string? _selectedOperatorID;

    public string NewOperatorName
    {
        get => _newOperatorName;
        set => this.RaiseAndSetIfChanged(ref _newOperatorName, value);
    }

    public string NewOperatorPhone
    {
        get => _newOperatorPhone;
        set => this.RaiseAndSetIfChanged(ref _newOperatorPhone, value);
    }

    public string NewOperatorCNIC
    {
        get => _newOperatorCNIC;
        set => this.RaiseAndSetIfChanged(ref _newOperatorCNIC, value);
    }

    public int? SelectedRouteID
    {
        get => _selectedRouteID;
        set => this.RaiseAndSetIfChanged(ref _selectedRouteID, value);
    }

    public int? SelectedWarehouseID
    {
        get => _selectedWarehouseID;
        set => this.RaiseAndSetIfChanged(ref _selectedWarehouseID, value);
    }

    public string? SelectedOperatorID
    {
        get => _selectedOperatorID;
        set => this.RaiseAndSetIfChanged(ref _selectedOperatorID, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> LoadWarehouseInventoryCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadHighYieldAreasCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadOperatorPerformanceCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCategoriesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadOperatorsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadRoutesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadAreasCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadWarehousesCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateCategoryCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateCategoryPriceCommand { get; }
    public ReactiveCommand<int, Unit> DeleteCategoryCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateOperatorCommand { get; }
    public ReactiveCommand<string, Unit> DeactivateOperatorCommand { get; }
    public ReactiveCommand<Unit, Unit> AssignOperatorToRouteCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshAllCommand { get; }

    public GovernmentDashboardViewModel(IGovernmentService service)
    {
        _service = service;

        // Initialize commands
        LoadWarehouseInventoryCommand = ReactiveCommand.CreateFromTask(LoadWarehouseInventoryAsync);
        LoadHighYieldAreasCommand = ReactiveCommand.CreateFromTask(LoadHighYieldAreasAsync);
        LoadOperatorPerformanceCommand = ReactiveCommand.CreateFromTask(LoadOperatorPerformanceAsync);
        LoadCategoriesCommand = ReactiveCommand.CreateFromTask(LoadCategoriesAsync);
        LoadOperatorsCommand = ReactiveCommand.CreateFromTask(LoadOperatorsAsync);
        LoadRoutesCommand = ReactiveCommand.CreateFromTask(LoadRoutesAsync);
        LoadAreasCommand = ReactiveCommand.CreateFromTask(LoadAreasAsync);
        LoadWarehousesCommand = ReactiveCommand.CreateFromTask(LoadWarehousesAsync);
        CreateCategoryCommand = ReactiveCommand.CreateFromTask(CreateCategoryAsync);
        UpdateCategoryPriceCommand = ReactiveCommand.CreateFromTask(UpdateCategoryPriceAsync);
        DeleteCategoryCommand = ReactiveCommand.CreateFromTask<int>(DeleteCategoryAsync);
        CreateOperatorCommand = ReactiveCommand.CreateFromTask(CreateOperatorAsync);
        DeactivateOperatorCommand = ReactiveCommand.CreateFromTask<string>(DeactivateOperatorAsync);
        AssignOperatorToRouteCommand = ReactiveCommand.CreateFromTask(AssignOperatorToRouteAsync);
        RefreshAllCommand = ReactiveCommand.CreateFromTask(RefreshAllDataAsync);

        // Load initial data
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            await RefreshAllDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Initialization error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadWarehouseInventoryAsync()
    {
        try
        {
            var inventory = await _service.GetWarehouseInventoryAsync();
            WarehouseInventory.Clear();
            foreach (var item in inventory)
                WarehouseInventory.Add(item);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load warehouse inventory: {ex.Message}";
        }
    }

    private async Task LoadHighYieldAreasAsync()
    {
        try
        {
            var areas = await _service.AnalyzeHighYieldAreasAsync();
            HighYieldAreas.Clear();
            foreach (var area in areas)
                HighYieldAreas.Add(area);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load high yield areas: {ex.Message}";
        }
    }

    private async Task LoadOperatorPerformanceAsync()
    {
        try
        {
            var performance = await _service.GetOperatorPerformanceReportAsync();
            OperatorPerformance.Clear();
            foreach (var perf in performance.OrderByDescending(p => p.TotalCollections))
                OperatorPerformance.Add(perf);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load operator performance: {ex.Message}";
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _service.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
                Categories.Add(category);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load categories: {ex.Message}";
        }
    }

    private async Task LoadOperatorsAsync()
    {
        try
        {
            var operators = await _service.GetAllOperatorsAsync();
            Operators.Clear();
            foreach (var op in operators)
                Operators.Add(op);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load operators: {ex.Message}";
        }
    }

    private async Task LoadRoutesAsync()
    {
        try
        {
            var routes = await _service.GetAllRoutesAsync();
            Routes.Clear();
            foreach (var route in routes)
                Routes.Add(route);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load routes: {ex.Message}";
        }
    }

    private async Task LoadAreasAsync()
    {
        try
        {
            var areas = await _service.GetAllAreasAsync();
            Areas.Clear();
            foreach (var area in areas)
                Areas.Add(area);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load areas: {ex.Message}";
        }
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var warehouses = await _service.GetAllWarehousesAsync();
            Warehouses.Clear();
            foreach (var warehouse in warehouses)
                Warehouses.Add(warehouse);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load warehouses: {ex.Message}";
        }
    }

    private async Task CreateCategoryAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                ErrorMessage = "Category name is required";
                return;
            }

            if (NewBasePricePerKg <= 0)
            {
                ErrorMessage = "Base price must be greater than 0";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            var dto = new CreateCategoryDto
            {
                CategoryName = NewCategoryName,
                BasePricePerKg = NewBasePricePerKg,
                Description = NewCategoryDescription
            };

            var categoryID = await _service.CreateCategoryAsync(dto);

            if (categoryID > 0)
            {
                // Reset form
                NewCategoryName = string.Empty;
                NewBasePricePerKg = 0;
                NewCategoryDescription = null;

                await LoadCategoriesAsync();
            }
            else
            {
                ErrorMessage = "Failed to create category";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating category: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateCategoryPriceAsync()
    {
        try
        {
            if (SelectedCategoryID <= 0)
            {
                ErrorMessage = "Please select a category";
                return;
            }

            if (NewBasePricePerKg <= 0)
            {
                ErrorMessage = "Base price must be greater than 0";
                return;
            }

            IsBusy = true;
            var success = await _service.UpdateCategoryPriceAsync(SelectedCategoryID, NewBasePricePerKg);

            if (success)
            {
                await LoadCategoriesAsync();
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Failed to update price";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating price: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteCategoryAsync(int categoryID)
    {
        try
        {
            IsBusy = true;
            var success = await _service.DeleteCategoryAsync(categoryID);

            if (success)
            {
                await LoadCategoriesAsync();
            }
            else
            {
                ErrorMessage = "Failed to delete category";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting category: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateOperatorAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewOperatorName))
            {
                ErrorMessage = "Operator name is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewOperatorCNIC))
            {
                ErrorMessage = "CNIC is required";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            var dto = new CreateOperatorDto
            {
                CNIC = NewOperatorCNIC,
                FullName = NewOperatorName,
                PhoneNumber = NewOperatorPhone
            };

            var operatorID = await _service.CreateOperatorAsync(dto);

            if (!string.IsNullOrEmpty(operatorID))
            {
                // Reset form
                NewOperatorName = string.Empty;
                NewOperatorPhone = string.Empty;
                NewOperatorCNIC = string.Empty;

                await LoadOperatorsAsync();
            }
            else
            {
                ErrorMessage = "Failed to create operator";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating operator: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeactivateOperatorAsync(string operatorID)
    {
        try
        {
            IsBusy = true;
            var success = await _service.DeactivateOperatorAsync(operatorID);

            if (success)
            {
                await LoadOperatorsAsync();
            }
            else
            {
                ErrorMessage = "Failed to deactivate operator";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deactivating operator: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AssignOperatorToRouteAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedOperatorID))
            {
                ErrorMessage = "Please select an operator";
                return;
            }

            if (!SelectedRouteID.HasValue || !SelectedWarehouseID.HasValue)
            {
                ErrorMessage = "Please select both route and warehouse";
                return;
            }

            IsBusy = true;
            var success = await _service.AssignOperatorToRouteAsync(
                SelectedOperatorID,
                SelectedRouteID.Value,
                SelectedWarehouseID.Value);

            if (success)
            {
                await LoadOperatorsAsync();
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Failed to assign operator";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error assigning operator: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAllDataAsync()
    {
        await LoadWarehouseInventoryAsync();
        await LoadHighYieldAreasAsync();
        await LoadOperatorPerformanceAsync();
        await LoadCategoriesAsync();
        await LoadOperatorsAsync();
        await LoadRoutesAsync();
        await LoadAreasAsync();
        await LoadWarehousesAsync();
    }
}
