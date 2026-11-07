# Changelog

## v1.1.3

### Added
- Support for carts custom inventory size

### Fixed
- Chest and wardrobe windows overlapping expanded player inventories or extending below the screen.
- Inventory columns being clipped when increasing container capacity, including 10 × 10 inventories.
- Scrollbar positioning and spacing when re-enabling scrollbars or resizing container inventories.

### Changed
- Container windows now account for player inventory size, including additional rows unlocked through game progression.
- With scrollbars removed, container windows automatically scale to keep all rows and columns visible on screen.
- Compiled with Valheim `v1.0.21`
- Updated BepInEx dependency `denikson-BepInExPack_Valheim-5.4.2360`

## v1.1.2

### Fixed
- Chest inventory dimensions not being restored correctly when loading containers
- Chest inventory UI clipping during the closing animation

## v1.1.1

### Fixed
- Chest inventory now correctly updates

## v1.1.0

### Added
- Customizable inventory capacity for supported chests and wardrobes.
- Option to remove inventory scrollbars and display all storage slots on a single page.
- Support for wardrobes.
- Option to place items in the topmost available inventory slots rather than the first abvailable bottom slot.

### Changed
- Reorganized configuration settings with automatic migration of existing values.
- If you experience configuration issues, delete `BepInEx\config\com.hex.chesthacks.cfg` and restart Valheim to regenerate it.

## v1.0.0
- Initial release
