using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Terraria.Plugins.Common;
using Terraria.Plugins.Common.Hooks;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using static Org.BouncyCastle.Math.EC.ECCurve;
using TShockAPI.Configuration;

namespace Terraria.Plugins.CoderCow.HouseRegions
{
	[ApiVersion(2, 1)]
	public class HouseRegionsPlugin : TerrariaPlugin
	{
		private const string TracePrefix = @"[Housing] ";
		public const string Define_Permission = "houseregions.define";
		public const string Delete_Permission = "houseregions.delete";
		public const string Share_Permission = "houseregions.share";
		public const string ShareWithGroups_Permission = "houseregions.sharewithgroups";
		public const string NoLimits_Permission = "houseregions.nolimits";
		public const string HousingMaster_Permission = "houseregions.housingmaster";
		public const string Cfg_Permission = "houseregions.cfg";

		public static HouseRegionsPlugin LatestInstance { get; private set; }

		public static string DataDirectory => Path.Combine(TShock.SavePath, "House Regions");
		public static string ConfigFilePath => Path.Combine(HouseRegionsPlugin.DataDirectory, "Config.xml");

		private bool hooksEnabled;
		internal PluginTrace Trace { get; }
		protected PluginInfo PluginInfo { get; }
		/// <summary>
		/// [DEPRECATED] Please use <see cref="HRConfig"/> instead
		/// </summary>
		[Obsolete]
		protected Configuration Config { get; private set; }
		protected GetDataHookHandler GetDataHookHandler { get; private set; }
		protected UserInteractionHandler UserInteractionHandler { get; private set; }
		public HousingManager HousingManager { get; private set; }

		#region New Config
		private ConfigurationFile _cFile;
		public static HouseRegionConfig HRConfig { get; private set; }
		#endregion

		//Literally all you had to do, tShock
		//Now instead I'm having to do reflection to get this to work
		public static IReadOnlyDictionary<int, TShockAPI.GetDataHandlers.LiquidType> ProjectilesAffectLiquid { get; private set; }
		public static IReadOnlyDictionary<GetDataHandlers.LiquidType, Utils.TileActionAttempt> Delegates = new Dictionary<GetDataHandlers.LiquidType, Utils.TileActionAttempt>()
		{
			[GetDataHandlers.LiquidType.Water] = DelegateMethods.SpreadWater,
			[GetDataHandlers.LiquidType.Lava] = DelegateMethods.SpreadLava,
			[GetDataHandlers.LiquidType.Honey] = DelegateMethods.SpreadHoney,
			[GetDataHandlers.LiquidType.Removal] = DelegateMethods.SpreadDry,
		};


		public HouseRegionsPlugin(Main game) : base(game)
		{
			this.PluginInfo = new PluginInfo(
			  "House Regions",
			  Assembly.GetAssembly(typeof(HouseRegionsPlugin)).GetName().Version,
			  "",
			  "CoderCow",
			  "TShock regions wrapper for player housing purposes."
			);

			this.Order = 1;

			this.Trace = new PluginTrace(HouseRegionsPlugin.TracePrefix);
			HouseRegionsPlugin.LatestInstance = this;
		}

		#region [Initialization]
		public override void Initialize()
		{
			ServerApi.Hooks.GamePostInitialize.Register(this, this.Game_PostInitialize);

			this.AddHooks();
		}

		private void Game_PostInitialize(EventArgs e)
		{
			ServerApi.Hooks.GamePostInitialize.Deregister(this, this.Game_PostInitialize);

			if (!Directory.Exists(HouseRegionsPlugin.DataDirectory))
				Directory.CreateDirectory(HouseRegionsPlugin.DataDirectory);

			if (!this.InitConfig())
				return;

			ProjectilesAffectLiquid = (Dictionary<int, GetDataHandlers.LiquidType>)typeof(GetDataHandlers).GetField("projectileCreatesLiquid", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

			this.HousingManager = new HousingManager(this.Trace, this.Config);
			this.InitUserInteractionHandler();

			this.hooksEnabled = true;
		}

		private bool InitConfig()
		{
			bool updatedXmlConfig = false;
			if (File.Exists(ConfigFilePath) && File.Exists(ConfigurationFile.FilePath))
			{
				DateTime xmlAccessTime = File.GetLastAccessTime(HouseRegionsPlugin.ConfigFilePath);
				DateTime jsonAccessTime = File.GetLastAccessTime(ConfigurationFile.FilePath);
				if (jsonAccessTime < xmlAccessTime)
				{
					updatedXmlConfig = true;
				}
			}

			bool existingConfig = false;
			if (File.Exists(ConfigFilePath))
			{
				try
				{
					this.Config = Configuration.Read(ConfigFilePath);
					existingConfig = true;
				}
				catch (Exception ex)
				{
					this.Trace.WriteLineError(
					  "Reading the configuration file failed. This plugin will be disabled. Exception details:\n{0}", ex
					);

					this.Dispose();
					return false;
				}
			}
			//Do not create xml config
			//Do not pass go
			/*else if(!File.Exists(ConfigurationFile.FilePath))
			{
				var assembly = Assembly.GetExecutingAssembly();
				string resourceNamexml = assembly.GetManifestResourceNames().Single(str => str.EndsWith("Config.xml"));
				XDocument xdoc = XDocument.Load(this.GetType().Assembly.GetManifestResourceStream(resourceNamexml));
				xdoc.Save(DataDirectory + "/Config.xml");
				string resourceNamexsd = assembly.GetManifestResourceNames().Single(str => str.EndsWith("Config.xsd"));
				XDocument xsddoc = XDocument.Load(this.GetType().Assembly.GetManifestResourceStream(resourceNamexsd));
				xsddoc.Save(DataDirectory + "/Config.xsd");

				this.Config = Configuration.Read(HouseRegionsPlugin.ConfigFilePath);
			}*/

			_cFile = new ConfigurationFile();
			if (!_cFile.TryRead(ConfigurationFile.FilePath, out HouseRegionConfig config))
			{
				if (config is default(HouseRegionConfig))
				{
					Directory.CreateDirectory(ConfigurationFile.DirectoryPath);
					if (existingConfig || updatedXmlConfig)
					{
						_cFile.Settings = new HouseRegionConfig(Config);
					}
					else
					{
						_cFile.Settings = new HouseRegionConfig();
					}
					_cFile.Write(ConfigurationFile.FilePath);
					_cFile.TryRead(ConfigurationFile.FilePath, out config);
				}
			}
			else if (updatedXmlConfig)
			{
				_cFile.Settings = new HouseRegionConfig(Config);
				_cFile.Write(ConfigurationFile.FilePath);
				_cFile.TryRead(ConfigurationFile.FilePath, out config);
			}
			HRConfig = config;

			//Backwards compat (oops should have been last commit oh well)
			Config = new Configuration()
			{
				MaxHousesPerUser = HRConfig.MaxHousesPerUser,
				MinSize = HRConfig.MinSize,
				MaxSize = HRConfig.MaxSize,
				AllowTShockRegionOverlapping = HRConfig.AllowTShockRegionOverlapping,
				DefaultZIndex = HRConfig.DefaultZIndex
			};

			return true;
		}

		private void InitUserInteractionHandler()
		{
			Func<Configuration> reloadConfiguration = () => {
				if (this.isDisposed)
					return null;

				bool updatedXmlConfig = false;
				if (File.Exists(ConfigFilePath) && File.Exists(ConfigurationFile.FilePath))
				{
					DateTime xmlAccessTime = File.GetLastAccessTime(HouseRegionsPlugin.ConfigFilePath);
					DateTime jsonAccessTime = File.GetLastAccessTime(ConfigurationFile.FilePath);
					if (jsonAccessTime < xmlAccessTime)
					{
						Console.WriteLine($"{xmlAccessTime} : {jsonAccessTime}");
						updatedXmlConfig = true;
					}
				}
				
				if (updatedXmlConfig)
				{
					_cFile.Settings = new HouseRegionConfig(Configuration.Read(HouseRegionsPlugin.ConfigFilePath));
					_cFile.Write(ConfigurationFile.FilePath);
					HRConfig = _cFile.Settings;
				}
				else if (_cFile.TryRead(ConfigurationFile.FilePath, out HouseRegionConfig config))
				{
					HRConfig = config;
				}

				this.Config = new Configuration()
				{
					MaxHousesPerUser = HRConfig.MaxHousesPerUser,
					MinSize = HRConfig.MinSize,
					MaxSize = HRConfig.MaxSize,
					AllowTShockRegionOverlapping = HRConfig.AllowTShockRegionOverlapping,
					DefaultZIndex = HRConfig.DefaultZIndex
				};

				return this.Config;
			};

			this.UserInteractionHandler = new UserInteractionHandler(
			  this.Trace, this.PluginInfo, this.Config, this.HousingManager, reloadConfiguration
			);
		}
		#endregion

		#region [Hook Handling]
		private void AddHooks()
		{
			if (this.GetDataHookHandler != null)
				throw new InvalidOperationException("Hooks already registered.");

			this.GetDataHookHandler = new GetDataHookHandler(this, true);
			this.GetDataHookHandler.InvokeTileEditOnMasswireOperation = GetDataHookHandler.MassWireOpTileEditInvokeType.AlwaysPlaceWire;
			this.GetDataHookHandler.TileEdit += this.Net_TileEdit;
			On.Terraria.Liquid.AddWater += Liquid_AddWater;
			On.Terraria.Liquid.SettleWaterAt += Liquid_SettleWaterAt;
			On.Terraria.Projectile.Kill += Projectile_Kill;
		}

		private void RemoveHooks()
		{
			this.GetDataHookHandler?.Dispose();

			ServerApi.Hooks.GamePostInitialize.Deregister(this, this.Game_PostInitialize);
			On.Terraria.Liquid.AddWater -= Liquid_AddWater;
			On.Terraria.Liquid.SettleWaterAt -= Liquid_SettleWaterAt;
			On.Terraria.Projectile.Kill -= Projectile_Kill;
		}

		private void Net_TileEdit(object sender, TileEditEventArgs e)
		{
			if (this.isDisposed || !this.hooksEnabled || e.Handled)
				return;

			e.Handled = this.UserInteractionHandler.HandleTileEdit(e.Player, e.EditType, e.BlockType, e.Location, e.ObjectStyle);
		}
		
		private bool IsOnEdgeOfHouse(int x, int y)
		{
			static Rectangle ResizeBounds(Rectangle orig, int resize)
			{
				return new Rectangle(orig.X - resize, orig.Y - resize, orig.Width + (2 * resize), orig.Height + (2 * resize));
			}
			return TShock.Regions.Regions.Any(
				(Region r) => HousingManager.IsHouseRegion(r.Name) && !r.InArea(x, y) && ResizeBounds(r.Area, 1).Contains(x, y)
			);
		}
		private void Liquid_AddWater(On.Terraria.Liquid.orig_AddWater orig, int x, int y)
		{
			if (this.isDisposed || !this.hooksEnabled || !HRConfig.HouseLiquidProtection)
			{
				orig.Invoke(x, y);
				return;
			}

			if (HRConfig.HouseLiquidProtection ^ !IsOnEdgeOfHouse(x, y))
			{
				orig.Invoke(x, y);
			}
		}
		private void Liquid_SettleWaterAt(On.Terraria.Liquid.orig_SettleWaterAt orig, int x, int y)
		{
			if (this.isDisposed || !this.hooksEnabled || !HRConfig.HouseLiquidProtection)
			{
				orig.Invoke(x, y);
				return;
			}

			if (HRConfig.HouseLiquidProtection ^ !IsOnEdgeOfHouse(x, y))
			{
				orig.Invoke(x, y);
			}
		}

		private static Utils.TileActionAttempt WithPermissionCheck(Utils.TileActionAttempt action, TSPlayer player)
			=> (int x, int y)
				=> player.HasBuildPermission(x, y) && action.Invoke(x, y);
		private void Projectile_Kill(On.Terraria.Projectile.orig_Kill orig, Projectile self)
		{
			if (this.isDisposed || !this.hooksEnabled || !HRConfig.HouseLiquidProtection)
			{
				orig.Invoke(self);
				return;
			}

			if (ProjectilesAffectLiquid.Keys.Contains(self.type))
			{
				self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
					self.Center.ToTileCoordinates(),
					3f,
					WithPermissionCheck(Delegates[ProjectilesAffectLiquid[self.type]], TShock.Players[self.owner])
				);
				self.active = false;
				self.netUpdate = true;
			}
			else if (self.type is ID.ProjectileID.DirtBomb or ID.ProjectileID.DirtStickyBomb)
			{
				self.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(
					self.Center.ToTileCoordinates(),
					3f,
					WithPermissionCheck(DelegateMethods.SpreadDirt, TShock.Players[self.owner])
				);
				self.active = false;
				self.netUpdate = true;
			}
			else
				orig.Invoke(self);
		}
		#endregion

		#region [TerrariaPlugin Overrides]
		public override string Name => this.PluginInfo.PluginName;
		public override Version Version => this.PluginInfo.VersionNumber;
		public override string Author => this.PluginInfo.Author;
		public override string Description => this.PluginInfo.Description;
		#endregion

		#region [IDisposable Implementation]
		private bool isDisposed;
		public bool IsDisposed => this.isDisposed;

		protected override void Dispose(bool isDisposing)
		{
			if (this.IsDisposed)
				return;

			if (isDisposing)
			{
				this.GetDataHookHandler?.Dispose();
				this.UserInteractionHandler?.Dispose();

				this.RemoveHooks();
			}

			base.Dispose(isDisposing);
			this.isDisposed = true;
		}
		#endregion
	}
}