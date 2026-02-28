# WPF Patterns & Best Practices

## MVVM Pattern

Use CommunityToolkit.Mvvm source generators for ViewModels:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ModifierKeyDisplay))]
private Key _modifierKey = Key.Apps;

[RelayCommand]
private void RemoveApplication(ApplicationShortcutViewModel application) { }
```

---
q
## UserControl Development

### Critical Rules

1. ❌ **NEVER set `DataContext` in UserControl constructor**
   - Breaks parent DataContext inheritance
   - Breaks external bindings like `Key="{Binding Key}"`
   
2. ✅ **ALWAYS use Dependency Properties for:**
   - Properties exposed to parent (external API)
   - Internal state that participates in binding

3. ✅ **ALWAYS use type aliases:**
   ```csharp
   using UserControl = System.Windows.Controls.UserControl;
   ```

### Internal Control References

**Simple controls (static content):** Use direct name references
```csharp
<ui:Button x:Name="DefaultButton" />
DefaultButton.Appearance = ControlAppearance.Primary;
```

**Complex controls (dynamic content):** Use Dependency Properties + ElementName binding
```csharp
<UserControl x:Name="Root">
    <ui:Button Content="{Binding ButtonContent, ElementName=Root}" />
</UserControl>
```

---

## Dependency Properties

### Basic DP Pattern

```csharp
public static readonly DependencyProperty SelectedModifierProperty =
    DependencyProperty.Register(nameof(SelectedModifier), typeof(Key),
        typeof(ModifierKeySelector),
        new PropertyMetadata(Key.None, OnSelectedModifierChanged));

public Key SelectedModifier
{
    get => (Key)GetValue(SelectedModifierProperty);
    set => SetValue(SelectedModifierProperty, value);
}

private static void OnSelectedModifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is ModifierKeySelector control)
        control.UpdateButtonStates();
}
```

### Two-Way Binding

**Option 1:** Use `FrameworkPropertyMetadata` with `BindsTwoWayByDefault`
```csharp
new FrameworkPropertyMetadata(defaultValue,
    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
    OnPropertyChanged)
```

**Option 2:** Use `SetCurrentValue()` to propagate changes
```csharp
// ❌ WRONG - Won't propagate through binding
Key = e.Key;

// ✅ CORRECT - Propagates to parent ViewModel
SetCurrentValue(KeyProperty, e.Key);
```

### Common Pitfalls

**Pitfall 1: DP getters/setters are bypassed**
- WPF uses internal `GetValue`/`SetValue` directly
- Use PropertyChanged callbacks to react to changes

**Pitfall 2: INotifyPropertyChanged in UserControls**
- ❌ Causes timing issues - PropertyChanged fires before bindings subscribe
- ✅ Use Dependency Properties instead

**Pitfall 3: DataContext override** (see Critical Rules above)

---

## WPF-UI Library

### Control Appearances

```csharp
using Wpf.Ui.Controls;

button.Appearance = isSelected 
    ? ControlAppearance.Primary    // Accent color (blue)
    : ControlAppearance.Secondary; // Gray/neutral
```

Common: `Primary`, `Secondary`, `Success`, `Danger`

### Type Aliases

```csharp
using Wpf.Ui.Controls;
using UserControl = System.Windows.Controls.UserControl;
using Button = Wpf.Ui.Controls.Button;
```

---

## Reference Examples

### Simple Selector: ModifierKeySelector
- Static button set with direct name references
- Single DP for selected value
- **File:** `UI/Controls/ModifierKeySelector.xaml.cs`

### Two-Way Selector: CycleModeSelector
- Uses `FrameworkPropertyMetadata` with `BindsTwoWayByDefault`
- Direct assignment propagates to parent
- **File:** `UI/Controls/CycleModeSelector.xaml.cs`

### Complex Interactive: KeyAssignmentButton
- Multiple DPs for internal state (ButtonContent, ButtonAppearance, IsListening)
- ElementName binding with `x:Name="Root"`
- Uses `SetCurrentValue()` for two-way propagation
- Handles keyboard focus and dynamic display
- **File:** `UI/Controls/KeyAssignmentButton.xaml.cs`

---

## Quick Decision Tree

1. **Expose property to parent?** → Dependency Property
2. **Static vs dynamic content?** → Direct names vs DPs + ElementName
3. **Two-way binding?** → `BindsTwoWayByDefault` OR `SetCurrentValue()`
4. **Internal state changes?** → DPs with private setters

**Most Important:**
1. ❌ Never set `DataContext` in UserControl constructor
2. ✅ Use Dependency Properties for bindable state
3. ✅ Use `SetCurrentValue()` for two-way propagation
4. ✅ Avoid `INotifyPropertyChanged` in UserControls
