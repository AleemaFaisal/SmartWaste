using App.Core;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;


namespace App.UI.ViewModels;

public class CitizenDashboardViewModel : ViewModelBase
{
    private readonly ICitizenService _service;
    private readonly string _citizenID;

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
    public ObservableCollection<WasteListing> MyListings { get; } = new();
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

    public CitizenDashboardViewModel(ICitizenService service, string citizenID)
    {
        _service = service;
        _citizenID = citizenID;

        System.Diagnostics.Debug.WriteLine($"CitizenDashboardViewModel created for CitizenID: {citizenID}");

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
        try
        {
            System.Diagnostics.Debug.WriteLine($"LoadListingsAsync called for CitizenID: {_citizenID}");

            var listings = await _service.GetMyListingsAsync(_citizenID);

            System.Diagnostics.Debug.WriteLine($"Received {listings.Count} listings from service");

            MyListings.Clear();

            foreach (var listing in listings.OrderByDescending(l => l.CreatedAt))
            {
                System.Diagnostics.Debug.WriteLine($"Adding listing: ID={listing.ListingID}, Category={listing.CategoryName}, Weight={listing.Weight}");
                MyListings.Add(listing);
            }

            System.Diagnostics.Debug.WriteLine($"MyListings.Count after adding: {MyListings.Count}");

            if (listings.Count == 0)
            {
                SuccessMessage = "No listings found. Create your first listing in the 'Sell Waste' tab!";
            }
            else
            {
                ClearMessages();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load listings: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading listings: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
        try
        {
            ClearMessages();

            // Validation
            if (SelectedCategoryID <= 0)
            {
                ErrorMessage = "Please select a waste category";
                return;
            }

            if (Weight <= 0 || Weight < 0.1m)
            {
                ErrorMessage = "Please enter a valid weight (minimum 0.1 kg)";
                return;
            }

            if (Weight > 1000)
            {
                ErrorMessage = "Weight cannot exceed 1000 kg. Please contact support for larger quantities.";
                return;
            }

            IsBusy = true;

            var selectedCategoryName = SelectedCategory?.CategoryName ?? "Unknown";
            var estimatedAmount = EstimatedPrice;

            var listingDto = new CreateListingDto
            {
                CitizenID = _citizenID,
                CategoryID = SelectedCategoryID,
                Weight = Weight
            };

            var listingID = await _service.CreateWasteListingAsync(listingDto);

            if (listingID > 0)
            {
                // Show success message
                SuccessMessage = $"✅ Listing created successfully! Your {selectedCategoryName} waste ({Weight:N2} kg) worth Rs. {estimatedAmount:N2} has been listed. An operator will collect it soon.";

                // Reset form
                SelectedCategory = null;
                SelectedCategoryID = 0;
                Weight = 0;
                EstimatedPrice = 0;

                // Refresh listings
                await LoadListingsAsync();
            }
            else
            {
                ErrorMessage = "Failed to create listing. Please try again.";
            }
        }
        catch (Exception ex)
        {
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
}
