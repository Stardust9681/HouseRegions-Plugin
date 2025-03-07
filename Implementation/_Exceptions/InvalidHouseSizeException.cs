﻿using System;
using System.Runtime.Serialization;

namespace Terraria.Plugins.CoderCow.HouseRegions
{
	[Serializable]
	public class InvalidHouseSizeException : Exception
	{
		#region [Property: RestrictingConfig]
		private readonly IHouseSizeRestraint restrictingConfig;

		public IHouseSizeRestraint RestrictingConfig
		{
			get { return this.restrictingConfig; }
		}
		#endregion

		public InvalidHouseSizeException(string message, Exception inner = null) : base(message, inner) { }

		public InvalidHouseSizeException(IHouseSizeRestraint restrictingConfig) : base("The size of the house does not match with the configured min / max settings.")
		{
			this.restrictingConfig = restrictingConfig;
		}

		public InvalidHouseSizeException() : base("The size of the house does not match with the configured min / max settings.") { }

		protected InvalidHouseSizeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}