using App.Core;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace App.UI.ViewModels;

public class OperatorDashboardViewModel : ViewModelBase
{
    private readonly IOperatorService _service;
    private readonly string _operatorID;

    // Operator Details
    private Operator? _operatorDetails;
    public Operator? OperatorDetails
    {
        get => _operatorDetails;
        set => this.RaiseAndSetIfChanged(ref _operatorDetails, value);
    }

    // Collection Points
    public ObservableCollection<OperatorCollectionPointView> CollectionPoints { get; } = new();

    // Collection History
    public ObservableCollection<Collection> CollectionHistory { get; } = new();

    // Performance Stats
    private OperatorPerformanceView? _performance;
    public OperatorPerformanceView? Performance
    {
        get => _performance;
        set => this.RaiseAndSetIfChanged(ref _performance, value);
    }

    // Collection Form
    private int _selectedListingID;
    private decimal _collectedWeight;
    private string? _photoProof;

    public int SelectedListingID
    {
        get => _selectedListingID;
        set => this.RaiseAndSetIfChanged(ref _selectedListingID, value);
    }

    public decimal CollectedWeight
    {
        get => _collectedWeight;
        set => this.RaiseAndSetIfChanged(ref _collectedWeight, value);
    }

    public string? PhotoProof
    {
        get => _photoProof;
        set => this.RaiseAndSetIfChanged(ref _photoProof, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> LoadDetailsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCollectionPointsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCollectionHistoryCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPerformanceCommand { get; }
    public ReactiveCommand<Unit, Unit> PerformCollectionCommand { get; }
    public ReactiveCommand<int, Unit> DepositWasteCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshAllCommand { get; }

    public OperatorDashboardViewModel(IOperatorService service, string operatorID)
    {
        _service = service;
        _operatorID = operatorID;

        // Initialize commands
        LoadDetailsCommand = ReactiveCommand.CreateFromTask(LoadDetailsAsync);
        LoadCollectionPointsCommand = ReactiveCommand.CreateFromTask(LoadCollectionPointsAsync);
        LoadCollectionHistoryCommand = ReactiveCommand.CreateFromTask(LoadCollectionHistoryAsync);
        LoadPerformanceCommand = ReactiveCommand.CreateFromTask(LoadPerformanceAsync);
        PerformCollectionCommand = ReactiveCommand.CreateFromTask(PerformCollectionAsync);
        DepositWasteCommand = ReactiveCommand.CreateFromTask<int>(DepositWasteAsync);
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

    private async Task LoadDetailsAsync()
    {
        try
        {
            OperatorDetails = await _service.GetOperatorDetailsAsync(_operatorID);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load operator details: {ex.Message}";
        }
    }

    private async Task LoadCollectionPointsAsync()
    {
        try
        {
            var points = await _service.GetMyCollectionPointsAsync(_operatorID);
            CollectionPoints.Clear();
            foreach (var point in points)
                CollectionPoints.Add(point);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load collection points: {ex.Message}";
        }
    }

    private async Task LoadCollectionHistoryAsync()
    {
        try
        {
            var history = await _service.GetMyCollectionHistoryAsync(_operatorID);
            CollectionHistory.Clear();
            foreach (var collection in history.OrderByDescending(c => c.CollectedDate))
                CollectionHistory.Add(collection);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load collection history: {ex.Message}";
        }
    }

    private async Task LoadPerformanceAsync()
    {
        try
        {
            Performance = await _service.GetMyPerformanceAsync(_operatorID);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load performance data: {ex.Message}";
        }
    }

    private async Task PerformCollectionAsync()
    {
        try
        {
            if (SelectedListingID <= 0)
            {
                ErrorMessage = "Please select a listing to collect";
                return;
            }

            if (CollectedWeight <= 0)
            {
                ErrorMessage = "Please enter a valid collected weight";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            if (OperatorDetails?.WarehouseID == null)
            {
                ErrorMessage = "No warehouse assigned";
                return;
            }

            var collectionDto = new CollectionDto
            {
                OperatorID = _operatorID,
                ListingID = SelectedListingID,
                CollectedWeight = CollectedWeight,
                WarehouseID = OperatorDetails.WarehouseID.Value
            };

            var result = await _service.CollectWasteAsync(collectionDto);

            if (result.Success && result.CollectionID > 0)
            {
                // Reset form
                SelectedListingID = 0;
                CollectedWeight = 0;
                PhotoProof = null;

                // Refresh data
                await LoadCollectionPointsAsync();
                await LoadCollectionHistoryAsync();
                await LoadPerformanceAsync();

                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Collection failed";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error performing collection: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DepositWasteAsync(int collectionID)
    {
        try
        {
            IsBusy = true;

            if (OperatorDetails?.WarehouseID == null)
            {
                ErrorMessage = "No warehouse assigned to operator";
                return;
            }

            // Find the collection to get category and weight
            var collection = CollectionHistory.FirstOrDefault(c => c.CollectionID == collectionID);
            if (collection == null)
            {
                ErrorMessage = "Collection not found";
                return;
            }

            // Note: We need CategoryID but Collection doesn't have it directly
            // This might need to be retrieved from the listing
            // For now, we'll skip the actual deposit implementation
            // The proper implementation would require joining with WasteListing

            ErrorMessage = "Deposit functionality not yet implemented - needs CategoryID from listing";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error depositing waste: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAllDataAsync()
    {
        await LoadDetailsAsync();
        await LoadCollectionPointsAsync();
        await LoadCollectionHistoryAsync();
        await LoadPerformanceAsync();
    }
}
