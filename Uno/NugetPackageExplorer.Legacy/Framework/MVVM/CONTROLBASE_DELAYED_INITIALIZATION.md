# ControlBase Delayed Initialization

## Overview

The `ControlBase` class provides a base for custom Uno platform controls with **delayed initialization** support for dependency properties. This feature allows controls to queue `SetValue()` operations during initialization and execute them in order once initialization is complete.

## Key Concept

Unlike `ViewModelBase` which queues property change *events*, `ControlBase` queues the actual `SetValue()` *operations* themselves. This is critical because:

1. Dependency properties use `SetValue()` which immediately triggers callbacks registered in `PropertyMetadata`
2. You cannot override `SetValue()` (it's sealed on `DependencyObject`)
3. The solution is to queue the `SetValue()` calls themselves, not their callbacks

## Use Cases

This feature is particularly useful when:

1. **Complex Control Initialization**: A control needs to set multiple dependency properties during initialization, and you want to ensure all properties are set before any callbacks execute.

2. **Async Data Loading**: The control needs to load data asynchronously and set multiple properties, batching all the SetValue operations until data is loaded.

3. **Interdependent Properties**: Multiple dependency properties have interdependencies (e.g., Min/Max/Value), and you want to set all of them before validation callbacks run.

4. **Performance**: You want to batch property setting to avoid triggering expensive operations (like layout or rendering) multiple times during initialization.

## API

### Methods

#### `BeginDelayedInitialization()`
Begins delayed initialization mode. After calling this method, `SetValueDelayed()` calls will be queued instead of executing immediately.

**Throws:**
- `InvalidOperationException` if delayed initialization has already been started.

#### `EndDelayedInitialization()`
Ends delayed initialization mode and executes all queued `SetValue()` operations in order. SetValue operations that trigger during execution are added to the queue to maintain proper ordering. Once the queue is drained, operations execute directly.

**Throws:**
- `InvalidOperationException` if delayed initialization has not been started or has already been completed.
- `InvalidOperationException` if the SetValue queue is not initialized.

#### `SetValueDelayed(DependencyProperty dp, object? value)`
Sets the value of a dependency property with support for delayed initialization. Use this instead of `SetValue()` directly to enable queueing during initialization.

**Parameters:**
- `dp`: The dependency property to set.
- `value`: The new value.

**Throws:**
- `ArgumentNullException` if dp is null.

#### `SetValueDelayed(DependencyProperty dp, Func<object?> valueFactory)`
Sets the value of a dependency property with support for delayed initialization. The value factory is invoked when the SetValue operation is actually executed.

**Parameters:**
- `dp`: The dependency property to set.
- `valueFactory`: A function that returns the value to set.

**Throws:**
- `ArgumentNullException` if dp or valueFactory is null.

### Properties

#### `IsDelayedInitializationActive` (protected)
Gets a value indicating whether the control is currently in delayed initialization mode.

Returns `true` if between `BeginDelayedInitialization()` and `EndDelayedInitialization()` calls, otherwise `false`.

## Usage Examples

### Example 1: Basic Usage

```csharp
public class DataGridControl : ControlBase
{
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
        // This callback fires when SetValue is executed
        control.RefreshItems();
    }
    
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
        control.ApplySorting();
    }
    
    public DataGridControl()
    {
        // Begin delayed initialization
        BeginDelayedInitialization();
        
        // Queue SetValue operations - callbacks DON'T fire yet
        SetValueDelayed(ItemsSourceProperty, GetDefaultItems());
        SetValueDelayed(SortColumnProperty, "Name");
        
        // Execute all SetValue operations (and their callbacks) in order
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
        control.RenderChart(); // Expensive operation
    }
    
    public ChartControl()
    {
        BeginDelayedInitialization();
        
        // Initial value queued
        SetValueDelayed(DataProperty, null);
        
        _ = LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            // Load data from service
            var data = await FetchDataFromServiceAsync();
            
            // Update property - still queued
            SetValueDelayed(DataProperty, data);
        }
        finally
        {
            // Execute all SetValue operations - RenderChart() called only once
            EndDelayedInitialization();
        }
    }
}
```

### Example 3: Interdependent Properties with Value Factory

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
        control.ValidateRange();
    }
    
    public RangeControl()
    {
        BeginDelayedInitialization();
        
        // Set all three properties
        SetValueDelayed(MinimumProperty, 0);
        SetValueDelayed(MaximumProperty, 100);
        
        // Value factory allows access to Min/Max when SetValue actually executes
        SetValueDelayed(ValueProperty, () => 
        {
            var min = (double)GetValue(MinimumProperty);
            var max = (double)GetValue(MaximumProperty);
            return (min + max) / 2; // Default to middle
        });
        
        // Now execute all SetValue operations - validation runs once with all values set
        EndDelayedInitialization();
    }
    
    private void ValidateRange()
    {
        // All three properties are set before this validation runs
        if (Value < Minimum) SetValue(ValueProperty, Minimum);
        if (Value > Maximum) SetValue(ValueProperty, Maximum);
    }
}
```

## Important Notes

### Direct SetValue vs SetValueDelayed

**During delayed initialization:**
- Use `SetValueDelayed()` to queue operations
- Regular `SetValue()` still works but executes immediately (bypassing the queue)
- Property setters (e.g., `Title = "value"`) use `SetValue()` internally and execute immediately

**After delayed initialization:**
- Both `SetValueDelayed()` and `SetValue()` execute immediately
- No difference in behavior

### Pattern for Using ControlBase

1. **Inherit from ControlBase** instead of Control
2. **Define dependency properties normally** (no changes needed)
3. **In constructor**, call `BeginDelayedInitialization()`
4. **Use SetValueDelayed()** to set property values
5. **Call EndDelayedInitialization()** to execute all queued SetValue operations

### Why Not Use Property Setters?

You might wonder: "Why not just do `Title = "value"` during delayed init?"

The problem is that property setters call `SetValue()` directly, which cannot be intercepted. You must explicitly use `SetValueDelayed()` to queue the operation.

**Consider providing alternate initialization methods:**

```csharp
public class MyControl : ControlBase
{
    public void Initialize(string title, string subtitle)
    {
        BeginDelayedInitialization();
        SetValueDelayed(TitleProperty, title);
        SetValueDelayed(SubtitleProperty, subtitle);
        EndDelayedInitialization();
    }
}
```

## Best Practices

1. **Always use try-finally**: When using with async operations:

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

2. **Use value factories for computed values**: When a value depends on other properties that are also being set:

   ```csharp
   SetValueDelayed(DerivedProperty, () => ComputeFromOtherProperties());
   ```

3. **Don't nest**: Don't call `BeginDelayedInitialization()` if already in delayed mode.

4. **Document your API**: If your control uses delayed initialization, document which methods trigger it.

5. **Consider separate initialization method**: Instead of doing it in the constructor, provide a separate `Initialize()` method.

## Comparison with ViewModelBase

| Aspect | ViewModelBase | ControlBase |
|--------|--------------|-------------|
| Base Type | INotifyPropertyChanged | Control (DependencyObject) |
| What's Queued | PropertyChanged events | SetValue() operations |
| When Callbacks Fire | During replay | During SetValue execution |
| API | OnPropertyChanged() | SetValueDelayed() |
| Use Case | MVVM view models | Custom controls |
| Property Access | Direct field access | GetValue/SetValue |
| Platform | WPF + Uno | Uno only |

## Performance

- **Memory**: Uses a `Queue<Action>` with minimal overhead
- **Time**: SetValue execution is O(n) where n is the number of queued operations
- **GC**: The queue is nulled after replay to allow garbage collection

## Limitations

1. **Not thread-safe**: Designed for single-threaded UI scenarios
2. **Uno only**: Uses Uno/WinUI types, not compatible with WPF
3. **Must use SetValueDelayed()**: Property setters bypass the queue
4. **No automatic detection**: You must explicitly opt-in by using SetValueDelayed()

## Testing

When testing controls that use ControlBase:

1. Test normal behavior (without delayed initialization)
2. Test delayed initialization with single property
3. Test multiple properties during initialization
4. Test value factory evaluation timing
5. Test that normal processing resumes after completion
6. Test error conditions (double begin, end without begin)

## Related Documentation

- See `/PackageViewModel/DELAYED_INITIALIZATION.md` for ViewModelBase delayed initialization
- See Uno platform documentation for dependency properties
- See WinUI documentation for custom controls
