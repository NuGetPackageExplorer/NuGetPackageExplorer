# ViewModelBase Delayed Initialization

## Overview

The `ViewModelBase` class now supports **delayed initialization** with queue-based INPC (INotifyPropertyChanged) event replay. This feature allows view models to queue property change notifications during initialization and replay them in order once initialization is complete.

## Use Cases

This feature is particularly useful when:

1. **Async Initialization**: The view model needs to perform asynchronous initialization (e.g., loading data from a service) but the UI may already be bound to it.

2. **Complex Initialization**: Multiple properties need to be set during initialization, and you want to ensure all property change notifications are delivered in the correct order after initialization completes.

3. **Performance**: You want to batch property change notifications to avoid triggering UI updates during initialization, then replay them once initialization is complete.

4. **Ordered Events**: You need to guarantee that property change events are processed in the exact order they occurred, even if some events trigger additional property changes.

## API

### Methods

#### `BeginDelayedInitialization()`
Begins delayed initialization mode. After calling this method, all property change events will be queued instead of being raised immediately.

**Throws:**
- `InvalidOperationException` if delayed initialization has already been started.

#### `EndDelayedInitialization()`
Ends delayed initialization mode and replays all queued property change events in order. Events that occur during replay are automatically added to the queue to maintain proper ordering. Once the queue is drained, normal immediate event processing resumes.

**Throws:**
- `InvalidOperationException` if delayed initialization has not been started or has already been completed.
- `InvalidOperationException` if the event queue is not initialized.

### Properties

#### `IsDelayedInitializationActive` (protected)
Gets a value indicating whether the view model is currently in delayed initialization mode.

Returns `true` if between `BeginDelayedInitialization()` and `EndDelayedInitialization()` calls, otherwise `false`.

## Usage Examples

### Example 1: Basic Async Initialization

```csharp
public class DataViewModel : ViewModelBase
{
    private string? _status;
    private List<Item>? _items;
    
    public string? Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }
    
    public List<Item>? Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DataViewModel()
    {
        // Start delayed initialization
        BeginDelayedInitialization();
        
        // Set initial state - events are queued
        Status = "Loading...";
        Items = null;
        
        // Initialize asynchronously
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            // Load data from service
            var data = await LoadDataFromServiceAsync();
            
            // Update properties - still queued
            Items = data;
            Status = "Loaded";
        }
        finally
        {
            // Replay all queued events
            EndDelayedInitialization();
        }
    }
}
```

### Example 2: Constructor Initialization

```csharp
public class ConfigViewModel : ViewModelBase
{
    private string? _title;
    private string? _description;
    private int _timeout;
    
    public string? Title
    {
        get => _title;
        set { if (_title != value) { _title = value; OnPropertyChanged(); } }
    }
    
    public string? Description
    {
        get => _description;
        set { if (_description != value) { _description = value; OnPropertyChanged(); } }
    }
    
    public int Timeout
    {
        get => _timeout;
        set { if (_timeout != value) { _timeout = value; OnPropertyChanged(); } }
    }
    
    public ConfigViewModel(Config config)
    {
        // Queue events during initialization
        BeginDelayedInitialization();
        
        // Load configuration
        Title = config.Title;
        Description = config.Description;
        Timeout = config.Timeout;
        
        // Additional computed properties
        if (string.IsNullOrEmpty(Description))
        {
            Description = $"Configuration for {Title}";
        }
        
        // Replay all events in order
        EndDelayedInitialization();
    }
}
```

### Example 3: Events During Replay

```csharp
public class SmartViewModel : ViewModelBase
{
    private string? _firstName;
    private string? _lastName;
    private string? _fullName;
    
    public string? FirstName
    {
        get => _firstName;
        set
        {
            if (_firstName != value)
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string? LastName
    {
        get => _lastName;
        set
        {
            if (_lastName != value)
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string? FullName
    {
        get => _fullName;
        private set
        {
            if (_fullName != value)
            {
                _fullName = value;
                OnPropertyChanged();
            }
        }
    }
    
    public SmartViewModel()
    {
        PropertyChanged += OnPropertyChangedHandler;
        
        BeginDelayedInitialization();
        
        FirstName = "John";
        LastName = "Doe";
        
        EndDelayedInitialization();
        // Events replay in order: FirstName, LastName, FullName (computed during replay)
    }
    
    private void OnPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        // This will be called during replay
        if (e.PropertyName == nameof(FirstName) || e.PropertyName == nameof(LastName))
        {
            // This property change is queued during replay
            FullName = $"{FirstName} {LastName}";
        }
    }
}
```

## Best Practices

1. **Always use try-finally**: When using delayed initialization with async operations, always use try-finally to ensure `EndDelayedInitialization()` is called even if an exception occurs.

   ```csharp
   BeginDelayedInitialization();
   try
   {
       // Initialization code
   }
   finally
   {
       EndDelayedInitialization();
   }
   ```

2. **Don't nest**: Don't call `BeginDelayedInitialization()` if you're already in delayed initialization mode. Check `IsDelayedInitializationActive` if needed.

3. **Consider thread safety**: The delayed initialization mechanism itself is not thread-safe. If you're setting properties from multiple threads, ensure proper synchronization.

4. **Event handler side effects**: Be aware that event handlers that trigger additional property changes during replay will have those changes queued and processed in order.

5. **Performance considerations**: Use delayed initialization when you have multiple properties to set during initialization. For single property changes or very simple view models, the overhead may not be worth it.

## Implementation Details

### How It Works

1. **Queue Creation**: When `BeginDelayedInitialization()` is called, a queue is created and the view model enters "delayed initialization mode".

2. **Event Queueing**: In this mode, calls to `OnPropertyChanged()` queue `PropertyChangedEventArgs` objects instead of raising events immediately.

3. **Event Replay**: When `EndDelayedInitialization()` is called:
   - The view model exits delayed initialization mode
   - It enters "replay mode"
   - Events are dequeued and raised one by one
   - Any events triggered during replay are added to the queue
   - Once the queue is empty, replay mode ends
   - The queue is disposed (set to null)

4. **Normal Processing**: After replay completes, `OnPropertyChanged()` returns to normal behavior - events are raised immediately.

### Thread Safety

The current implementation is not thread-safe. If you need to set properties from multiple threads during initialization, you must provide your own synchronization.

### Performance

- **Memory**: Uses a `Queue<PropertyChangedEventArgs>` which has minimal overhead
- **Time**: Event replay is O(n) where n is the number of queued events
- **GC**: The queue is nulled after replay to allow garbage collection

## Backward Compatibility

This feature is **fully backward compatible**:

- By default, view models are in "initialized" state (not delayed)
- Existing code that doesn't use delayed initialization works exactly as before
- `OnPropertyChanged()` has the same behavior when not in delayed initialization mode

## Testing

The implementation includes comprehensive unit tests covering:

1. Normal behavior (no delayed initialization)
2. Delayed initialization with event queuing and replay
3. Multiple property changes during initialization
4. Events triggered during replay are properly queued
5. Normal processing resumes after replay completes
6. Invalid operations throw appropriate exceptions

All tests pass successfully, validating the correctness of the implementation.
