using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.Events.EventArgs;
using EPlayer = Exiled.API.Features.Player;
using Exiled.API.Features;
using UnityEngine;
using MEC;

namespace PlayerReconnect.Handlers
{
	public class Player
	{
		public void OnJoined(JoinedEventArgs ev)
		{
			if (TrackingAndMethods.DisconnectedPlayers.ContainsKey(ev.Player.UserId))
			{
				var tuple = TrackingAndMethods.DisconnectedPlayers[ev.Player.UserId];
				tuple.Item1.Player.ClearInventory();
				tuple.Item1.Respawned = true;
				TrackingAndMethods.Left(tuple.Item3);
				TrackingAndMethods.Dispose(tuple.Item2, tuple.Item3);
			}

			if (TrackingAndMethods.Coroutines.ContainsKey(ev.Player.UserId))
			{
				foreach(CoroutineHandle coroutine in TrackingAndMethods.Coroutines[ev.Player.UserId])
					Timing.KillCoroutines(coroutine);
				TrackingAndMethods.Coroutines.Remove(ev.Player.UserId);
			}
			TrackingAndMethods.RespawnPlayer(ev.Player);
		}

		public void OnHurting(HurtingEventArgs ev)
		{
			if (TrackingAndMethods.DisconnectedPlayers.ContainsKey(ev.Target.UserId))
			{
				PlayerStats playerStats = TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.PlayerStats;
				if (playerStats.syncArtificialHealth > 0f)
				{
					float ahpAmount = ev.Amount * playerStats.artificialNormalRatio;
					float amount = ev.Amount - ahpAmount;
					playerStats.syncArtificialHealth -= ahpAmount;
					if (playerStats.syncArtificialHealth < 0f)
					{
						amount += Mathf.Abs(playerStats.syncArtificialHealth);
						playerStats.syncArtificialHealth = 0f;
					}
					playerStats.Health -= amount;
				} else
				{
					playerStats.Health -= ev.Amount;
				}

				if (playerStats.Health <= 0)
				{
					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Alive = false;
					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Player.DropItems();
					ev.Target.ReferenceHub.GetComponent<RagdollManager>().SpawnRagdoll(ev.Target.GameObject.transform.position, ev.Target.GameObject.transform.rotation,
						(ev.Target.ReferenceHub.playerMovementSync == null) ? Vector3.zero : ev.Target.ReferenceHub.playerMovementSync.PlayerVelocity,
						(int)ev.Target.ReferenceHub.characterClassManager.CurClass, ev.HitInformations, ev.Target.ReferenceHub.characterClassManager.CurRole.team > Team.SCP,
						ev.Target.GameObject.GetComponent<Dissonance.Integrations.MirrorIgnorance.MirrorIgnorancePlayer>().PlayerId,
						ev.Target.ReferenceHub.nicknameSync.DisplayName, ev.Target.ReferenceHub.queryProcessor.PlayerId);
					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Player.Role = RoleType.Spectator;
				}
			}
		}
	}
}
