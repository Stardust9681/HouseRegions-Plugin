House Regions Plugin
===================

### A TShock Region Wrapper for Housing Purposes
This plugin provides players on TShock driven Terraria servers the possibility of defining houses in which other players can not alter any tiles. It accomplishes this by utilizing TShock's region system, i.e. this plugin simply wraps the region system functionality with an easy to use and more restricted interface designed for regular users.

For quick usage and for the sake of usabilitiy house regions are kept entirely unnamed, when being defined two points to mark the region boundaries are sufficient.
To change parameters of a house region later, like adding shared players or groups, the player must simply stand in the region they want to change and execute the related house commands. The maximum amount of house regions per user, several size restrictions, and whether house regions can overlap with regular TShock regions can be configured.

Warning: TShock regions defined through this plugin are named in the format `*H_User:HouseIndex` thus, if you manually define a TShock region with this name format, this plugin will treat the region just like a house.

### How to Install
Note: This plugin requires 
[TerrariaAPI-Server](https://github.com/NyxStudios/TerrariaAPI-Server) and [TShock](https://github.com/NyxStudios/TShock) in order to work. You can't use this with a vanilla Terraria server.

Grab the latest release and put the _.dll_ files into your server's _ServerPlugins_ directory. Also put the contents of the _tshock/_ folder into your server's _tshock_ folder. You may change the configuration options to your needs by editing the _tshock/House Regions/HouseConfig.json_ file.

### Commands
| Command Name | Permissions Required | Description |
| ------------ | -------------------- | ----------- |
| /house | `None` | Introduction to this plugin |
| /house commands | `None` | Shows the list of this plugin's commands |
| /house summary | `houseregions.housingmaster` | Shows a list of all house owners |
| /house info | `None` | Shows information about a house |
| /house data | `houseregions.housingmaster` | Shows information about a house by name |
| /house define | `houseregions.define` | Creates a new house |
| /house resize <up\|down\|left\|right> <amount> | `houseregions.define` | Resizes a house (up/down/left/right) by a given amount (inlcuding negatives) |
| /house share <user> | `houseregions.share` | Shares a house with a given user |
| /house unshare <user> | `houseregions.share` | Revokes access from a given user |
| /house shareGroup <group> | `houseregions.sharegroup` | Shares a house with a given group |
| /house unshareGroup <group> | `houseregions.sharegroupd` | Revokes access from a given group |
| /house delete | `houseregions.delete` | Deletes a house |
| /house scan | `None` | Outlines nearby houses |
| house reloadconfig | `houseregions.cfg` | Reloads the plugin's config |

To get more information about a command type `/house <command> help` ingame.

### Permissions
| Permission | Description |
| ---------- | ----------- |
| `houseregions.define` | Can define or resize existing houses |
| `houseregions.delete` | Can delete existing houses |
| `houseregions.share` | Can share houses |
| `houseregions.sharewithgroups` | Can share houses with TShock groups |
| `houseregions.nolimits` | Can define houses without a maximum limit or size restrictions |
| `houseregions.housingmaster` | Can display a list of all house owners. Can change settings of any house, owned or not |
| `houseregions.cfg` | Can reload the configuration file |

### Configuration
Please see Config Info.md

### Credits
Original Plugin by [Coder Cow](https://github.com/CoderCow)
> Update to tShock 5.2 from [Max The Great](https://github.com/Maxthegreat99)
> Contributions by [several others](https://github.com/Stardust9681/HouseRegions-Plugin/graphs/contributors)

Icon made by [freepik](http://www.freepik.com/)