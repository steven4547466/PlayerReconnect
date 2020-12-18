using Exiled.API.Features;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;

namespace PlayerReconnect
{
	public static partial class TrackingAndMethods
	{
		public static Dictionary<string, Tuple<ReconnectData, CustomNetworkManager, NetworkConnection>> DisconnectedPlayers = new Dictionary<string, Tuple<ReconnectData, CustomNetworkManager, NetworkConnection>>();

		public static Dictionary<string, List<CoroutineHandle>> Coroutines = new Dictionary<string, List<CoroutineHandle>>();
	}
}
