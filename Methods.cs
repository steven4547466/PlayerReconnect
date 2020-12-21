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
			if (!savedPlayer.Alive || playerStats.Health <= 0)
			{
				DisconnectedPlayers.Remove(player.UserId);
				return;
			}
			Log.Info("Respawned");
			Timing.CallDelayed(0.5f, () =>
			{
				Log.Info("Spawning as " + savedPlayer.Role);
				player.SetRole(savedPlayer.Role);
				Timing.CallDelayed(1f, () =>
				{
					try
					{
						player.ClearInventory();
						foreach (KeyValuePair<AmmoType, uint> ammoPair in savedPlayer.Ammo)
						{
							player.Ammo[(int)ammoPair.Key] = ammoPair.Value;
						}

						for (int i = 0; i < savedPlayer.Inventory.Count; i++)
						{
							Inventory.SyncItemInfo syncItemInfo = savedPlayer.Inventory[i];
							player.AddItem(syncItemInfo);
						}
						player.Position = savedPlayer.Position;
						player.Rotation = savedPlayer.Rotation;
						player.Rotations = savedPlayer.Rotations;
						if (savedPlayer.Role == RoleType.Scp079)
						{
							Scp079PlayerScript script = player.ReferenceHub.GetComponent<Scp079PlayerScript>();
							player.Camera = savedPlayer.Camera;
							script.ForceLevel(savedPlayer.Scp079PlayerScript.Lvl, false);
							script.NetworkcurExp = savedPlayer.Scp079PlayerScript.NetworkcurExp;
							script.NetworkmaxMana = savedPlayer.Scp079PlayerScript.NetworkmaxMana;
							script.NetworkcurMana = savedPlayer.Scp079PlayerScript.NetworkcurMana;
							//script.lockedDoors = savedPlayer.Scp079PlayerScript.lockedDoors;
						}

						player.MaxHealth = playerStats.maxHP;
						player.MaxAdrenalineHealth = playerStats.maxArtificialHealth;
						player.Health = playerStats.Health;
						player.AdrenalineHealth = playerStats.unsyncedArtificialHealth;

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

		public static void Left(NetworkConnection conn, bool respawned = false)
		{
			if (conn.identity == null || conn.identity.gameObject == null)
				return;

			Player player = Player.Get(conn.identity.gameObject);

			if (player == null || player.IsHost)
				return;

			if (!respawned)
			{
				var ev = new LeftEventArgs(player);

				Log.SendRaw($"Player {ev.Player.Nickname} ({ev.Player.UserId}) ({player?.Id}) disconnected", ConsoleColor.Green);

				Exiled.Events.Handlers.Player.OnLeft(ev);
			}

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
					PlayerStats.HitInfo info = new PlayerStats.HitInfo(-1f, "DISCONNECT", DamageTypes.Wall, 0);
					identity.GetComponent<PlayerStats>().HurtPlayer(info, conn.identity.gameObject, true);
					GameObject go = identity.gameObject;
					if (go == null) return;
					ReferenceHub referenceHub = ReferenceHub.GetHub(go);
					PlayerMovementSync playerMovementSync = referenceHub.playerMovementSync;
					CharacterClassManager ccm = referenceHub.characterClassManager;
					if (DisconnectedPlayers.ContainsKey(ccm.UserId) && !DisconnectedPlayers[ccm.UserId].Item1.Respawned)
					{
						DisconnectedPlayers[ccm.UserId].Item1.Player.DropItems();
						go.GetComponent<RagdollManager>().SpawnRagdoll(go.transform.position, go.transform.rotation,
							(playerMovementSync == null) ? Vector3.zero : playerMovementSync.PlayerVelocity, (int)ccm.CurClass,
							info, ccm.CurRole.team > Team.SCP, go.GetComponent<Dissonance.Integrations.MirrorIgnorance.MirrorIgnorancePlayer>().PlayerId,
							referenceHub.nicknameSync.DisplayName, referenceHub.queryProcessor.PlayerId);
					}
					Timing.CallDelayed(0.5f, () =>
					{
						if (DisconnectedPlayers.ContainsKey(ccm.UserId)) DisconnectedPlayers.Remove(ccm.UserId);
					});
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
			while (playerStats.syncArtificialHealth > 0)
			{
				yield return Timing.WaitForSeconds(1f);
				playerStats.syncArtificialHealth -= playerStats.artificialHpDecay;
				if (playerStats.syncArtificialHealth < 0) playerStats.syncArtificialHealth = 0;
			}
		}
	}
}
