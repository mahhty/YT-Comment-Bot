# Mobile Survival Game (Rust-like)

A mobile survival game inspired by Rust, featuring resource gathering, crafting, building, and survival mechanics optimized for touch devices.

## Features

### Core Gameplay
- **Resource Gathering**: Collect wood, stone, metal ore, food, and water
- **Crafting System**: Create tools, weapons, and building materials
- **Base Building**: Construct shelters and defensive structures
- **Survival Mechanics**: Manage hunger, thirst, health, and temperature
- **Combat System**: Fight against wildlife and other threats
- **Progression System**: Unlock new recipes and abilities

### Mobile Optimizations
- **Touch Controls**: Intuitive joystick and tap-based interface
- **Performance Optimized**: Designed for mobile hardware limitations
- **Battery Efficient**: Optimized rendering and processing
- **Responsive UI**: Scales across different screen sizes
- **Offline Play**: Works without internet connection

## Getting Started

### Requirements
- Unity 2022.3 LTS or later
- Android SDK for Android builds
- Xcode for iOS builds

### Installation
1. Clone this repository
2. Open the project in Unity
3. Configure build settings for your target platform
4. Build and deploy to your mobile device

## Controls

### Touch Controls
- **Movement**: Virtual joystick (bottom-left)
- **Camera**: Drag to look around
- **Interact**: Tap on objects
- **Inventory**: Tap inventory button
- **Crafting**: Tap crafting button
- **Building**: Long press for build mode

## Game Systems

### Resource Types
- **Wood**: From trees and fallen logs
- **Stone**: From rocks and stone deposits
- **Metal Ore**: From metal nodes
- **Food**: From hunting animals and gathering plants
- **Water**: From rivers, lakes, and rain collection

### Crafting Recipes
- **Tools**: Hatchet, Pickaxe, Spear
- **Weapons**: Bow, Arrows, Melee weapons
- **Building**: Foundations, Walls, Doors, Roofs
- **Survival**: Campfire, Sleeping bag, Water collector

### Building System
- **Foundation-based**: Start with foundations, build up
- **Modular**: Snap-together building pieces
- **Upgradeable**: Upgrade materials from wood to stone to metal

## Development

### Project Structure
```
Assets/
├── Scripts/          # All C# scripts
├── Prefabs/          # Game object prefabs
├── Materials/        # 3D materials and textures
├── Audio/           # Sound effects and music
├── UI/              # User interface assets
└── Scenes/          # Unity scenes
```

### Key Scripts
- `PlayerController.cs`: Handle player movement and input
- `ResourceManager.cs`: Manage resource collection and storage
- `CraftingSystem.cs`: Handle recipe management and crafting
- `BuildingSystem.cs`: Manage structure placement and building
- `SurvivalManager.cs`: Track hunger, thirst, health, temperature

## License

This project is licensed under the MIT License - see the LICENSE file for details.