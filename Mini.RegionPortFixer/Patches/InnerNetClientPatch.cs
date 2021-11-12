// <copyright file="InnerNetClientPatch.cs" company="miniduikboot">
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

// COPYRIGHT NOTE: Some methods from this file have been copied from Among Us and are not covered under the GPL. When this is the case, a comment has been added to these methods.
namespace Mini.RegionPortFixer.Patches
{
	using System;
	using HarmonyLib;
	using InnerNet;
	using Hazel.Udp;
	using Il2CppSystem.Net;

	// From Reactor
	internal static class CustomServersPatch
	{
		/// <summary>
		/// Send the account id only to Among Us official servers
		/// </summary>
		[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.GetConnectionData))]
		public static class DontSendAccountIdPatch
		{
			public static void Prefix(ref bool useDtlsLayout)
			{
				var serverManager = ServerManager.Instance;
				DnsRegionInfo? region = serverManager.CurrentRegion.TryCast<DnsRegionInfo>();
				if (region == null || !region.Fqdn.EndsWith("among.us"))
				{
					RegionPortFixerPlugin.LogMessage($"MPRF: not officials, disabling DTLS layout");
					useDtlsLayout = false;
				}
				else
				{
					RegionPortFixerPlugin.LogMessage($"MPRF: connecting to officials, enabling DTLS layout");
				}
			}
		}

		/// <summary>
		/// Encrypt connection only to Among Us official servers
		/// </summary>
		[HarmonyPatch(typeof(AuthManager), nameof(AuthManager.CreateDtlsConnection))]
		public static class DontEncryptCustomServersPatch
		{
			public static bool Prefix(ref UnityUdpClientConnection __result, string targetIp, ushort targetPort)
			{
				var serverManager = ServerManager.Instance;
				DnsRegionInfo? region = serverManager.CurrentRegion.TryCast<DnsRegionInfo>();
				if (region == null || !region.Fqdn.EndsWith("among.us"))
				{
					RegionPortFixerPlugin.LogMessage($"MPRF: not officials, disabling DTLS connection");
					var remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort - 3);
					__result = new UnityUdpClientConnection(remoteEndPoint);
					return false;
				}
				RegionPortFixerPlugin.LogMessage($"MPRF: connecting to officials, enabling DTLS connection");

				return true;
			}
		}
	}

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SetEndpoint))]
	public static class IncSetEndpointPatch
	{
		public static void Prefix(InnerNetClient __instance, ref string addr, ref ushort port)
		{
			RegionPortFixerPlugin.LogMessage($"MRPF: Connecting to {addr}:{port}");

			// TODO prevent local game to get corrected
			ServerManager mgr = DestroyableSingleton<ServerManager>.Instance;
			if (string.Equals(addr, mgr.OnlineNetAddress, StringComparison.Ordinal) && __instance.GameMode == GameModes.OnlineGame)
			{
				RegionPortFixerPlugin.LogMessage($"MRPF: Correcting port to {mgr.OnlineNetPort}");
				port = mgr.OnlineNetPort;
			}
		}
	}
	// End of Reactor code

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Connect))]
	public static class IncConnectPatch
	{
		public static void Prefix(InnerNetClient __instance)
		{
			RegionPortFixerPlugin.LogMessage($"joining on {__instance.networkAddress}:{__instance.networkPort}, connected: {__instance.AmConnected}");
		}
	}

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.JoinGame))]
	public static class IncJoinGamePatch
	{
		public static void Prefix(InnerNetClient __instance)
		{
			RegionPortFixerPlugin.LogMessage($"joining on {__instance.networkAddress}:{__instance.networkPort}, connected: {__instance.AmConnected}");
		}
	}

	[HarmonyPatch(typeof(DnsRegionInfo), nameof(DnsRegionInfo.PopulateServers))]
	public static class PopulateServersPatch
	{
		public static bool Prefix(DnsRegionInfo __instance)
		{
			// AUTHORSHIP: This method is copied from Among Us 2021.6.30 and is not covered under the GPL
			RegionPortFixerPlugin.LogMessage($"Populating {__instance.Name} with fqdn {__instance.Fqdn}");

			try
			{
				IPAddress[] hostAddresses = Dns.GetHostAddresses(__instance.Fqdn);
				if (hostAddresses == null)
				{
					RegionPortFixerPlugin.LogMessage("HA null!!!");
					return false;
				}

				RegionPortFixerPlugin.LogMessage($"Got {hostAddresses.Length} servers");
				ServerInfo[] array = new ServerInfo[hostAddresses.Length];
				for (int i = 0; i < hostAddresses.Length; i++)
				{
					RegionPortFixerPlugin.LogMessage($"Got addr {hostAddresses[i]}");
					array[i] = new ServerInfo($"{__instance.Name}-{i}", hostAddresses[i].ToString(), __instance.Port);
				}

				__instance.cachedServers = array;
			}
			catch (Exception e)
			{
				RegionPortFixerPlugin.LogMessage($"Failed to populate, {e.Message}; {e.StackTrace}");
				__instance.cachedServers = new ServerInfo[1]
				{
					// FIX: do not hardcode 22023 here
					new ServerInfo(__instance.Name ?? string.Empty, __instance.DefaultIp, __instance.Port),

					// END OF FIX
				};
			}

			// Do not execute original function
			return false;
		}
	}

	[HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
	public static class JgbOnClickPatch
	{
		public static bool Prefix(JoinGameButton __instance)
		{
			// AUTHORSHIP: This method is copied from Among Us 2021.6.30 and is not covered under the GPL
			// Debug log statements are added
			RegionPortFixerPlugin.LogMessage($"JoingameButton clicked, GameMode {__instance.GameMode}");
			if (string.IsNullOrWhiteSpace(__instance.netAddress))
			{
				RegionPortFixerPlugin.LogMessage("netaddr is null or whitespace");
				return false;
			}

			if (__instance.GameMode == GameModes.OnlineGame && !DestroyableSingleton<AccountManager>.Instance.CanPlayOnline())
			{
				AmongUsClient.Instance.LastDisconnectReason = DisconnectReasons.NotAuthorized;
				DestroyableSingleton<DisconnectPopup>.Instance.Show();
			}
			else
			{
				if (NameTextBehaviour.Instance && NameTextBehaviour.Instance.ShakeIfInvalid())
				{
					RegionPortFixerPlugin.LogMessage("MTRF: Shaking");
					return false;
				}

				if (StatsManager.Instance.AmBanned)
				{
					AmongUsClient.Instance.LastDisconnectReason = DisconnectReasons.IntentionalLeaving;
					DestroyableSingleton<DisconnectPopup>.Instance.Show();
				}
				else
				{
					if (!DestroyableSingleton<MatchMaker>.Instance.Connecting(__instance))
					{
						RegionPortFixerPlugin.LogMessage("MTRF: already connecting");
						return false;
					}

					AmongUsClient.Instance.GameMode = __instance.GameMode;
					if (__instance.GameMode == GameModes.OnlineGame)
					{
						RegionPortFixerPlugin.LogMessage("MTRF: online game");

						AmongUsClient.Instance.SetEndpoint(DestroyableSingleton<ServerManager>.Instance.OnlineNetAddress, 22023);

						AmongUsClient.Instance.MainMenuScene = "MMOnline";
						int num = GameCode.GameNameToInt(__instance.GameIdText.text);
						if (num == -1)
						{
							if (string.IsNullOrWhiteSpace(__instance.GameIdText.text))
							{
								TextTranslatorTMP component = __instance.gameNameText.GetComponent<TextTranslatorTMP>();
								if ((bool)component)
								{
									component.ResetText();
								}
							}

							_ = __instance.StartCoroutine(Effects.SwayX(__instance.GameIdText.transform));
							DestroyableSingleton<MatchMaker>.Instance.NotConnecting();
							RegionPortFixerPlugin.LogMessage("MTRF: code bad");
							return false;
						}

						AmongUsClient.Instance.GameId = num;
					}
					else
					{
						RegionPortFixerPlugin.LogMessage("MTRF: not online game");
						AmongUsClient.Instance.SetEndpoint(__instance.netAddress, 22023);
						AmongUsClient.Instance.GameId = 32;
						AmongUsClient.Instance.GameMode = GameModes.LocalGame;
						AmongUsClient.Instance.MainMenuScene = "MatchMaking";
					}

					_ = __instance.StartCoroutine(__instance.JoinGame());
				}
			}

			return false;
		}
	}
}
