using System;
using Exiled.Events.EventArgs;
using Exiled.Events.Handlers;
using Exiled.Loader.Features;
using HarmonyLib;
using MEC;
using UnityEngine;

namespace PlayerReconnect.Patches
{
	[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkIsVerified), MethodType.Setter)]
	class Joined
	{
		public static void Prefix(CharacterClassManager __instance, bool value)
		{
			try
			{
				if (!value || (string.IsNullOrEmpty(__instance.UserId) && CharacterClassManager.OnlineMode))
					return;

				if (!Exiled.API.Features.Player.Dictionary.TryGetValue(__instance.gameObject, out Exiled.API.Features.Player player))
				{
					player = new Exiled.API.Features.Player(ReferenceHub.GetHub(__instance.gameObject));

					Exiled.API.Features.Player.Dictionary.Add(__instance.gameObject, player);
				}

				Exiled.API.Features.Log.SendRaw($"Player {player?.Nickname} ({player?.UserId}) ({player?.Id}) connected with the IP: {player?.IPAddress}", ConsoleColor.Green);

				if (PlayerManager.players.Count >= CustomNetworkManager.slots)
					MultiAdminFeatures.CallEvent(MultiAdminFeatures.EventType.SERVER_FULL);

				Timing.CallDelayed(0.25f, () =>
				{
					if (player?.IsMuted == true)
						player.ReferenceHub.characterClassManager.SetDirtyBit(2UL);
				});

				if (player != null)
				{
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
					if (willRespawn) return;
				}

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