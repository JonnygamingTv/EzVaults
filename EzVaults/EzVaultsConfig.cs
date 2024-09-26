using EzVaults.Enums;
using Rocket.API;
using System.Collections.Generic;

namespace EzVaults
{
    public class EzVaultsConfig : IRocketPluginConfiguration
    {
        public bool CloseVaultOnPickup;
        public bool CancelPickup;
        public bool PickupActivateEvents;
        public bool ManualHandler;
        public bool SeperateVaults;
        public bool ignoreCase;
        public int AllowVehicle;
        public float CacheTime;
        public EDatabase Database;
        public UnityEngine.Color Color;
        //public List<string> VaultNames { get; set; }
        //public List<string> VaultPerms { get; set; }
        //public List<List<byte>> Vaults { get; set; }
        public List<Vaulter> Vaulter {get;set;}
        public void LoadDefaults()
        {
            CloseVaultOnPickup = true; CancelPickup = false; PickupActivateEvents = true; ManualHandler = false;SeperateVaults = true; ignoreCase = true; AllowVehicle = 0;
            CacheTime = 0f;
            Database = EDatabase.FILE;
            Color = UnityEngine.Color.red;
            // VaultNames = new List<string>() { "small","medium","large" };
            //VaultPerms = new List<string>() { "vault.small","vault.medium","vault.large" };
            //Vaults = new List<List<byte>>() { new List<byte>(){ 5, 5 }, new List<byte>() { 5, 9 }, new List<byte>() { 9, 18 } };
            Vaulter = new List<Vaulter>() { new Vaulter("Large","Vault.Large",10,10), new Vaulter("Small", "Vault.Small", 5, 5) };
        }
    }
}
