// <copyright file="RegionPortFixerPlugin.cs" company="miniduikboot">
// This file is part of Mini.RegionPortFixer.
//
// Mini.RegionPortFixer is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Mini.RegionPortFixer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Mini.RegionPortFixer.  If not, see https://www.gnu.org/licenses/
// </copyright>

namespace Mini.RegionPortFixer
{
	using System;
	using BepInEx;
	using BepInEx.IL2CPP;
	using BepInEx.Logging;
	using HarmonyLib;
#if REACTOR
	using Reactor;
#endif

	/**
	 * <summary>
	 * Plugin that installs user specified servers into the region file.
	 * </summary>
	  */
	[BepInPlugin(Id)]
	[BepInProcess("Among Us.exe")]
#if REACTOR
	[ReactorPluginSide(PluginSide.ClientOnly)]
#endif
	public class RegionPortFixerPlugin : BasePlugin
	{
		private const string Id = "at.duikbo.regionportfixer";

		public Harmony Harmony { get; } = new Harmony(Id);

		internal static ManualLogSource Logger;

		/**
		 * <summary>
		 * Load the plugin and install the servers.
		 * </summary>
		 */
		public override void Load()
		{
			this.Log.LogInfo("Starting Mini.RegionPortFixer r24");
			Logger = this.Log;

			this.Harmony.PatchAll();

			this.Log.LogInfo("Started Mini.RegionPortFixer");
		}

		public override bool Unload()
		{
			this.Log.LogInfo("Unloading Mini.RegionPortFixer");
			this.Harmony.UnpatchAll();
			return base.Unload();
		}

	}
}
