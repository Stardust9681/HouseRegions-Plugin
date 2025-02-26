Configuration
=

## Config File
A config file will be automatically created when this plugin is loaded for the first time. To access it, navigate to your local tShock install folder (directory), open to `../tshock/House Regions/`. If this is working correctly, you should find a file, `HouseConfig.json`.
If you used a previous version of this plugin, you may also find `Config.xml`. This config is deprecated, and it is recommended you begin using `HouseConfig.json` instead. `Config.xml` may be safely removed at any time, and should not reappear after doing so.

## Config Fields
Inside the config file you will find a number of settings. The following explains in detail what each is used for, and how it works.

### MaxHousesPerUser (Type: `Int32`)
Defines the maximum number of houses users without the `houseregions.nolimits` permission may create.
When set to 0, no limit is imposed.

Default:
```
10
```

### MinSize (Type: `complex`)
Defines the minimum size houses made by users without the `houseregions.nolimits` permission may be.

Default:
```
TotalTiles: 10,
Width: 8,
Height: 4
```

### MaxSize (Type: `complex`)
Defines the maximum size houses made by users without the `houseregions.nolimits` permission may be.

Default:
```
TotalTiles: 7500,
Width: 150,
Height: 150
```

### AllowTShockRegionOverlapping (Type: `Boolean`)
Allows or disallows user-defined housing regions to overlap with standard tShock regions.
tShock regions prefixed with a star (\*) ignore this setting, and will be allowed to overlap regardless.
tShock regions defined after the house will not adhere to this setting.
User-defined houses, regardless of this setting, cannot overlap with house regions owned by other players.

Default:
```
false
```

### HouseLiquidProtection (Type: `Boolean`)
Enables or disabled advanced liquid protection for housing regions.
When enabled, liquids will be prevented from flowing into the region. When enabled, liquid bombs cannot create liquid inside regions the player does not have access to.
May affect server performance with liquids.

Default:
```
true
```

### DefaultZIndex (Type: `Int32`)
Default Z-layer for house regions to be made on.
This plugin wraps tShock's existing region functionality, which includes Z-layering. Higher Z-index gives higher priority (so for overlapping regions, the one with a higher Z-index will take precedence)

Default:
```
0
```