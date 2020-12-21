using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.Events.EventArgs;
using MEC;

namespace PlayerReconnect.Handlers
{
	public class Server
	{
		public void OnRestartingRound()
		{
			TrackingAndMethods.DisconnectedPlayers.Clear();
			foreach (List<CoroutineHandle> coroutines in TrackingAndMethods.Coroutines.Values)
			{
				foreach (CoroutineHandle coroutine in coroutines)
					Timing.KillCoroutines(coroutine);
			}
			TrackingAndMethods.Coroutines.Clear();
		}
	}
}
