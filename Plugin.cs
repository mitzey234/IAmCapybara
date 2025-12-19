using CommandSystem;
using HarmonyLib;
using RemoteAdmin;
using System;
using System.Linq;
using CentralAuth;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;
using ICommand = CommandSystem.ICommand;
using Logger = LabApi.Features.Console.Logger;
using Object = UnityEngine.Object;

namespace IAmCapybara
{
    public class IAmCapybara : Plugin
    {
        public override string Name => "IAmCapybara";
        public override string Description => "You cannot stop him";
        public override string Author => "Mitzey";
        public override LoadPriority Priority => LoadPriority.Highest;
        public override Version Version => new Version(2, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 4, 2);
        internal static IAmCapybara Instance { get; private set; }
        
        public Config Config { get; private set; } = null!;

        private Harmony Harmony { get; set; }
        
        public static void SetScale(Player player, Vector3 scale, bool visibleToPlayer)
        {
            try
            {
                player.ReferenceHub.transform.localScale = scale;
                new SyncedScaleMessages.ScaleMessage(scale, player.ReferenceHub).SendToHubsConditionally<SyncedScaleMessages.ScaleMessage>((Func<ReferenceHub, bool>) (n => (visibleToPlayer || n.authManager.UserId != player.UserId) && n.authManager.InstanceMode == ClientInstanceMode.ReadyClient));
                if (!visibleToPlayer) player.ReferenceHub.transform.localScale = Vector3.one;
                new SyncedScaleMessages.ScaleMessage(player.ReferenceHub.transform.localScale, player.ReferenceHub).SendToHubsConditionally<SyncedScaleMessages.ScaleMessage>((Func<ReferenceHub, bool>) (n => n.authManager.UserId == player.UserId && n.authManager.InstanceMode == ClientInstanceMode.ReadyClient));
            }
            catch (Exception exception)
            {
                Logger.Error($"Set scale error: {exception}");
            }
        }

        public static void Attack(Player target)
        {
            Scp956Pinata._instance.RpcAttack();
            Vector3 normalized = (target.Position - Scp956Pinata._instance._tr.position).normalized;
            target.ReferenceHub.playerStats.DealDamage(new Scp956DamageHandler(normalized));
        }

        public override void Enable()
        {
            Harmony = new Harmony(Name);
            Harmony.PatchAll();
            
            Instance = this;
            EventHandlers handlers = new EventHandlers();
            CustomHandlersManager.RegisterEventsHandler(handlers);
        }

        public override void Disable()
        {
            
        }
        
        public override void LoadConfigs()
        {
            base.LoadConfigs();
            Config = this.LoadConfig<Config>("config.yml");  
        }
        
        public static Player targetPlayer;

        public static bool spin = false;

        public static float speed = 2f;
        
        public static bool perspective = false;

        public static bool doesNotDiscriminate = false;
    }

    class CustomComponent : MonoBehaviour
    {
        float Timer = 0f;

        public static bool active = false;

        bool firstStart = false;

        private float rotation = 0f;

        private Vector3 temp = new Vector3(0f, 0.4f, 0f);

        public Scp956Pinata instance;

        public void Awake()
        {
            
        }

        public void Start()
        {

        }

        //Updates 45x a second
        public void Update()
        {
            Timer += Time.deltaTime;
            if (Timer <= 1f/45f) return;
            Timer = 0f;

            rotation += IAmCapybara.speed;
            if (rotation >= 360f) rotation = 0;

            if (instance == null) return;

            if ((IAmCapybara.targetPlayer == null || IAmCapybara.targetPlayer.Equals(null)) && !active) return;

            if (active && IAmCapybara.targetPlayer == null && Scp956Pinata.IsSpawned)
            {
                instance.Network_syncPos = new Vector3(0, 0, 0);
                instance._spawnPos = new Vector3(0, 0, 0);
                instance.Network_spawned = false;
                active = false;
                return;
            }
            else if (active && IAmCapybara.targetPlayer == null)
            {
                active = false;
                return;
            }


            if (IAmCapybara.targetPlayer != null)
            {
                if (IAmCapybara.targetPlayer.Equals(null))
                {
                    IAmCapybara.targetPlayer = null;
                    return;
                }
                if (!IAmCapybara.targetPlayer.IsAlive)
                {
                    IAmCapybara.targetPlayer = null;
                    return;
                }
                if (!active)
                {
                    active = true;
                    firstStart = true;
                }
            }
            else
            {
                return;
            }

            if (!active) return;

            if (firstStart && Scp956Pinata.IsSpawned)
            {
                firstStart = false;
                instance.Network_syncPos = new Vector3(0, 0, 0);
                instance._spawnPos = new Vector3(0, 0, 0);
                instance.Network_spawned = false;
                return;
            }
            else if (!Scp956Pinata.IsSpawned)
            {
                firstStart = false;
                instance.Network_carpincho = (byte)69;
                Scp956Pinata.IsSpawned = true;
            }
            Vector3 vector = IAmCapybara.targetPlayer.Position + (IAmCapybara.perspective ? temp : Vector3.zero);
            instance.Network_syncPos = vector;
            instance._spawnPos = vector;
            instance.Network_syncRot = IAmCapybara.spin ? rotation : IAmCapybara.targetPlayer.Camera.rotation.eulerAngles.y;
            instance.Network_spawned = true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class PluginCommand : ParentCommand
    {
        public override string Command => "iam";

        public override string[] Aliases => Array.Empty<string>();

        public override string Description => "Controls some funnies";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new CapybaraCommand());
            RegisterCommand(new FlyingCommand());
            RegisterCommand(new FakeAttackCommand());
            RegisterCommand(new AttackCommand());
            RegisterCommand(new SpinCommand());
            RegisterCommand(new PerspectiveCommand());
            RegisterCommand(new KillScpsCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }
                Logger.Info(8);
                response = "Plugin Menu:\n" +
                    "iam capybara - Makes you the capybara, or makes you not the capybara\n" +
                    "iam flying - Makes the capybara fly\n" +
                    "iam fake - Plays a fake attack noise.. Probably\n" +
                    "iam attack - attacks the closest player\n" +
                    "iam spin - Makes the capybara spin\n" +
                    "iam perspective - Shrinks your perspective to the small boi";
                return true;
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class CapybaraCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "Tis quite funny";

        string ICommand.Command { get; } = "capybara";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                if (Scp956Pinata._instance == null)
                {
                    response = "The pinata is missing from the scene!";
                    return false;
                }
                
                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    IAmCapybara.targetPlayer = null;
                    response = "You are no longer a capybara";
                    return true;
                }

                IAmCapybara.targetPlayer = Player.Get(sender);
                IAmCapybara.SetScale(IAmCapybara.targetPlayer, new Vector3(1f, 0.53f, 1f), IAmCapybara.perspective);
                
                foreach (var player in Player.GetAll())player.DisableEffect<Scp956Target>();
                
                response = "You are now a capybara";
                return true;
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class FlyingCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "flying";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    Scp956Pinata._instance.Network_flying = !Scp956Pinata._instance.Network_flying;
                    response = Scp956Pinata._instance.Network_flying ? "You're now flying!" : "You're no longer flying";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class FakeAttackCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "fake";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    Scp956Pinata._instance.RpcAttack();
                    response = "Spooky, musta been scary";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class AttackCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "attack";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    Player actualTarget = null;
                    foreach (Player p in Player.GetAll().Where(p => p.IsAlive && (IAmCapybara.doesNotDiscriminate ? p.IsSCP : !p.IsSCP) && !p.IsHost && p != IAmCapybara.targetPlayer))
                    {
                        if (actualTarget == null)
                        {
                            actualTarget = p;
                            continue;
                        }
                        if (actualTarget == p) continue;
                        if ((IAmCapybara.targetPlayer.Position - p.Position).sqrMagnitude < (IAmCapybara.targetPlayer.Position - actualTarget.Position).sqrMagnitude) actualTarget = p;
                    }

                    if (actualTarget == null)
                    {
                        response = "Could not find target!";
                        return false;
                    }

                    IAmCapybara.Attack(actualTarget);
                    response = "Ouch! That looked like it hurt..";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class SpinCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "SPEEN";

        string ICommand.Command { get; } = "spin";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    if (arguments.Count >= 1)
                    {
                        float speed;
                        try
                        {
                            speed = float.Parse(arguments.First());
                        }
                        catch
                        {
                            response = "Failed to parse argument";
                            return true;
                        }

                        IAmCapybara.speed = speed;
                        response = "Set speed to: " + speed;
                        return true;
                    }
                    IAmCapybara.spin = !IAmCapybara.spin;
                    response = IAmCapybara.spin ? "SPEEEEEEEN" : "oof..";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }
    
    [CommandHandler(typeof(PluginCommand))]
    class PerspectiveCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "Shrinks your perspective to the small boi";

        string ICommand.Command { get; } = "perspective";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    IAmCapybara.perspective = !IAmCapybara.perspective;
                    response = IAmCapybara.perspective ? "Smol" : "not so smol..";
                    if (!IAmCapybara.perspective) IAmCapybara.SetScale(IAmCapybara.targetPlayer, new Vector3(1f, 1f, 1f), true);
                    IAmCapybara.SetScale(IAmCapybara.targetPlayer, new Vector3(1f, 0.53f, 1f), IAmCapybara.perspective);
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }
    
    [CommandHandler(typeof(PluginCommand))]
    class KillScpsCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "Toggles whether or not your attack can target SCPs";

        string ICommand.Command { get; } = "scps";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!IAmCapybara.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (IAmCapybara.targetPlayer == target)
                {
                    IAmCapybara.doesNotDiscriminate = !IAmCapybara.doesNotDiscriminate;
                    response = IAmCapybara.doesNotDiscriminate ? "SCPs beware" : "SCPs can rest easy, for now...";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class ForcePinata : ICommand
    {
        public string Command  => "forcepinata";
        
        public string[] Aliases => new[] { "fp" };
        
        public string Description { get; }
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                Player target = Player.Get(sender);
                if (!target.RemoteAdminAccess)
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }
            }
            
            if (Scp956Pinata._instance == null)
            {
                var template = Resources.FindObjectsOfTypeAll<Scp956Pinata>()
                    .FirstOrDefault();

                if (template == null)
                {
                    response = "Could not find prefab";
                    return false;
                }

                var go = Object.Instantiate(template.gameObject);
                go.SetActive(true);
                NetworkServer.Spawn(go);
                response = "Spawned instance";
                return true;
            }
            else
            {
                response = "Instance already created";
                return false;
            }
        }
    }
    
    [HarmonyPatch(typeof(Scp956Pinata))]
    class Patches
    {
        [HarmonyPatch(nameof(Scp956Pinata.Awake))]
        [HarmonyPostfix]
        public static void Awake(Scp956Pinata __instance)
        {
            Logger.Warn("Attaching to pinata instance");
            try
            {
                __instance.gameObject.AddComponent<CustomComponent>().instance = __instance;
                Logger.Warn("Added Component to pinata instance");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        [HarmonyPatch(nameof(Scp956Pinata.UpdateAi))]
        [HarmonyPrefix]
        public static bool UpdateAi(Scp956Pinata __instance)
        {
            if (IAmCapybara.targetPlayer != null || CustomComponent.active) return false;
            return true;
        }
    }
}