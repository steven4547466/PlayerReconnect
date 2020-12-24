using System;
using Exiled.Events.EventArgs;
using Exiled.Events.Handlers;
using Exiled.Loader.Features;
using HarmonyLib;
using MEC;
using UnityEngine;

namespace PlayerReconnect.Patches
{
	[HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.CallCmdServerSignatureComplete)), HarmonyPriority(Priority.First)]
	class Joined
	{
		public static void Postfix(ServerRoles __instance)
		{

			try
			{
				if (!__instance.PublicKeyAccepted)
					return;

				if (Exiled.API.Features.Player.Dictionary.ContainsKey(__instance.gameObject))
					return;

				var player = new Exiled.API.Features.Player(ReferenceHub.GetHub(__instance.gameObject));
				Exiled.API.Features.Player.Dictionary.Add(__instance.gameObject, player);

				Exiled.API.Features.Log.SendRaw($"Player {player?.Nickname} ({player?.UserId}) ({player?.Id}) connected with the IP: {player?.IPAddress}", ConsoleColor.Green);

				if (PlayerManager.players.Count >= CustomNetworkManager.slots)
					MultiAdminFeatures.CallEvent(MultiAdminFeatures.EventType.SERVER_FULL);

				Timing.CallDelayed(0.25f, () =>
				{
					if (player?.IsMuted == true)
						player.ReferenceHub.characterClassManager.SetDirtyBit(2UL);
				});

				bool willRespawn = false;
				if (TrackingAndMethods.DisconnectedPlayers.ContainsKey(player.UserId))
				{
					willRespawn = true;
					var tuple = TrackingAndMethods.DisconnectedPlayers[player.UserId];
					tuple.Item1.Player.ClearInventory();
					tuple.Item1.Respawned = true;
					TrackingAndMethods.Left(tuple.Item3, true);
					TrackingAndMethods.Dispose(tuple.Item2, tuple.Item3);
				}

				if (TrackingAndMethods.Coroutines.ContainsKey(player.UserId))
				{
					foreach (CoroutineHandle coroutine in TrackingAndMethods.Coroutines[player.UserId])
						Timing.KillCoroutines(coroutine);
					TrackingAndMethods.Coroutines.Remove(player.UserId);
				}
				TrackingAndMethods.RespawnPlayer(player);

				if (willRespawn)
					return;

				var ev = new JoinedEventArgs(Exiled.API.Features.Player.Get(__instance.gameObject));

				Player.OnJoined(ev);
			}
			catch (Exception exception)
			{
				Exiled.API.Features.Log.Error($"{typeof(Joined).FullName}:\n{exception}");
			}
		}
	}
}