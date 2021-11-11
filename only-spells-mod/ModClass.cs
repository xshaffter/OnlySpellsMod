using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace only_spells_mod
{
    internal class BehaviourManager : MonoBehaviour
    {
        private const int APS = 120;
        private int refresh_count = 0;
        private int focus_refresh_count = 0;
        private const int REF_TIME = 1_000_000_000 / APS;
        private DateTime last_refresh = DateTime.Now;
        private DateTime focus_last_refresh = DateTime.Now;
        internal bool IsFocusing = false;
        public void Update()
        {

            if (PlayerData.instance != null)
            {
                if (PlayerData.instance.fireballLevel == 0)
                {
                    PlayerData.instance.fireballLevel = 1;
                    PlayerData.instance.hasSpell = true;
                }
            }

            if (HeroController.instance != null)
            {
                var now = DateTime.Now;
                if ((now.Ticks - last_refresh.Ticks) * 100 >= REF_TIME)
                {
                    if (++refresh_count % 10 == 0)
                    {
                        HeroController.instance.AddMPCharge(1);
                        refresh_count = 0;
                    }
                    last_refresh = now;
                }

                if (Input.GetKeyUp(KeyCode.Joystick1Button1))
                {
                    IsFocusing = false;
                } 
                else if (Input.GetKeyDown(KeyCode.Joystick1Button1) && HeroController.instance.CanFocus())
                {
                    IsFocusing = true;
                }
            }
        }
    }
    public class OnlySpellsMod : Mod
    {
        public override string GetVersion() => "zote spells";
        internal static OnlySpellsMod Instance;
        internal BehaviourManager behaviour;
        int old_value = 0;
        int reserve_old_value = 0;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;

            var go = new GameObject();
            behaviour = go.AddComponent<BehaviourManager>();
            GameObject.DontDestroyOnLoad(go);
            ModHooks.Instance.SetPlayerIntHook += CheckFocus;
        }

        private void CheckFocus(string intName, int value)
        {
            if ((intName == "MPCharge") && behaviour.IsFocusing)
            {
                var diff = value - old_value;
                if (diff < 0)
                {
                    PlayerData.instance.SetIntInternal(intName, old_value - (diff * 2));
                }

                old_value = value;
            }
            else if ((intName == "MPReserve") && behaviour.IsFocusing)
            {
                var diff = value - reserve_old_value;
                if (diff < 0)
                {
                    PlayerData.instance.SetIntInternal(intName, reserve_old_value - (diff * 2));
                }

                old_value = value;
            }
            else if (intName == "MPReserve")
            {
                PlayerData.instance.SetIntInternal(intName, value);
                reserve_old_value = value;
            }
            else if (intName == "MPCharge")
            {
                PlayerData.instance.SetIntInternal(intName, value);
                old_value = value;
            }
            else
            {
                PlayerData.instance.SetIntInternal(intName, value);
            }
        }
    }
}