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

		public void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
		{
			if (TrackingAndMethods.DisconnectedPlayers.Any(p => p.Value.Item1.DissonanceId == ev.DissonanceId)) ev.IsAllowed = false;
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

			}
		}
	}
}
