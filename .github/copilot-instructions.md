# Copilot Instructions for DiagramFlow

## Project Overview

DiagramFlow is a WPF-based diagram editor desktop application that allows users to create, view, and manipulate diagrams on a scrollable canvas. The application focuses on canvas operations including zoom and pan functionality, shape manipulation, and undo/redo capabilities.

## Technical Stack

- **Framework**: .NET Framework 4.7.2
- **Language**: C# 7.3
- **UI Technology**: WPF (Windows Presentation Foundation)
- **Target OS**: Windows 10/11
- **Required Libraries**:
  - Fody.PropertyChanged: For automatic property change notification in ViewModels
  - ReactiveProperty: For reactive property and command definitions

## Project Structure

```
DiagramFlow/
├── DiagramFlow/          # Main application project
│   ├── App.xaml          # Application definition
│   ├── MainWindow.xaml   # Main window UI
│   ├── MainWindow.xaml.cs # Main window code-behind
│   ├── ViewModels/       # ViewModel classes
│   └── Properties/       # Assembly info and resources
├── Documents/            # Project documentation (in Japanese)
│   ├── RequirementsDefinition.md
│   ├── DiagramEditorSpecification.md
│   └── ImplementationPlan.md
└── DiagramFlow.slnx      # Solution file
```

## Build and Test

### Building the Project
```bash
# Build with MSBuild (from Visual Studio Developer Command Prompt)
msbuild DiagramFlow.slnx /p:Configuration=Release
```

### Testing
- Currently, there is no automated test infrastructure in place
- Manual testing should focus on:
  - Canvas zoom operations (Ctrl + MouseWheel)
  - Pan operations (right-click + drag)
  - Double-click zoom functionality
  - Zoom slider synchronization

## Coding Conventions

### General Guidelines

1. **MVVM Pattern**: Follow the Model-View-ViewModel pattern strictly
   - Views are defined in XAML files
   - ViewModels handle business logic and UI state
   - Use data binding to connect Views and ViewModels

2. **Property Change Notifications**: Use Fody.PropertyChanged for automatic INotifyPropertyChanged implementation
   - ViewModels should be decorated with appropriate attributes
   - Avoid manual property change notifications unless necessary

3. **Reactive Properties**: Use ReactiveProperty for properties and commands
   - Example: `public ReactiveProperty<double> ZoomScale { get; }`

4. **Event Handling**:
   - Prefer code-behind for direct UI event handlers (e.g., mouse events)
   - Use Commands for user actions that involve business logic
   - Always check for null references when accessing ViewModels

5. **Naming Conventions**:
   - PascalCase for class names, methods, and properties
   - Private fields: prefix with underscore + camelCase (e.g., `_isPanning`, `_lastMousePosition`)
   - Meaningful names that describe purpose

### WPF-Specific Conventions

1. **Layout**: Use `ScrollViewer` with `Canvas` for diagram editor
2. **Transformations**: Use `ScaleTransform` for zoom operations
3. **Mouse Operations**:
   - Left-click: Shape selection and movement
   - Right-click + drag: Pan operation
   - Ctrl + MouseWheel: Zoom in/out
   - Double-click: Toggle zoom
4. **Update UI after transformations**: Call `UpdateLayout()` when needed after scale changes

### Code Style

- Use explicit types instead of `var` for clarity
- Add XML documentation comments for public methods and properties
- Keep methods focused and small (single responsibility)
- Handle edge cases (e.g., clamp zoom values between 0.1 and 4.0)

## Important Constraints

1. **Do NOT use**:
   - .NET Core or .NET 6+ (must use .NET Framework 4.7.2)
   - External UI control libraries (DevExpress, Infragistics, etc.)
   - Standard features should use WPF built-in controls only

2. **Must use**:
   - Fody.PropertyChanged for property change notifications
   - ReactiveProperty for reactive properties and commands

## Common Tasks

### Adding a New ViewModel
1. Create class in `ViewModels` folder
2. Use Fody.PropertyChanged attributes for automatic notifications
3. Define properties using ReactiveProperty
4. Connect to View via DataContext binding

### Modifying Zoom/Pan Behavior
1. Check the requirements in `Documents/RequirementsDefinition.md`
2. Update `MainWindow.xaml.cs` code-behind for event handlers
3. Update ViewModel properties if needed
4. Test with mouse wheel and slider synchronization

### Adding New Shapes
1. Define shape classes in appropriate folder
2. Update Canvas rendering logic
3. Implement selection and manipulation logic
4. Consider undo/redo impact

## Performance Considerations

- Zoom and pan operations must be responsive without UI freezing
- Handle high-frequency mouse wheel events efficiently
- Use `UpdateLayout()` judiciously to avoid performance issues

## Future Enhancements (Out of Current Scope)

- Shape selection and multi-selection
- Undo/Redo functionality
- Save/Load diagram data
- Touch input support
- Connection lines between shapes
- Minimap display

## Language Note

Most documentation in the `Documents/` folder is written in Japanese. The core requirements specify:
- Canvas zoom capabilities (最小: 10%, 最大: 400% / minimum: 10%, maximum: 400%)
- Pan with right-click drag
- Double-click zoom with animation
- Zoom slider synchronization

## Getting Started for New Contributors

1. Ensure you have Visual Studio with WPF workload installed
2. Open `DiagramFlow.slnx` in Visual Studio
3. Restore NuGet packages (Fody.PropertyChanged, ReactiveProperty)
4. Build and run the application
5. Review documentation in `Documents/` folder for detailed requirements
