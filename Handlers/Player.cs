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

				if (playerStats.Health <= 1f)
				{
					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Alive = false;
					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Player.DropItems();

					if (ev.Target.ReferenceHub.characterClassManager.Classes.CheckBounds(ev.Target.Role) &&
						(!playerStats._pocketCleanup || ev.DamageType != DamageTypes.Pocket) && ev.DamageType != DamageTypes.RagdollLess)
					{
						ev.Target.ReferenceHub.GetComponent<RagdollManager>().SpawnRagdoll(ev.Target.GameObject.transform.position, ev.Target.GameObject.transform.rotation,
							(ev.Target.ReferenceHub.playerMovementSync == null) ? Vector3.zero : ev.Target.ReferenceHub.playerMovementSync.PlayerVelocity,
							(int)ev.Target.ReferenceHub.characterClassManager.CurClass, ev.HitInformations, ev.Target.ReferenceHub.characterClassManager.CurRole.team > Team.SCP,
							ev.Target.GameObject.GetComponent<Dissonance.Integrations.MirrorIgnorance.MirrorIgnorancePlayer>().PlayerId,
							ev.Target.ReferenceHub.nicknameSync.DisplayName, ev.Target.ReferenceHub.queryProcessor.PlayerId);
					}

					TrackingAndMethods.DisconnectedPlayers[ev.Target.UserId].Item1.Player.Role = RoleType.Spectator;
				}
			}
		}
	}
}
