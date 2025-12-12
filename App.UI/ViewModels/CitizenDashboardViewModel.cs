using App.Core;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using App.Factory;
using App.UI.Services;


namespace App.UI.ViewModels;

public class CitizenDashboardViewModel : ViewModelBase
{
    private ICitizenService _service;
    private readonly string _citizenID;
    private readonly string _connectionString;
    private bool _useEntityFramework;

    // Backend toggle
    public bool UseEntityFramework
    {
        get => _useEntityFramework;
        set
        {
            this.RaiseAndSetIfChanged(ref _useEntityFramework, value);
            _ = SwitchBackendAsync();
        }
    }

    public string CurrentBackend => UseEntityFramework ? "Entity Framework (LINQ)" : "Stored Procedures (SQL)";

    // Selected category backing field
    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            if (value != null)
            {
                SelectedCategoryID = value.CategoryID;
            }
        }
    }

    // Profile
    private CitizenProfileView? _profile;
    public CitizenProfileView? Profile
    {
        get => _profile;
        set => this.RaiseAndSetIfChanged(ref _profile, value);
    }

    // Collections
    public ObservableCollection<ListingDto> MyListings { get; } = new();
    public ObservableCollection<TransactionRecord> MyTransactions { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Area> Areas { get; } = new();

    // Form properties
    private int _selectedCategoryID;
    private decimal _weight = 0;
    private decimal _estimatedPrice = 0;

    public int SelectedCategoryID
    {
        get => _selectedCategoryID;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategoryID, value);
            _ = CalculatePriceAsync();
        }
    }

    public decimal Weight
    {
        get => _weight;
        set
        {
            this.RaiseAndSetIfChanged(ref _weight, value);
            _ = CalculatePriceAsync();
        }
    }

    public decimal EstimatedPrice
    {
        get => _estimatedPrice;
        set => this.RaiseAndSetIfChanged(ref _estimatedPrice, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> LoadProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadListingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadTransactionsCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateListingCommand { get; }
    public ReactiveCommand<int, Unit> CancelListingCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshAllCommand { get; }

    public CitizenDashboardViewModel(ICitizenService service, string citizenID, string connectionString, bool useEf)
    {
        _service = service;
        _citizenID = citizenID;
        _connectionString = connectionString;
        _useEntityFramework = useEf;

        // Clear log file for fresh debugging session
        DebugLogger.ClearLog();
        DebugLogger.Log($"=== CitizenDashboardViewModel CREATED ===");
        DebugLogger.Log($"CitizenID: {citizenID}");
        DebugLogger.Log($"Backend: {(useEf ? "Entity Framework" : "Stored Procedures")}");
        DebugLogger.Log($"Log file location: {DebugLogger.GetLogFilePath()}");

        // Initialize commands
        LoadProfileCommand = ReactiveCommand.CreateFromTask(LoadProfileAsync);
        LoadListingsCommand = ReactiveCommand.CreateFromTask(LoadListingsAsync);
        LoadTransactionsCommand = ReactiveCommand.CreateFromTask(LoadTransactionsAsync);
        CreateListingCommand = ReactiveCommand.CreateFromTask(CreateListingAsync);
        CancelListingCommand = ReactiveCommand.CreateFromTask<int>(CancelListingAsync);
        RefreshAllCommand = ReactiveCommand.CreateFromTask(RefreshAllDataAsync);

        // Load initial data
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessages();

            // Load categories and areas
            var categories = await _service.GetActiveCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
                Categories.Add(category);

            var areas = await _service.GetAreasAsync();
            Areas.Clear();
            foreach (var area in areas)
                Areas.Add(area);

            // Load profile and data
            await RefreshAllDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to initialize dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            Profile = await _service.GetMyProfileAsync(_citizenID);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load profile: {ex.Message}";
        }
    }

    private async Task LoadListingsAsync()
    {
        DebugLogger.LogSeparator();
        DebugLogger.Log("LoadListingsAsync START (UI ViewModel)");
        DebugLogger.Log($"CitizenID: {_citizenID}");
        DebugLogger.Log($"Current thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        DebugLogger.Log($"MyListings.Count before clear: {MyListings.Count}");

        try
        {
            DebugLogger.Log("Calling _service.GetMyListingsAsync...");

            var listings = await _service.GetMyListingsAsync(_citizenID);

            DebugLogger.Log($"Received {listings.Count} listings from service");
            DebugLogger.Log($"Thread after await: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            if (listings == null)
            {
                DebugLogger.Log("WARNING: listings is NULL!");
                return;
            }

            // CRITICAL FIX: Ensure UI updates happen on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DebugLogger.Log("Now on UI thread. Clearing MyListings ObservableCollection...");
                DebugLogger.Log($"UI thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                MyListings.Clear();
                DebugLogger.Log($"MyListings cleared. Count: {MyListings.Count}");

                if (listings.Count == 0)
                {
                    DebugLogger.Log("No listings to add. Setting success message.");
                    SuccessMessage = "No listings found. Create your first listing in the 'Sell Waste' tab!";
                }
                else
                {
                    DebugLogger.Log($"Adding {listings.Count} listings to MyListings ObservableCollection...");

                    int index = 0;
                    foreach (var listing in listings.OrderByDescending(l => l.CreatedAt))
                    {
                        index++;
                        DebugLogger.Log($"Adding listing #{index}: ID={listing.ListingID}, Category={listing.CategoryName}");

                        MyListings.Add(listing);
                    }

                    DebugLogger.Log($"All listings added. Final MyListings.Count: {MyListings.Count}");
                    ClearMessages();
                }
            });

            DebugLogger.Log("LoadListingsAsync COMPLETED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR in LoadListingsAsync: {ex.Message}");
            DebugLogger.Log($"Exception type: {ex.GetType().Name}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = $"Failed to load listings: {ex.Message}";
            });
        }
    }

    private async Task LoadTransactionsAsync()
    {
        try
        {
            var transactions = await _service.GetMyTransactionsAsync(_citizenID);
            MyTransactions.Clear();
            foreach (var transaction in transactions.OrderByDescending(t => t.TransactionDate))
                MyTransactions.Add(transaction);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load transactions: {ex.Message}";
        }
    }

    private async Task CreateListingAsync()
    {
        DebugLogger.LogSeparator();
        DebugLogger.Log("CreateListingAsync START");
        DebugLogger.Log($"CitizenID: {_citizenID}");
        DebugLogger.Log($"SelectedCategoryID: {SelectedCategoryID}");
        DebugLogger.Log($"Weight: {Weight}");

        try
        {
            ClearMessages();

            // Validation
            if (SelectedCategoryID <= 0)
            {
                DebugLogger.Log("Validation failed: No category selected");
                ErrorMessage = "Please select a waste category";
                return;
            }

            if (Weight <= 0 || Weight < 0.1m)
            {
                DebugLogger.Log("Validation failed: Invalid weight");
                ErrorMessage = "Please enter a valid weight (minimum 0.1 kg)";
                return;
            }

            if (Weight > 1000)
            {
                DebugLogger.Log("Validation failed: Weight too large");
                ErrorMessage = "Weight cannot exceed 1000 kg. Please contact support for larger quantities.";
                return;
            }

            DebugLogger.Log("Validation passed");
            IsBusy = true;

            var selectedCategoryName = SelectedCategory?.CategoryName ?? "Unknown";
            var estimatedAmount = EstimatedPrice;

            var listingDto = new CreateListingDto
            {
                CitizenID = _citizenID,
                CategoryID = SelectedCategoryID,
                Weight = Weight
            };

            DebugLogger.Log($"Calling _service.CreateWasteListingAsync with DTO:");
            DebugLogger.Log($"  CitizenID: {listingDto.CitizenID}");
            DebugLogger.Log($"  CategoryID: {listingDto.CategoryID}");
            DebugLogger.Log($"  Weight: {listingDto.Weight}");

            var listingID = await _service.CreateWasteListingAsync(listingDto);

            DebugLogger.Log($"CreateWasteListingAsync returned ListingID: {listingID}");

            if (listingID > 0)
            {
                DebugLogger.Log("Listing created successfully!");

                // Show success message
                SuccessMessage = $"✅ Listing created successfully! Your {selectedCategoryName} waste ({Weight:N2} kg) worth Rs. {estimatedAmount:N2} has been listed. An operator will collect it soon.";

                // Reset form
                SelectedCategory = null;
                SelectedCategoryID = 0;
                Weight = 0;
                EstimatedPrice = 0;

                DebugLogger.Log("Form reset. Now refreshing listings...");

                // Refresh listings
                await LoadListingsAsync();

                DebugLogger.Log("CreateListingAsync COMPLETED SUCCESSFULLY");
            }
            else
            {
                DebugLogger.Log("ERROR: ListingID was 0 or negative");
                ErrorMessage = "Failed to create listing. Please try again.";
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR in CreateListingAsync: {ex.Message}");
            DebugLogger.Log($"Exception type: {ex.GetType().Name}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");

            ErrorMessage = $"Error creating listing: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelListingAsync(int listingID)
    {
        try
        {
            ClearMessages();
            IsBusy = true;

            var success = await _service.CancelListingAsync(listingID, _citizenID);

            if (success)
            {
                SuccessMessage = "✅ Listing cancelled successfully!";
                await LoadListingsAsync();
            }
            else
            {
                ErrorMessage = "Failed to cancel listing. Only pending listings can be cancelled.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cancelling listing: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CalculatePriceAsync()
    {
        try
        {
            if (SelectedCategoryID > 0 && Weight > 0)
            {
                var price = await _service.CalculatePriceAsync(SelectedCategoryID, Weight);
                EstimatedPrice = price;
            }
            else
            {
                EstimatedPrice = 0;
            }
        }
        catch
        {
            EstimatedPrice = 0;
        }
    }

    private async Task RefreshAllDataAsync()
    {
        await LoadProfileAsync();
        await LoadListingsAsync();
        await LoadTransactionsAsync();
    }

    private async Task SwitchBackendAsync()
    {
        try
        {
            DebugLogger.LogSeparator();
            DebugLogger.Log($"Switching backend to: {(UseEntityFramework ? "Entity Framework" : "Stored Procedures")}");

            IsBusy = true;
            SuccessMessage = $"Switching to {CurrentBackend}...";

            // Create new service with the selected backend
            _service = ServiceFactory.CreateCitizenService(UseEntityFramework, _connectionString);

            DebugLogger.Log("Service recreated. Now refreshing all data...");

            // Refresh all data with new service
            await RefreshAllDataAsync();

            SuccessMessage = $"Successfully switched to {CurrentBackend}!";
            DebugLogger.Log("Backend switch completed successfully");

            // Clear success message after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                if (SuccessMessage?.Contains("switched") == true)
                {
                    Dispatcher.UIThread.InvokeAsync(() => ClearMessages());
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR switching backend: {ex.Message}");
            ErrorMessage = $"Failed to switch backend: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
