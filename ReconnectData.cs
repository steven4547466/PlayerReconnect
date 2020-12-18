using Exiled.API.Enums;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlayerReconnect
{
	public class ReconnectData
	{
		public Player Player;
		public RoleType Role;
		public PlayerStats PlayerStats;
		public List<Inventory.SyncItemInfo> Inventory;
		public int CurItemIndex;
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector2 Rotations;
		public Camera079 Camera;
		public string DisplayNickname;
		public int CufferId;
		public string CustomPlayerInfo;
		public Dictionary<AmmoType, uint> Ammo;
		public string DissonanceId;

		public ReconnectData(Player player)
		{
			Player = player;
			Role = player.Role;
			PlayerStats savedStats = player.GameObject.GetComponent<PlayerStats>();
			int maxHp = savedStats.maxHP;
			int maxAhp = savedStats.maxArtificialHealth;
			float hp = savedStats.Health;
			float ahp = savedStats.syncArtificialHealth;
			UnityEngine.Object.DestroyImmediate(savedStats);

			PlayerStats = player.GameObject.AddComponent<PlayerStats>();
			PlayerStats.maxHP = maxHp;
			PlayerStats.maxArtificialHealth = maxAhp;
			PlayerStats.Health = hp;
			PlayerStats.syncArtificialHealth = ahp;
				
			Inventory = new List<Inventory.SyncItemInfo>();
			foreach (Inventory.SyncItemInfo syncItemInfo in player.Inventory.items)
			{
				Inventory.Add(new Inventory.SyncItemInfo 
				{ 
					id=syncItemInfo.id, durability=syncItemInfo.durability, modBarrel=syncItemInfo.modBarrel, 
					modOther=syncItemInfo.modOther, modSight=syncItemInfo.modSight, uniq=syncItemInfo.uniq
				});
			}
			CurItemIndex = player.CurrentItemIndex;
			Position = player.Position;
			Rotation = player.Rotation;
			Rotations = player.Rotations;
			Camera = player.Camera;
			DisplayNickname = player.DisplayNickname;
			CufferId = player.CufferId;
			CustomPlayerInfo = player.CustomPlayerInfo;
			Ammo = new Dictionary<AmmoType, uint>();
			foreach(AmmoType ammo in Enum.GetValues(typeof(AmmoType)))
			{
				Ammo.Add(ammo, player.Ammo[(int)ammo]);
				Log.Info(ammo + " " + player.Ammo[(int)ammo]);
			}
			DissonanceId = player.GameObject.GetComponent<Dissonance.Integrations.MirrorIgnorance.MirrorIgnorancePlayer>().PlayerId;
		}
	}
}
