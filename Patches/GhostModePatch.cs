﻿// -----------------------------------------------------------------------
// <copyright file="GhostMode.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using CustomPlayerEffects;

using Exiled.API.Features;

using HarmonyLib;

using Mirror;

using UnityEngine;

using Scp096 = PlayableScps.Scp096;

namespace PlayerReconnect.Patches
{
    [HarmonyPatch(typeof(PlayerPositionManager), nameof(PlayerPositionManager.TransmitData)), HarmonyPriority(Priority.First)]
    class GhostMode
    {
        private static readonly Vector3 GhostPos = Vector3.up * 6000f;

        public static bool Prefix(PlayerPositionManager __instance)
        {
            try
            {
                if (++__instance._frame != __instance._syncFrequency)
                    return false;

                __instance._frame = 0;

                List<GameObject> players = PlayerManager.players;
                __instance._usedData = players.Count;

                if (__instance._receivedData == null
                    || __instance._receivedData.Length < __instance._usedData)
                {
                    __instance._receivedData = new PlayerPositionData[__instance._usedData * 2];
                }

                for (int index = 0; index < __instance._usedData; ++index)
                    __instance._receivedData[index] = new PlayerPositionData(ReferenceHub.GetHub(players[index]));

                if (__instance._transmitBuffer == null
                    || __instance._transmitBuffer.Length < __instance._usedData)
                {
                    __instance._transmitBuffer = new PlayerPositionData[__instance._usedData * 2];
                }

                foreach (GameObject gameObject in players)
                {
                    Player player = GetPlayerOrServer(gameObject);

                    if (player == null) continue;

                    Array.Copy(__instance._receivedData, __instance._transmitBuffer, __instance._usedData);

                    if (player.Role.Is939())
                    {
                        for (int index = 0; index < __instance._usedData; ++index)
                        {
                            if (__instance._transmitBuffer[index].position.y < 800f)
                            {
                                ReferenceHub hub2 = ReferenceHub.GetHub(__instance._transmitBuffer[index].playerID);

                                if (hub2.characterClassManager.CurRole.team != Team.SCP
                                    && hub2.characterClassManager.CurRole.team != Team.RIP
                                    && !hub2
                                        .GetComponent<Scp939_VisionController>()
                                        .CanSee(player.ReferenceHub.characterClassManager.Scp939))
                                {
                                    MakeGhost(index, __instance._transmitBuffer);
                                }
                            }
                        }
                    }
                    else if (player.Role != RoleType.Spectator && player.Role != RoleType.Scp079)
                    {
                        for (int index = 0; index < __instance._usedData; ++index)
                        {
                            PlayerPositionData ppd = __instance._transmitBuffer[index];
                            if (!ReferenceHub.TryGetHub(ppd.playerID, out var targetHub))
                                continue;

                            Player currentTarget = GetPlayerOrServer(targetHub.gameObject);
                            Scp096 scp096 = player.ReferenceHub.scpsController.CurrentScp as Scp096;

                            Vector3 vector3 = ppd.position - player.ReferenceHub.playerMovementSync.RealModelPosition;
                            if (Math.Abs(vector3.y) > 35f)
                            {
                                MakeGhost(index, __instance._transmitBuffer);
                            }
                            else
                            {
                                float sqrMagnitude = vector3.sqrMagnitude;
                                if (player.ReferenceHub.playerMovementSync.RealModelPosition.y < 800f)
                                {
                                    if (sqrMagnitude >= 1764f)
                                    {
                                        if (!(sqrMagnitude < 4225f))
                                        {
                                            MakeGhost(index, __instance._transmitBuffer);
                                            continue;
                                        }
                                        if (!(currentTarget.ReferenceHub.scpsController.CurrentScp is Scp096 scp) || !scp.EnragedOrEnraging)
                                        {
                                            MakeGhost(index, __instance._transmitBuffer);
                                            continue;
                                        }
                                    }
                                }
                                else if (sqrMagnitude >= 7225f)
                                {
                                    MakeGhost(index, __instance._transmitBuffer);
                                    continue;
                                }

                                if (scp096 != null
                                    && scp096.EnragedOrEnraging
                                    && !scp096.HasTarget(currentTarget.ReferenceHub)
                                    && currentTarget.Team != Team.SCP)
                                {
                                    MakeGhost(index, __instance._transmitBuffer);
                                }
                                else if (currentTarget.ReferenceHub.playerEffectsController.GetEffect<Scp268>().Enabled)
                                {
                                    bool flag2 = false;
                                    if (scp096 != null)
                                        flag2 = scp096.HasTarget(currentTarget.ReferenceHub);

                                    if (player.Role != RoleType.Scp079
                                        && player.Role != RoleType.Spectator
                                        && !flag2)
                                    {
                                        MakeGhost(index, __instance._transmitBuffer);
                                    }
                                }
                            }
                        }
                    }

                    for (var z = 0; z < __instance._usedData; z++)
                    {
                        var ppd = __instance._transmitBuffer[z];

                        if (player.Id == ppd.playerID)
                            continue;

                        if (ppd.position == GhostPos)
                            continue;

                        if (!ReferenceHub.TryGetHub(ppd.playerID, out var targetHub))
                            continue;

                        var target = GetPlayerOrServer(targetHub.gameObject);

                        if (target?.ReferenceHub == null)
                            continue;

                        if (target.IsInvisible || PlayerCannotSee(player, target.Id))
                        {
                            MakeGhost(z, __instance._transmitBuffer);
                        }
                        else if (player.Role == RoleType.Scp173
                            && ((!Exiled.Events.Events.Instance.Config.CanTutorialBlockScp173
                                    && target.Role == RoleType.Tutorial)
                                || Scp173.TurnedPlayers.Contains(target)))
                        {
                            RotatePlayer(z, __instance._transmitBuffer, FindLookRotation(player.Position, target.Position));
                        }
                    }

                    NetworkConnection networkConnection = player.ReferenceHub.characterClassManager.netIdentity.isLocalPlayer
                        ? NetworkServer.localConnection
                        : player.ReferenceHub.characterClassManager.netIdentity.connectionToClient;
                    if (__instance._usedData <= 20)
                    {
                        networkConnection.Send(
                            new PlayerPositionManager.PositionMessage(__instance._transmitBuffer, (byte)__instance._usedData, 0), 1);
                    }
                    else
                    {
                        byte part;
                        for (part = 0; part < __instance._usedData / 20; ++part)
                            networkConnection.Send(new PlayerPositionManager.PositionMessage(__instance._transmitBuffer, 20, part), 1);
                        byte count = (byte)(__instance._usedData % (part * 20));
                        if (count > 0)
                            networkConnection.Send(new PlayerPositionManager.PositionMessage(__instance._transmitBuffer, count, part), 1);
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.Error($"GhostMode error: {exception}");
                return true;
            }
        }

        private static Vector3 FindLookRotation(Vector3 player, Vector3 target) => (target - player).normalized;

        private static bool PlayerCannotSee(Player source, int playerId) => source.TargetGhostsHashSet.Contains(playerId) || source.TargetGhosts.Contains(playerId);

        private static void MakeGhost(int index, PlayerPositionData[] buff) => buff[index] = new PlayerPositionData(GhostPos, buff[index].rotation, buff[index].playerID);

        private static void RotatePlayer(int index, PlayerPositionData[] buff, Vector3 rotation) => buff[index]
            = new PlayerPositionData(buff[index].position, Quaternion.LookRotation(rotation).eulerAngles.y, buff[index].playerID);

        private static Player GetPlayerOrServer(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            var refHub = ReferenceHub.GetHub(gameObject);

            return refHub.isLocalPlayer ? Server.Host : Player.Get(gameObject);
        }
    }
}