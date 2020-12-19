using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events;
using HarmonyLib;

namespace PlayerReconnect
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance;

        public override string Name { get; } = "PlayerReconnect";
        public override string Author { get; } = "Steven4547466";
        public override Version Version { get; } = new Version(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new Version(2, 1, 19);
        public override string Prefix { get; } = "PlayerReconnect";

		public Handlers.Player player { get; set; }
		public Handlers.Server server { get; set; }

		private int harmonyPatches = 0;
        private Harmony HarmonyInstance { get; set; }

        public override void OnEnabled()
		{
			base.OnEnabled();
            Instance = this;
			Events.DisabledPatchesHashSet.Add(typeof(CustomNetworkManager).GetMethod(nameof(CustomNetworkManager.OnServerDisconnect)));
            Events.Instance.ReloadDisabledPatches();
			RegisterEvents();

			HarmonyInstance = new Harmony($"steven4547466.playerreconnect-{++harmonyPatches}");
            HarmonyInstance.PatchAll();
        }

		public override void OnDisabled()
		{
			base.OnDisabled();
            Instance = null;
			UnregisterEvents();
			HarmonyInstance.UnpatchAll();
        }

		public void RegisterEvents()
		{
			player = new Handlers.Player();
			server = new Handlers.Server();
			Exiled.Events.Handlers.Player.Joined += player.OnJoined;
			Exiled.Events.Handlers.Player.Hurting += player.OnHurting;
			Exiled.Events.Handlers.Server.RestartingRound += server.OnRestartingRound;
		}

		public void UnregisterEvents()
		{
			Exiled.Events.Handlers.Player.Joined -= player.OnJoined;
			Exiled.Events.Handlers.Player.Hurting -= player.OnHurting;
			Exiled.Events.Handlers.Server.RestartingRound -= server.OnRestartingRound;
			player = null;
			server = null;
		}
	}
}
