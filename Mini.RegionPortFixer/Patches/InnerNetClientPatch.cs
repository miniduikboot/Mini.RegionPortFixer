
// COPYRIGHT NOTE: Some methods from this file have been copied from Among Us and are not covered under the GPL. When this is the case, a comment has been added to these methods.
namespace Mini.RegionPortFixer.Patches
{
	using HarmonyLib;
	using InnerNet;
	using UnityEngine;
	using System;
	using System.Net;

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SetEndpoint))]
	public static class InnerNetClientSetEndpointPatch
	{
		public static void Prefix(InnerNetClient __instance, ref string addr, ref ushort port)
		{
			Debug.Log($"MRPF: Connecting to {addr}:{port}");
			// TODO prevent local game to get corrected
			ServerManager mgr = DestroyableSingleton<ServerManager>.Instance;
			if (string.Equals(addr, mgr.OnlineNetAddress, StringComparison.Ordinal) && __instance.GameMode == GameModes.OnlineGame)
			{
				Debug.Log($"MRPF: Correcting port to {mgr.OnlineNetPort}");
				port = mgr.OnlineNetPort;
			}
		}
	}

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Connect))]
	public static class INCConnectPatch
	{
		public static void Prefix(InnerNetClient __instance)
		{
			Debug.Log($"MPRF: joining on {__instance.networkAddress}:{__instance.networkPort}, connected: {__instance.AmConnected}");
		}
	}

	[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.JoinGame))]
	public static class INCJoinGamePatch
	{
		public static void Prefix(InnerNetClient __instance)
		{
			Debug.Log($"MPRF: joining on {__instance.networkAddress}:{__instance.networkPort}, connected: {__instance.AmConnected}");
		}
	}

	[HarmonyPatch(typeof(DnsRegionInfo), nameof(DnsRegionInfo.PopulateServers))]
	public static class PopulateServersPatch
	{
		public static bool Prefix(DnsRegionInfo __instance)
		{
			// AUTHORSHIP: This method is copied from Among Us 2021.6.30 and is not covered under the GPL
			Debug.Log($"MPRF: Populating {__instance.Name} with fqdn {__instance.Fqdn}");

			try
			{
				IPAddress[] hostAddresses = Dns.GetHostAddresses(__instance.Fqdn);
				if (hostAddresses == null)
				{
					Debug.Log("MPRF: HA null!!!");
					return false;
				}
				Debug.Log($"MPRF: Got {hostAddresses.Length} servers");
				ServerInfo[] array = new ServerInfo[hostAddresses.Length];
				for (int i = 0; i < hostAddresses.Length; i++)
				{
					Debug.Log($"MPRF: Got addr {hostAddresses[i].ToString()}");
					array[i] = new ServerInfo($"{__instance.Name}-{i}", ((object)hostAddresses[i]).ToString(), __instance.Port);
				}
				__instance.cachedServers = array;
			}
			catch (Exception e)
			{
				Debug.Log($"MPRF: Failed to populate, {e.Message}; {e.StackTrace}");
				__instance.cachedServers = new ServerInfo[1]
				{
					// FIX: do not hardcode 22023 here
					new ServerInfo(__instance.Name ?? "", __instance.DefaultIp, __instance.Port)
					// END OF FIX
				};
			}
			// Do not execute original function
			return false;
		}

	}

	[HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
	public static class JGBOnClickPatch
	{
		public static bool Prefix(JoinGameButton __instance)
		{
			// AUTHORSHIP: This method is copied from Among Us 2021.6.30 and is not covered under the GPL
			// Debug log statements are added
			Debug.Log($"MPRF: JoingameButton clicked, GameMode {__instance.GameMode}");
			if (string.IsNullOrWhiteSpace(__instance.netAddress))
			{
				Debug.Log("MPRF: netaddr is null or whitespace");
				return false;
			}
			if (__instance.GameMode == GameModes.OnlineGame && !DestroyableSingleton<AccountManager>.Instance.CanPlayOnline())
			{
				AmongUsClient.Instance.LastDisconnectReason = DisconnectReasons.NotAuthorized;
				DestroyableSingleton<DisconnectPopup>.Instance.Show();
			}
			else
			{
				if ((bool)NameTextBehaviour.Instance && NameTextBehaviour.Instance.ShakeIfInvalid())
				{
					Debug.Log("MTRF: Shaking");
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
						Debug.Log("MTRF: already connecting");
						return false;
					}
					AmongUsClient.Instance.GameMode = __instance.GameMode;
					if (__instance.GameMode == GameModes.OnlineGame)
					{
						Debug.Log("MTRF: online game");

						// EDIT: stop hardcoding port
						//ServerManager mgr = DestroyableSingleton<ServerManager>.Instance;
						//AmongUsClient.Instance.SetEndpoint(mgr.OnlineNetAddress, mgr.OnlineNetPort);
						// END OF EDIT
						// TODO: find out why this edit is not needed
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
							__instance.StartCoroutine(Effects.SwayX(__instance.GameIdText.transform));
							DestroyableSingleton<MatchMaker>.Instance.NotConnecting();
							Debug.Log("MTRF: code bad");
							return false;
						}
						AmongUsClient.Instance.GameId = num;
					}
					else
					{
						Debug.Log("MTRF: not online game");
						AmongUsClient.Instance.SetEndpoint(__instance.netAddress, 22023);
						AmongUsClient.Instance.GameId = 32;
						AmongUsClient.Instance.GameMode = GameModes.LocalGame;
						AmongUsClient.Instance.MainMenuScene = "MatchMaking";
					}
					__instance.StartCoroutine(__instance.JoinGame());
				}
			}
			return false;

		}
	}
}
