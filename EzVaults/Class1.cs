using EzVaults.Enums;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EzVaults
{
    public class EzVaults : RocketPlugin<EzVaultsConfig>
    {
        public static Dictionary<UnturnedPlayer, bool> vaultBkp = new Dictionary<UnturnedPlayer, bool>();
        public static Dictionary<UnturnedPlayer, int> vaultCurrent = new Dictionary<UnturnedPlayer, int>();
        public static Dictionary<UnturnedPlayer, Items> VaultItems = new Dictionary<UnturnedPlayer, Items>();
        public static Dictionary<UnturnedPlayer, Items> VaultItemCache = new Dictionary<UnturnedPlayer, Items>();
        public static Dictionary<UnturnedPlayer, TimerEventHook> PlayTimers = new Dictionary<UnturnedPlayer, TimerEventHook>();
        public static Dictionary<UnturnedPlayer, Items> VaultCache = new Dictionary<UnturnedPlayer, Items>();
        //public static List<Rocket.API.IRocketCommand> RCooldowns = new List<Rocket.API.IRocketCommand>();
        public static EzVaults Instance { get; private set; }
        public string dir;
        protected override void Load()
        {
            Instance = this;
            dir = Directory;
            if (!File.Exists(Path.Combine(Directory, "vaults")))
                System.IO.Directory.CreateDirectory(Path.Combine(Directory, "vaults"));
            for (int i = 0; i < Instance.Configuration.Instance.Vaulter.Count; i++)
            {
                if (!File.Exists(Path.Combine(Directory, "vaults", Instance.Configuration.Instance.Vaulter[i].Name)))
                    System.IO.Directory.CreateDirectory(Path.Combine(Directory, "vaults", Instance.Configuration.Instance.Vaulter[i].Name));
            }
            UnturnedPlayerEvents.OnPlayerUpdateGesture += PlayerUpdateGesture;
            Rocket.Unturned.U.Events.OnPlayerDisconnected += DisConn;
            UnturnedPlayerEvents.OnPlayerDeath += Playerded;
            ItemManager.onTakeItemRequested += TIRH;
            if (Configuration.Instance.ManualHandler)
            {
                UnturnedPlayerEvents.OnPlayerInventoryAdded += PIA;
                UnturnedPlayerEvents.OnPlayerInventoryRemoved += PIR;
#if DEBUG
            UnturnedPlayerEvents.OnPlayerInventoryUpdated += PIU;
#endif
            }
            Rocket.Core.Logging.Logger.Log("Loaded Vaults: "+dir);
        }
        protected override void Unload()
        {
            Rocket.Core.Logging.Logger.Log("Bye then");
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= PlayerUpdateGesture;
            Rocket.Unturned.U.Events.OnPlayerDisconnected -= DisConn;
            UnturnedPlayerEvents.OnPlayerDeath -= Playerded;
            ItemManager.onTakeItemRequested -= TIRH;
            UnturnedPlayerEvents.OnPlayerInventoryAdded -= PIA;
            UnturnedPlayerEvents.OnPlayerInventoryRemoved -= PIR;
#if DEBUG
            UnturnedPlayerEvents.OnPlayerInventoryUpdated -= PIU;
#endif
            foreach (UnturnedPlayer player in vaultCurrent.Keys)
                SavePlayer(player);
        }
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {$"{EResponse.IN_VEHICLE}", "Accessing Vault while in a vehicle is not allowed!"},
            {$"{EResponse.VAULT_NO_PERMISSION}", "You don't have permission to {0} vault!"},
            {$"{EResponse.NO_PERMISSION}", "You don't have permission to any vault!"},
            {$"{EResponse.VAULT_NOT_FOUND}", "Vault not found!"},
            {$"{EResponse.VAULTS}", "Vaults ({0}): {1}"},
        };
        private async void Playerded(UnturnedPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            await System.Threading.Tasks.Task.Run(() => SavePlayer(player));
        }
        private async void DisConn(UnturnedPlayer player) { await System.Threading.Tasks.Task.Run(()=> SavePlayer(player,true)); }
        async void PlayerUpdateGesture(UnturnedPlayer player, UnturnedPlayerEvents.PlayerGesture gesture)
        {
            if (gesture.GetHashCode() !=1)
            {
                await System.Threading.Tasks.Task.Run(() => SavePlayer(player));
            }
        }

        public static void LoadVault(UnturnedPlayer Player, int x, Rocket.API.IRocketCommand c)
        {
            if (x != -1)
            {
                string SVault = Instance.Configuration.Instance.Vaulter[x].Name;
                Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() => Rocket.Unturned.Chat.UnturnedChat.Say(Player, "Opening vault: " + SVault, EzVaults.Instance.Configuration.Instance.Color, true));
                Items vaultItems;
                if(VaultCache.TryGetValue(Player, out vaultItems)){
                    if (Instance.Configuration.Instance.CacheTime>0 && PlayTimers.TryGetValue(Player, out TimerEventHook _g) && _g != null) _g.CancelTimer();
#if DEBUG
                Rocket.Core.Logging.Logger.Log("Cache..");
#endif
                }else{
#if DEBUG
                    Rocket.Core.Logging.Logger.Log("Taking from file..");
#endif
                    vaultItems = new Items((byte)(Player.IsInVehicle ? Instance.Configuration.Instance.AllowVehicle : 7));
                    vaultItems.resize(Instance.Configuration.Instance.Vaulter[x].Width, Instance.Configuration.Instance.Vaulter[x].Height);
                    //Items vaultItemCache = new Items((byte)(Player.IsInVehicle ? Instance.Configuration.Instance.AllowVehicle : 7));
                    //vaultItemCache.resize(Instance.Configuration.Instance.Vaulter[x].Width, Instance.Configuration.Instance.Vaulter[x].Height);
                    if (File.Exists(Path.Combine(Instance.dir, "vaults", SVault, Player.Id)))
                    {
#if DEBUG
                        try
                        {
#endif
                            string content = File.ReadAllText(Path.Combine(Instance.dir, "vaults", SVault, Player.Id));
                            string[] cont = content.Split(',');
                            for (int i = 0; i < cont.Length; i++)
                            {
                                try
                                {
                                    string[] info = cont[i].Split('.');
                                    if (ushort.TryParse(info[0], out ushort s))
                                    {
                                        Item ite = new Item(s, false);
                                        if (info.Length > 4)
                                        {
                                            ite.amount = byte.Parse(info[4]);
                                            ite.durability = byte.Parse(info[5]);
                                            ite.quality = byte.Parse(info[6]);
                                            if (info.Length > 7)
                                            {
                                                ite.metadata = new byte[info.Length - 7];
                                                for (int ii = 7; ii < info.Length; ii++) { try { ite.metadata[ii - 7] = byte.Parse(info[ii]); /*ite.metadata.SetValue(byte.Parse(info[ii]), ii - 7);*/ } catch (System.Exception e) { Rocket.Core.Logging.Logger.Log(e.StackTrace + "\nLength: " + ite.metadata.Length.ToString() + "\nFixedSize: " + (ite.metadata.IsFixedSize ? "true" : "false")); } }
                                            }
                                        }
                                        byte X = byte.Parse(info[1]);
                                        byte Y = byte.Parse(info[2]);
                                        if (X > Instance.Configuration.Instance.Vaulter[x].Width || Y > Instance.Configuration.Instance.Vaulter[x].Height)
                                        {
                                            byte px;
                                            byte py;
                                            byte rot;
                                            bool failed = !vaultItems.tryFindSpace(X, Y, out px, out py, out rot);
                                            if (!failed) { vaultItems.addItem(px, py, rot, ite); }
                                            else
                                            {
                                            Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() => ItemManager.dropItem(ite, Player.Position, true, true, true));
                                            }
                                        }
                                        else
                                        {
                                            vaultItems.addItem(X, Y, byte.Parse(info[3]), ite);
                                            //vaultItemCache.addItem(X, Y, byte.Parse(info[3]), ite);
                                        }
                                    }
                                }
                                catch (System.Exception e) { Rocket.Core.Logging.Logger.Log(cont[i]+"\n"+e.StackTrace); }
                            }
#if DEBUG
                        }
                        catch (System.Exception e) { Rocket.Core.Logging.Logger.Log(SVault+"\n"+e.StackTrace);
                            Rocket.Unturned.Chat.UnturnedChat.Say(Player, "!! There was a problem opening vault " + SVault, EzVaults.Instance.Configuration.Instance.Color, true);
                        }
#endif
                    }
                    if (Instance.Configuration.Instance.ManualHandler)
                    {
                        vaultBkp.Add(Player, !Instance.Configuration.Instance.PickupActivateEvents);
                        int ps = VaultItemCache.Count;
                        Items _vaultItems = vaultItems;
                        VaultItemCache.Add(Player, _vaultItems);
                        vaultItems.onItemAdded = (byte page, byte index, ItemJar jar) =>
                        {
                            Rocket.Core.Logging.Logger.Log(jar.interactableItem.name+" item added to "+Player.DisplayName+"'s vault: "+page+" / "+index);
                            VaultItemCache[Player].addItem(jar.x,jar.y,jar.rot,jar.item);
                        };
                        vaultItems.onItemRemoved = (byte page, byte index, ItemJar jar) =>
                        {
                            Rocket.Core.Logging.Logger.Log(jar.interactableItem.name + " item removed from " + Player.DisplayName + "'s vault: " + page + " / " + index);
                            VaultItemCache[Player].removeItem(index);
                        };
                        vaultItems.onItemUpdated = (byte page, byte index, ItemJar jar) =>
                        {
                            Rocket.Core.Logging.Logger.Log(jar.interactableItem.name + " item updated in " + Player.DisplayName + "'s vault: " + page + " / " + index);
                            //VaultItemCache[ps].items[index] = jar;
                        };
                        vaultItems.onStateUpdated += OnVaultStorageUpdated(Player, SVault, x, vaultItems);
                    }
                    VaultItems.Add(Player, vaultItems);
                    vaultCurrent.Add(Player, x);
                    //RCooldowns.Add(c);
                    if (Instance.Configuration.Instance.CacheTime > 0) PlayTimers.Add(Player, new TimerEventHook());
                }
                Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() =>
                {
                    Player.Player.inventory.updateItems((byte)(Player.IsInVehicle ? Instance.Configuration.Instance.AllowVehicle : 7), vaultItems);
                    Player.Player.inventory.sendStorage();
                });
            }
            else
            {
                Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() => Rocket.Unturned.Chat.UnturnedChat.Say(Player, "Invalid vault.", EzVaults.Instance.Configuration.Instance.Color, true));
            }
            Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() => Player.Player.equipment.dequip());
        }
        public void SavePlayer(UnturnedPlayer P, bool a = false)
        {
            Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() => P.Player.equipment.dequip());
            if (VaultItems.TryGetValue(P, out Items _Items))
            {
                string res="";
                for (byte i = 0; i < _Items.getItemCount(); i++)
                {
                    ItemJar it = _Items.getItem(i);
                    res += (i==0?"":",")+it.item.id+"."+ it.x+"."+ it.y+"."+ it.rot+"."+it.item.amount+"."+it.item.durability+"."+it.item.quality;
                    for (int ii = 0; ii < it.item.metadata.Length; ii++) { res += "."+it.item.metadata[ii]; }
                }
                try{File.WriteAllText(Path.Combine(Directory, "vaults", EzVaults.Instance.Configuration.Instance.Vaulter[vaultCurrent[P]].Name, P.Id), res);}catch(System.Exception e){ Rocket.Core.Logging.Logger.Log(vaultCurrent[P].ToString()+": "+P.DisplayName+"\n"); Rocket.Core.Logging.Logger.LogError(e.StackTrace);}
                Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread(() =>
                {
                    P.Player.inventory.updateItems((byte)(P.IsInVehicle ? Instance.Configuration.Instance.AllowVehicle : 7), null);
                    P.Player.inventory.sendStorage();
                    P.Inventory.closeStorageAndNotifyClient();
                });
                if (Configuration.Instance.CacheTime==0)
                {
                    vaultCurrent.Remove(P);
                    VaultItems.Remove(P);
                    if (Configuration.Instance.ManualHandler)
                    {
                        VaultItemCache.Remove(P);
                        vaultBkp.Remove(P);
                    }
                }
                else if (a) {
                    vaultCurrent.Remove(P);
                    VaultItems.Remove(P);
                    PlayTimers.Remove(P);
                    if (Configuration.Instance.ManualHandler)
                    {
                        VaultItemCache.Remove(P);
                        vaultBkp.Remove(P);
                    }
                }
                else if(PlayTimers.TryGetValue(P, out TimerEventHook timer) && timer !=null)
                {
                    PlayTimers[P].SetTimer(Configuration.Instance.CacheTime);
                    //PlayTimers[x].OnTimerTriggered.Invoke(FinishVault(x));
                    PlayTimers[P].OnTimerTriggered.AddListener(FinishVault(P));
                }
            }
        }
        private UnityEngine.Events.UnityAction FinishVault(UnturnedPlayer P) {
            vaultCurrent.Remove(P);
            VaultItems.Remove(P);
            if (Configuration.Instance.ManualHandler)
            {
                VaultItemCache.Remove(P);
                vaultBkp.Remove(P);
            }
            PlayTimers.Remove(P);
            return null;
        }
        private int FV2(int x)
        {
            return 1;
        }
        static SDG.Unturned.StateUpdated OnVaultStorageUpdated(UnturnedPlayer P, string Sv, int X, Items Vi)
        {
#if DEBUG
            int x = vaultOwners.IndexOf(P);
            if(x!=-1)Rocket.Core.Logging.Logger.Log(P.DisplayName + ":" + Sv+"; "+Vi.items.Count+" / "+VaultItems[x].items.Count);
#endif
            /*int x = vaultOwners.IndexOf(P);
            if (x != -1)
            {
                VaultItems[x] = Vi;
            }*/
                return null;
        }
        void PIA(UnturnedPlayer P, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar I)
        {
#if DEBUG
            Rocket.Core.Logging.Logger.Log(P.DisplayName + ":" + I.item.id + ", added " + inventoryIndex+" to "+inventoryGroup);
#endif
            if (VaultItems.TryGetValue(P, out Items items) && inventoryGroup == Rocket.Unturned.Enumerations.InventoryGroup.Storage && vaultBkp[P])
            {
                items.items.Add(I);
            }
        }
        void PIR(UnturnedPlayer P, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar I)
        {
#if DEBUG
            Rocket.Core.Logging.Logger.Log(P.DisplayName + ":" + I.item.id + ", removed " + inventoryIndex + " from " + inventoryGroup);
#endif
            if (VaultItems.TryGetValue(P, out Items items) && inventoryGroup == Rocket.Unturned.Enumerations.InventoryGroup.Storage && vaultBkp[P])
            {
                items.removeItem(inventoryIndex);
            }
        }
        void PIU(UnturnedPlayer P, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar I)
        {
            Rocket.Core.Logging.Logger.Log(P.DisplayName + ":" + I.item.id);
            Rocket.Core.Logging.Logger.Log(inventoryGroup+": "+inventoryIndex);
            string bonus = "";if (VaultItems.TryGetValue(P, out Items items)) bonus = items.items.Count.ToString();
            Rocket.Core.Logging.Logger.Log(", "+bonus);
        }
        void TIRH(Player player, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            UnturnedPlayer P = UnturnedPlayer.FromPlayer(player);
            if (vaultCurrent.ContainsKey(P))
            {
                if(Configuration.Instance.PickupActivateEvents) vaultBkp[P] = true;
                if (Configuration.Instance.CancelPickup) shouldAllow = false;
                if (Configuration.Instance.CloseVaultOnPickup) P.Inventory.closeStorageAndNotifyClient();
            }
        }
    }
}