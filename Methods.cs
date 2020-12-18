using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;
using Mirror;
using Exiled.Events.EventArgs;
using UnityEngine;
using Exiled.API.Enums;

namespace PlayerReconnect
{
	public static partial class TrackingAndMethods
	{
		public static void RespawnPlayer(Player player)
		{
			if (player == null) return;
			if (!DisconnectedPlayers.ContainsKey(player.UserId)) return;
			ReconnectData savedPlayer = DisconnectedPlayers[player.UserId].Item1;
			PlayerStats playerStats = DisconnectedPlayers[player.UserId].Item1.PlayerStats;
			if (playerStats.Health <= 0) return;
			Timing.CallDelayed(0.1f, () => 
			{
				player.SetRole(savedPlayer.Role);
				player.ClearInventory();
				Timing.CallDelayed(1f, () =>
				{
					try
					{

						foreach(KeyValuePair<AmmoType, uint> ammoPair in savedPlayer.Ammo)
						{
							player.Ammo[(int)ammoPair.Key] = ammoPair.Value;
						}

						for (int i = 0; i < savedPlayer.Inventory.Count; i++)
						{
							Inventory.SyncItemInfo syncItemInfo = savedPlayer.Inventory[i];
							player.AddItem(syncItemInfo);
							Log.Info(player.CurrentItemIndex);
							if (savedPlayer.CurItemIndex == i)
							{
								Log.Info("Current item set: " + i);
								player.CurrentItem = syncItemInfo;
							}
							Log.Info("Item index after: " + player.CurrentItemIndex);
						}

						player.Position = savedPlayer.Position;
						player.Rotation = savedPlayer.Rotation;
						player.Rotations = savedPlayer.Rotations;
						if (savedPlayer.Camera != null) player.Camera = savedPlayer.Camera;

						player.MaxHealth = playerStats.maxHP;
						player.MaxAdrenalineHealth = playerStats.maxArtificialHealth;
						player.Health = playerStats.Health;
						player.AdrenalineHealth = playerStats.syncArtificialHealth;

						if (savedPlayer.DisplayNickname != null) player.DisplayNickname = savedPlayer.DisplayNickname;
						if (!string.IsNullOrEmpty(savedPlayer.CustomPlayerInfo)) player.CustomPlayerInfo = savedPlayer.CustomPlayerInfo;
						if (savedPlayer.CufferId != -1) player.CufferId = savedPlayer.CufferId;

						if (DisconnectedPlayers.ContainsKey(player.UserId)) DisconnectedPlayers.Remove(player.UserId);
						UnityEngine.Object.DestroyImmediate(savedPlayer.Player.GameObject);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				});
			});
		}

		public static void Left(NetworkConnection conn)
		{
			if (conn.identity == null || conn.identity.gameObject == null)
				return;

			Player player = Player.Get(conn.identity.gameObject);

			if (player == null || player.IsHost)
				return;

			var ev = new LeftEventArgs(player);

			Log.SendRaw($"Player {ev.Player.Nickname} ({ev.Player.UserId}) ({player?.Id}) disconnected", ConsoleColor.Green);

			Exiled.Events.Handlers.Player.OnLeft(ev);

			Player.IdsCache.Remove(player.Id);
			Player.UserIdsCache.Remove(player.UserId);
			Player.Dictionary.Remove(player.GameObject);
		}

		public static void Dispose(CustomNetworkManager cnm, NetworkConnection conn)
		{
			if (cnm._disconnectDrop)
			{
				NetworkIdentity identity = conn.identity;
				if (identity != null)
				{
					identity.GetComponent<PlayerStats>().HurtPlayer(new PlayerStats.HitInfo(-1f, "DISCONNECT", DamageTypes.Wall, 0), conn.identity.gameObject, true);
				}
			}
			NetworkServer.DestroyPlayerForConnection(conn);
			if (LogFilter.Debug)
			{
				Debug.Log("OnServerDisconnect: Client disconnected.");
			}
			Dissonance.Integrations.MirrorIgnorance.MirrorIgnoranceServer.ForceDisconnectClient(conn);
			conn.Disconnect();
			conn.Dispose();
		}

		public static IEnumerator<float> AhpDecay(PlayerStats playerStats)
		{
			while(playerStats.syncArtificialHealth > 0)
			{
				yield return Timing.WaitForSeconds(1f);
				playerStats.syncArtificialHealth -= playerStats.artificialHpDecay;
				if (playerStats.syncArtificialHealth < 0) playerStats.syncArtificialHealth = 0;
			}
		}
	}
}
