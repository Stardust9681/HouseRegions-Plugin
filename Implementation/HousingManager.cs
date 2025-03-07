﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mysqlx.Crud;
using Terraria.Plugins.Common;

using TShockAPI;
using TShockAPI.DB;

namespace Terraria.Plugins.CoderCow.HouseRegions
{
	public class HousingManager
	{
		private const string HouseRegionNameAppendix = "*H_";
		private const char HouseRegionNameNumberSeparator = ':';

		private Configuration config;

		public PluginTrace Trace { get; private set; }

		/// <summary>
		/// [DEPRECATED] Please use <see cref="HRConfig"/> instead
		/// </summary>
		[Obsolete]
		public Configuration Config
		{
			get { return this.config; }
			set
			{
				if (value == null) throw new ArgumentNullException();
				this.config = value;
			}
		}

		public HouseRegionConfig HRConfig => HouseRegionsPlugin.HRConfig;


		public HousingManager(PluginTrace trace, Configuration config)
		{
			if (trace == null) throw new ArgumentNullException();
			if (config == null) throw new ArgumentNullException();

			this.Trace = trace;
			this.config = config;
		}

		public void CreateHouseRegion(TSPlayer player, Rectangle area, bool checkOverlaps = true, bool checkPermissions = false, bool checkDefinePermission = false)
		{
			if (player == null) throw new ArgumentNullException();
			if (!player.IsLoggedIn) throw new PlayerNotLoggedInException();

			this.CreateHouseRegion(player.Account, player.Group, area, checkOverlaps, checkPermissions, checkDefinePermission);
		}

		public void CreateHouseRegion(UserAccount user, Group group, Rectangle area, bool checkOverlaps = true, bool checkPermissions = false, bool checkDefinePermission = false)
		{
            Console.WriteLine("A");
            if (user == null) throw new ArgumentNullException();
			if (group == null) throw new ArgumentNullException();
			if (!(area.Width > 0 && area.Height > 0)) throw new ArgumentException();
			Console.WriteLine("B");
			int maxHouses = int.MaxValue;
			if (checkPermissions)
			{
				if (!group.HasPermission(HouseRegionsPlugin.Define_Permission))
					throw new MissingPermissionException(HouseRegionsPlugin.Define_Permission);

				if (!group.HasPermission(HouseRegionsPlugin.NoLimits_Permission))
				{
					if (HRConfig.MaxHousesPerUser > 0)
						maxHouses = HRConfig.MaxHousesPerUser;

					IHouseSizeRestraint restrictingSizeConfig;
					if (!this.CheckHouseRegionValidSize(area, out restrictingSizeConfig))
						throw new InvalidHouseSizeException(restrictingSizeConfig);
				}
			}
			Console.WriteLine("C");
			if (checkOverlaps && this.CheckHouseRegionOverlap(user.Name, area))
				throw new HouseOverlapException();
			Console.WriteLine("D");
			// Find a free house index.
			int houseIndex;
			string houseName = null;
			for (houseIndex = 1; houseIndex <= maxHouses; houseIndex++)
			{
				houseName = this.ToHouseRegionName(user.Name, houseIndex);
				if (TShock.Regions.GetRegionByName(houseName) == null)
					break;
			}
			Console.WriteLine("E");
			if (houseIndex > maxHouses)
				throw new LimitEnforcementException("Max amount of houses reached.");
			Console.WriteLine("F");
			if (!TShock.Regions.AddRegion(
			  area.X, area.Y, area.Width, area.Height, houseName, user.Name, Main.worldID.ToString(),
			  this.HRConfig.DefaultZIndex
			))
				throw new InvalidOperationException("House region might already exist.");
			Console.WriteLine("G");
		}

		public string ToHouseRegionName(string owner, int houseIndex)
		{
			if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException();
			if (!(houseIndex > 0)) throw new ArgumentOutOfRangeException();

			//Disable chat tags
			owner = owner.Replace("[", "(");

			return string.Concat(
			  HousingManager.HouseRegionNameAppendix, owner, HousingManager.HouseRegionNameNumberSeparator, houseIndex
			);
		}

		public bool TryGetHouseRegionAtPlayer(TSPlayer player, out string owner, out int houseIndex, out Region region)
		{
			if (player == null) throw new ArgumentNullException();

			for (int i = 0; i < TShock.Regions.Regions.Count; i++)
			{
				region = TShock.Regions.Regions[i];
				if (region.InArea(player.TileX, player.TileY) && this.TryGetHouseRegionData(region.Name, out owner, out houseIndex))
					return true;
			}

			owner = null;
			region = null;
			houseIndex = -1;
			return false;
		}

		public bool TryGetHouseRegionData(string regionName, out string owner, out int houseIndex)
		{
			if (regionName == null) throw new ArgumentNullException();

			owner = null;
			houseIndex = -1;

			if (!regionName.StartsWith(HousingManager.HouseRegionNameAppendix))
				return false;

			int separatorIndex = regionName.LastIndexOf(HousingManager.HouseRegionNameNumberSeparator);
			if (
			  separatorIndex == -1 || separatorIndex == regionName.Length - 1 ||
			  separatorIndex <= HousingManager.HouseRegionNameAppendix.Length
			)
				return false;

			string houseIndexRaw = regionName.Substring(separatorIndex + 1);
			if (!int.TryParse(houseIndexRaw, out houseIndex))
				return false;

			owner = regionName.Substring(HousingManager.HouseRegionNameAppendix.Length, separatorIndex - HousingManager.HouseRegionNameAppendix.Length);
			return true;
		}

		public void SetHouseRegionOwner(Region region, string newOwnerName)
		{
			if (region == null) throw new ArgumentNullException();
			if (newOwnerName == null) throw new ArgumentNullException();

			string currentOwner;
			int index;
			if (!this.TryGetHouseRegionData(region.Name, out currentOwner, out index))
				throw new ArgumentException("The given region is not a house region.");

			if (currentOwner == newOwnerName)
				return;

			string newRegionName = this.ToHouseRegionName(newOwnerName, index);
			TShock.DB.Query("UPDATE Regions SET RegionName=@0,Owner=@1 WHERE RegionName=@2 AND WorldID=@3", newRegionName, newOwnerName, region.Name, Main.worldID.ToString());
			region.Name = newRegionName;
			region.Owner = newOwnerName;
		}

		public bool IsHouseRegion(string regionName)
		{
			if (regionName == null) throw new ArgumentNullException();

			if (!regionName.StartsWith(HousingManager.HouseRegionNameAppendix))
				return false;

			int separatorIndex = regionName.LastIndexOf(HousingManager.HouseRegionNameNumberSeparator);
			if (
			  separatorIndex == -1 || separatorIndex == regionName.Length - 1 ||
			  separatorIndex <= HousingManager.HouseRegionNameAppendix.Length
			)
				return false;

			string houseIndexRaw = regionName.Substring(separatorIndex + 1);
			if (!int.TryParse(houseIndexRaw, out _))
				return false;
			return true;
		}

		public bool CheckHouseRegionOverlap(string owner, Rectangle regionArea)
		{
			for (int i = 0; i < TShock.Regions.Regions.Count; i++)
			{
				Region tsRegion = TShock.Regions.Regions[i];
				if (
				  regionArea.Right < tsRegion.Area.Left || regionArea.X > tsRegion.Area.Right ||
				  regionArea.Bottom < tsRegion.Area.Top || regionArea.Y > tsRegion.Area.Bottom
				)
					continue;

				string houseOwner;
				int houseIndex;
				if (!this.TryGetHouseRegionData(tsRegion.Name, out houseOwner, out houseIndex))
				{
					if (HRConfig.AllowTShockRegionOverlapping || tsRegion.Name.StartsWith("*"))
						continue;

					return true;
				}
				if (houseOwner == owner)
					continue;

				return true;
			}

			return false;
		}

		public bool CheckHouseRegionValidSize(Rectangle regionArea, out IHouseSizeRestraint problematicConfig)
		{
			int areaTotalTiles = regionArea.Width * regionArea.Height;

			problematicConfig = HRConfig.MinSize;
			if (
			  regionArea.Width < HRConfig.MinSize.Width || regionArea.Height < HRConfig.MinSize.Height ||
			  areaTotalTiles < HRConfig.MinSize.TotalTiles
			)
				return false;

			problematicConfig = HRConfig.MaxSize;
			if (
			  regionArea.Width > HRConfig.MaxSize.Width || regionArea.Height > HRConfig.MaxSize.Height ||
			  areaTotalTiles > HRConfig.MaxSize.TotalTiles
			)
				return false;

			problematicConfig = default(Configuration.HouseSizeConfig);
			return true;
		}

		public bool CheckHouseRegionValidSize(Rectangle regionArea)
		{
			return this.CheckHouseRegionValidSize(regionArea, out _);
		}
	}
}