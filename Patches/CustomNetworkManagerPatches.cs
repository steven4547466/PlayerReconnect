using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using MEC;

namespace PlayerReconnect.Patches
{
	[HarmonyPatch(typeof(CustomNetworkManager), nameof(CustomNetworkManager.OnServerDisconnect), new[] { typeof(NetworkConnection) })]
	class CustomNetworkManagerOnServerDisconnectPatch
	{
		public static bool Prefix(CustomNetworkManager __instance, NetworkConnection conn)
		{
			Player savedPlayer = Player.Get(conn.identity.gameObject);
			if (TrackingAndMethods.DisconnectedPlayers.ContainsKey(savedPlayer.UserId)) return false;
			try
			{
				if (!Round.IsStarted || savedPlayer == null || savedPlayer.IsHost)
				{
					TrackingAndMethods.Left(conn);
					TrackingAndMethods.Dispose(__instance, conn);
					return false;
				}
				TrackingAndMethods.DisconnectedPlayers.Add(savedPlayer.UserId, new Tuple<ReconnectData, CustomNetworkManager, NetworkConnection>(new ReconnectData(savedPlayer), __instance, conn));
				PlayerManager.RemovePlayer(conn.identity.gameObject);
				UnityEngine.Object.DestroyImmediate(conn.identity.gameObject.GetComponent<PlayerPositionManager>());
			}
			catch(Exception e)
			{
				Log.Error(e);
			}
			TrackingAndMethods.Coroutines.Add(savedPlayer.UserId, new List<CoroutineHandle>() {
				Timing.CallDelayed(Plugin.Instance.Config.ReconnectTime, () =>
				{
					try
					{
						if (!TrackingAndMethods.DisconnectedPlayers.ContainsKey(savedPlayer.UserId))
							return;
						TrackingAndMethods.Left(conn);
						TrackingAndMethods.Dispose(__instance, conn);
					}
					catch (Exception e)
					{
						Log.Error($"Server disconnect patch issue: {e}");
						TrackingAndMethods.Dispose(__instance, conn);
					}
				}),
				Timing.RunCoroutine(TrackingAndMethods.AhpDecay(TrackingAndMethods.DisconnectedPlayers[savedPlayer.UserId].Item1.PlayerStats))
			});

			return false;
		}
	}
}
