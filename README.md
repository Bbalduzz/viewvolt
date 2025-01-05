# ViewVolt

A Revit plugin for managing and storing 3D view positions. It provides programmatic control over Revit 3D view positions through a dedicated interface. It enables systematic view management for documentation and team coordination.

### Features
- Position storage and retrieval
- View position search functionality
- Batch view management
- Quick-save operations
- XML-based position data
- Custom naming system

### Technical Stack
- C# / .NET Framework
- Autodesk Revit API
- Windows Forms
- XML Serialization

## Installation & Setup

### System Requirements
- Autodesk Revit 2019+
- .NET Framework 4.7.2+
- Windows 7+

### Installation Process
1. Terminate all Revit instances
2. Extract release package
3. Deploy to Revit Addins directory: `C:\ProgramData\Autodesk\Revit\Addins\[VERSION]`
   - ViewVolt.dll
   - ViewVolt.addin
4. Launch Revit

### Data Storage
Position data location: `%AppData%\Roaming\ViewVoltPositions.xml`

## API Documentation

### ViewPosition Class
Core data structure for position management.

#### Properties
| Property | Type | Purpose |
|----------|------|---------|
| Name | string | Position identifier |
| EyeX/Y/Z | double | Camera coordinates |
| UpX/Y/Z | double | Up vector |
| ForwardX/Y/Z | double | Forward vector |
| CreatedAt | DateTime | Creation timestamp |

### Core Methods

#### `QuickSaveCommand.Execute`
Saves current view position.

Parameters:
- `commandData`: ExternalCommandData
- `message`: string (ref)
- `elements`: ElementSet

Returns: Result.Succeeded/Failed

Implementation:
```csharp
var command = new QuickSaveCommand();
var result = command.Execute(commandData, ref message, elements);
```

#### `ViewManagerForm.LoadSelectedView`
Applies stored position to active view.

Error States:
- Non-3D view active
- Invalid position data
- Transaction failure


> MIT license