# ControlBase Delayed Initialization

## Overview

The `ControlBase` class provides a base for custom Uno platform controls with **delayed initialization** support and queue-based dependency property change callback replay. This feature allows controls to queue dependency property change callbacks during initialization and replay them in order once initialization is complete.

## Use Cases

This feature is particularly useful when:

1. **Complex Control Initialization**: A control needs to set multiple dependency properties during initialization, and you want to ensure all property change callbacks are executed in the correct order after initialization completes.

2. **Async Data Loading**: The control needs to load data asynchronously while dependency properties are being set, and you want to batch the property change handling.

3. **Interdependent Properties**: Multiple dependency properties have interdependencies, and you want to ensure they're all set before any callbacks execute.

4. **Performance**: You want to batch dependency property change callbacks to avoid triggering expensive operations (like layout or rendering) multiple times during initialization.

## API

### Methods

#### `BeginDelayedInitialization()`
Begins delayed initialization mode. After calling this method, all dependency property change callbacks (that use `HandlePropertyChanged`) will be queued instead of being invoked immediately.

**Throws:**
- `InvalidOperationException` if delayed initialization has already been started.

#### `EndDelayedInitialization()`
Ends delayed initialization mode and replays all queued property change callbacks in order. Callbacks that trigger during replay are automatically added to the queue to maintain proper ordering. Once the queue is drained, normal immediate callback processing resumes.

**Throws:**
- `InvalidOperationException` if delayed initialization has not been started or has already been completed.
- `InvalidOperationException` if the property change queue is not initialized.

#### `HandlePropertyChanged(Action callback)`
Handles dependency property changes with support for delayed initialization. Call this method from your property changed callbacks instead of executing the logic directly.

**Parameters:**
- `callback`: The callback action to invoke for the property change.

**Throws:**
- `ArgumentNullException` if callback is null.

### Properties

#### `IsDelayedInitializationActive` (protected)
Gets a value indicating whether the control is currently in delayed initialization mode.

Returns `true` if between `BeginDelayedInitialization()` and `EndDelayedInitialization()` calls, otherwise `false`.

## Usage Examples

### Example 1: Basic Usage with Dependency Properties

```csharp
public class DataGridControl : ControlBase
{
    #region ItemsSource DependencyProperty
    
    public static readonly DependencyProperty ItemsSourceProperty = 
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(DataGridControl),
            new PropertyMetadata(null, OnItemsSourceChanged));
    
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DataGridControl)d;
        control.HandlePropertyChanged(() => control.OnItemsSourceChangedImpl(e.OldValue, e.NewValue));
    }
    
    private void OnItemsSourceChangedImpl(object? oldValue, object? newValue)
    {
        // This will be queued during delayed initialization
        RefreshItems();
    }
    
    #endregion
    
    #region SortColumn DependencyProperty
    
    public static readonly DependencyProperty SortColumnProperty = 
        DependencyProperty.Register(
            nameof(SortColumn),
            typeof(string),
            typeof(DataGridControl),
            new PropertyMetadata(null, OnSortColumnChanged));
    
    public string? SortColumn
    {
        get => (string?)GetValue(SortColumnProperty);
        set => SetValue(SortColumnProperty, value);
    }
    
    private static void OnSortColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DataGridControl)d;
        control.HandlePropertyChanged(() => control.OnSortColumnChangedImpl());
    }
    
    private void OnSortColumnChangedImpl()
    {
        // This will be queued during delayed initialization
        ApplySorting();
    }
    
    #endregion
    
    public DataGridControl()
    {
        // Begin delayed initialization
        BeginDelayedInitialization();
        
        // Set multiple properties - callbacks are queued
        ItemsSource = GetDefaultItems();
        SortColumn = "Name";
        
        // End delayed initialization - all callbacks execute in order
        EndDelayedInitialization();
    }
}
```

### Example 2: Async Initialization

```csharp
public class ChartControl : ControlBase
{
    public static readonly DependencyProperty DataProperty = 
        DependencyProperty.Register(
            nameof(Data),
            typeof(IEnumerable<DataPoint>),
            typeof(ChartControl),
            new PropertyMetadata(null, OnDataChanged));
    
    public IEnumerable<DataPoint>? Data
    {
        get => (IEnumerable<DataPoint>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
    
    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ChartControl)d;
        control.HandlePropertyChanged(() => control.RenderChart());
    }
    
    public ChartControl()
    {
        BeginDelayedInitialization();
        
        Data = null; // Initial state
        
        _ = LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            // Load data from service
            var data = await FetchDataFromServiceAsync();
            
            // Update property - still queued
            Data = data;
            
            // Additional initialization...
            await Task.Delay(100);
        }
        finally
        {
            // Replay all queued callbacks
            EndDelayedInitialization();
        }
    }
    
    private void RenderChart()
    {
        // Expensive rendering operation
        // Only called once after all properties are set
    }
}
```

### Example 3: Interdependent Properties

```csharp
public class RangeControl : ControlBase
{
    public static readonly DependencyProperty MinimumProperty = 
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(RangeControl),
            new PropertyMetadata(0.0, OnRangeChanged));
    
    public static readonly DependencyProperty MaximumProperty = 
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(RangeControl),
            new PropertyMetadata(100.0, OnRangeChanged));
    
    public static readonly DependencyProperty ValueProperty = 
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(RangeControl),
            new PropertyMetadata(0.0, OnRangeChanged));
    
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RangeControl)d;
        control.HandlePropertyChanged(() => control.ValidateRange());
    }
    
    public RangeControl()
    {
        BeginDelayedInitialization();
        
        // Set all three properties
        Minimum = 0;
        Maximum = 100;
        Value = 50;
        
        // Now validate once with all values set
        EndDelayedInitialization();
    }
    
    private void ValidateRange()
    {
        // Ensure Value is within Min/Max
        if (Value < Minimum) Value = Minimum;
        if (Value > Maximum) Value = Maximum;
    }
}
```

## Integration Pattern

The typical pattern for integrating ControlBase with dependency properties is:

1. **Define the dependency property** with a static callback
2. **In the static callback**, call `HandlePropertyChanged` on the instance
3. **Pass a lambda** that invokes your actual property change logic

```csharp
// 1. Define property with callback
public static readonly DependencyProperty MyProperty = 
    DependencyProperty.Register(
        nameof(MyProperty),
        typeof(string),
        typeof(MyControl),
        new PropertyMetadata(null, OnMyPropertyChanged));  // ← Static callback

// 2. Static callback uses HandlePropertyChanged
private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (MyControl)d;
    control.HandlePropertyChanged(() => control.OnMyPropertyChangedImpl(e));  // ← Queue or invoke
}

// 3. Actual implementation
private void OnMyPropertyChangedImpl(DependencyPropertyChangedEventArgs e)
{
    // Your property change logic here
}
```

## Best Practices

1. **Always use try-finally**: When using delayed initialization with async operations, always use try-finally to ensure `EndDelayedInitialization()` is called.

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

2. **Don't nest**: Don't call `BeginDelayedInitialization()` if already in delayed initialization mode. Check `IsDelayedInitializationActive` if needed.

3. **Use for initialization only**: This feature is designed for control initialization. Don't use it for runtime property changes unless you have a specific reason.

4. **Be consistent**: Either use `HandlePropertyChanged` for all your property callbacks or none. Mixing approaches can be confusing.

5. **Consider performance**: Use delayed initialization when you have multiple properties that trigger expensive operations (layout, rendering, data processing).

## Implementation Details

### How It Works

1. **Queue Creation**: `BeginDelayedInitialization()` creates an action queue and enters delayed mode
2. **Callback Queueing**: Property change callbacks are queued instead of being invoked
3. **Callback Replay**: `EndDelayedInitialization()` replays all callbacks in FIFO order
4. **Replay Protection**: Callbacks during replay are queued to maintain order
5. **Normal Resumption**: After queue drains, callbacks execute immediately

### Thread Safety

The current implementation is not thread-safe. If you need to set properties from multiple threads during initialization, you must provide your own synchronization. However, this is typically not an issue for UI controls which operate on the UI thread.

### Performance

- **Memory**: Uses a `Queue<Action>` with minimal overhead
- **Time**: Callback replay is O(n) where n is the number of queued callbacks
- **GC**: The queue is nulled after replay to allow garbage collection

## Comparison with ViewModelBase

| Feature | ViewModelBase | ControlBase |
|---------|--------------|-------------|
| Base Type | INotifyPropertyChanged | Control |
| Mechanism | PropertyChanged events | Dependency property callbacks |
| Queue Type | PropertyChangedEventArgs | Action delegates |
| Use Case | MVVM view models | Custom controls |
| Platform | WPF + Uno | Uno only |

## Backward Compatibility

This feature is **fully backward compatible**:

- Controls that don't inherit from ControlBase are unaffected
- By default, controls are in "initialized" state (not delayed)
- Existing controls that inherit from Control continue to work
- `HandlePropertyChanged` can be adopted incrementally

## Example: Converting Existing Control

**Before:**
```csharp
public class MyControl : Control
{
    public static readonly DependencyProperty TitleProperty = 
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(MyControl),
            new PropertyMetadata(null, OnTitleChanged));
    
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MyControl)d).UpdateTitle();  // Executes immediately
    }
    
    private void UpdateTitle()
    {
        // Update logic
    }
}
```

**After:**
```csharp
public class MyControl : ControlBase  // ← Inherit from ControlBase
{
    public static readonly DependencyProperty TitleProperty = 
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(MyControl),
            new PropertyMetadata(null, OnTitleChanged));
    
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MyControl)d;
        control.HandlePropertyChanged(() => control.UpdateTitle());  // ← Use HandlePropertyChanged
    }
    
    private void UpdateTitle()
    {
        // Update logic (unchanged)
    }
    
    public MyControl()
    {
        // Optionally use delayed initialization
        BeginDelayedInitialization();
        // ... set properties ...
        EndDelayedInitialization();
    }
}
```

## Testing

When testing controls that use ControlBase:

1. Test normal behavior (without delayed initialization)
2. Test delayed initialization with single property
3. Test multiple properties during initialization
4. Test callbacks triggered during replay
5. Test that normal processing resumes after replay
6. Test error conditions (double begin, end without begin)

## Limitations

1. **Not thread-safe**: Requires external synchronization for multi-threaded use
2. **Uno only**: Uses Uno/WinUI types, not compatible with WPF
3. **Requires HandlePropertyChanged**: Only callbacks wrapped in `HandlePropertyChanged` are queued
4. **Memory overhead**: Queue holds delegates during initialization

## Related Documentation

- See `/PackageViewModel/DELAYED_INITIALIZATION.md` for ViewModelBase delayed initialization
- See Uno platform documentation for dependency properties
- See WinUI documentation for custom controls
