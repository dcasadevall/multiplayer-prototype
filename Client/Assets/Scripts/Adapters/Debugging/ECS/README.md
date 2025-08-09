# Entity debug system, built with AI

FYI this whole namespace was built entirely by AI, so it may not be perfect.

## **ECS Debugging System Overview**

I've created several debugging tools that work together to give you complete visibility into your ECS world:

### **1. ECSWorldDebugger**
- **Real-time Inspector Display**: Shows entity count, component breakdown, and world info in the Inspector
- **Console Logging**: Logs debug information to Unity console
- **Context Menu Actions**: Right-click for "Dump Full World State", "Log All Entities", etc.

### **2. ECSVisualDebugger**
- **Scene View Visualization**: Draws entity positions as colored spheres in Scene view
- **Entity Labels**: Shows entity IDs and component info above entities
- **Color Coding**: Players (blue), Projectiles (red), Others (white)
- **Editor Menu**: Toggle visibility and log entity positions

### **3. ECSInspectorWindow**
- **Real-time Editor Window**: Window > ECS Inspector
- **Entity Browser**: Expandable list of all entities with their components
- **Filtering**: Search by name, filter by component type, position, etc.
- **Property Inspection**: View all component properties using reflection
- **Scene Integration**: Focus entities in Scene view

### **4. ECSDebugSystem**
- **Change Tracking**: Logs entity creation, destruction, and component changes
- **Periodic Summaries**: Logs entity/component counts every 60 ticks
- **Automatic Detection**: Tracks all entity lifecycle events

### **5. ECSDebugManager**
- **Easy Setup**: Add to any GameObject in your scene
- **Component Management**: Automatically creates and manages debug components
- **Editor Integration**: Menu items for quick setup

## **How to Use**

### **Quick Setup:**
1. **Add Debug Manager**: `ECS > Debug > Add Debug Manager to Scene`
2. **Open Inspector**: `Window > ECS Inspector`
3. **Register with DI**: Make sure the `ECSDebugManager` is registered in your VContainer setup

### **Manual Setup:**
```csharp
// In your DI container setup
builder.RegisterComponent(debugManagerGameObject.GetComponent<ECSDebugManager>());
builder.Register<ECSDebugSystem>(Lifetime.Singleton);
```

### **Features You Can Use:**

1. **Real-time Monitoring**: Watch entity counts and component breakdowns in the Inspector
2. **Visual Debugging**: See entities as colored spheres in Scene view
3. **Detailed Inspection**: Use the ECS Inspector window to browse all entities
4. **Change Tracking**: Get console logs when entities are created/destroyed
5. **Property Inspection**: View all component values using reflection
6. **Filtering**: Find specific entities by type, component, or name

### **Menu Items Available:**
- `ECS > Debug > Add Debug Manager to Scene`
- `ECS > Debug > Open ECS Inspector`
- `ECS > Debug > Log All Entities`
- `ECS > Debug > Show Entity Positions` (in Scene view)
- `ECS > Debug > Show Entity Labels` (in Scene view)

This gives you complete visibility into your ECS world, making it easy to debug entity creation, component changes, and system behavior. The tools are designed to be non-intrusive and can be easily enabled/disabled as needed.

```csharp
// In your DI container setup
builder.RegisterComponent(debugManagerGameObject.GetComponent<ECSDebugManager>());
builder.Register<ECSDebugSystem>(Lifetime.Singleton);
```
