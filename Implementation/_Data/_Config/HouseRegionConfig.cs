using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TShockAPI;

namespace Terraria.Plugins.CoderCow.HouseRegions
{
	public interface IHouseSizeRestraint
	{
		int TotalTiles { get; set; }
		int Width { get; set;  }
		int Height { get; set;  }
	}
	public class HouseRegionConfig
	{
		public struct HouseSizeConfig : IHouseSizeRestraint
		{
			public int TotalTiles { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }

			public HouseSizeConfig(int totalTiles, int width, int height) : this()
			{
				this.TotalTiles = totalTiles;
				this.Width = width;
				this.Height = height;
			}
		}
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		[DefaultValue(10)]
		public int MaxHousesPerUser { get; internal set; }
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		public IHouseSizeRestraint MinSize { get; internal set; } = new HouseSizeConfig(25, 8, 4);
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		public IHouseSizeRestraint MaxSize { get; internal set; } = new HouseSizeConfig(7500, 150, 150);
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		[DefaultValue(false)]
		public bool AllowTShockRegionOverlapping { get; internal set; }
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		[DefaultValue(true)]
		public bool HouseLiquidProtection { get; internal set; }
		[JsonInclude]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include | DefaultValueHandling.Populate)]
		[DefaultValue(0)]
		public int DefaultZIndex { get; internal set; }
	}
	public class ConfigurationFile : TShockAPI.Configuration.ConfigFile<HouseRegionConfig>
	{
		public static readonly string DirectoryPath = Path.Combine(TShock.SavePath, "House Regions");
		public static readonly string FilePath = Path.Combine(DirectoryPath, "HouseConfig.json");

		public bool TryRead(string filePath, out HouseRegionConfig config)
		{
			config = Read(filePath, out bool notFound);
			return !notFound;
		}
	}
}
