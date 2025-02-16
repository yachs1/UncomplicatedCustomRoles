﻿using Exiled.API.Features;
using System.Collections.Generic;
using System;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.Structures;
using Handler = UncomplicatedCustomRoles.Events.EventHandler;

using PlayerHandler = Exiled.Events.Handlers.Player;
using ServerHandler = Exiled.Events.Handlers.Server;

namespace UncomplicatedCustomRoles
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomRoles";
        public override string Prefix => "UncomplicatedCustomRoles";
        public override string Author => "FoxWorn3365, Dr.Agenda";
        public override Version Version { get; } = new(1, 3, 0);
        public override Version RequiredExiledVersion { get; } = new(8, 3, 5);
        public static Plugin Instance;
        internal Handler Handler;
        public static Dictionary<int, ICustomRole> CustomRoles;
        public static Dictionary<int, int> PlayerRegistry = new();
        public static Dictionary<int, int> RolesCount = new();
        public static List<int> RoleSpawnQueue = new();
        internal FileConfigs FileConfigs;
        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            CustomRoles = new();

            FileConfigs = new();

            ServerHandler.RoundStarted += Handler.OnRoundStarted;
            ServerHandler.RespawningTeam += Handler.OnRespawningTeam;
            PlayerHandler.Died += Handler.OnDied;
            PlayerHandler.Spawning += Handler.OnSpawning;
            PlayerHandler.Spawned += Handler.OnPlayerSpawned;
            PlayerHandler.Escaping += Handler.OnEscaping;

            foreach (ICustomRole CustomRole in Config.CustomRoles)
            {
                SpawnManager.RegisterCustomSubclass(CustomRole);
            }

            FileConfigs.Welcome();
            FileConfigs.LoadAll();

            base.OnEnabled();
        }
        public override void OnDisabled()
        {
            Instance = null;

            ServerHandler.RoundStarted -= Handler.OnRoundStarted;
            ServerHandler.RespawningTeam -= Handler.OnRespawningTeam;
            PlayerHandler.Died -= Handler.OnDied;
            PlayerHandler.Spawning -= Handler.OnSpawning;
            PlayerHandler.Spawned -= Handler.OnPlayerSpawned;
            PlayerHandler.Escaping -= Handler.OnEscaping;

            Handler = null;
            CustomRoles = null;

            base.OnDisabled();
        }
    }
}