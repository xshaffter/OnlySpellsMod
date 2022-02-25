using HutongGames.PlayMaker;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace only_spells_mod
{
    public class BehaviourManager : MonoBehaviour
    {

        private const int APS = 120;
        private const int PiedraChamanModifierBase = 33;
        private const int PiedraChamanModifierNew = 28;
        private int refresh_count = 0;
        private int focus_refresh_count = 0;
        private const int REF_TIME = 1_000_000_000 / APS;
        private DateTime last_refresh = DateTime.Now;
        private DateTime focus_last_refresh = DateTime.Now;
        private double mpLoadout = 0;
        public void Update()
        {
            if (PlayerData.instance != null)
            {
                int nailStage = (PlayerData.instance.nailDamage - 5) / 4;
                OnlySpellsMod.Instance.Log(nailStage);
                switch (nailStage)
                {
                    case 0:
                        OnlySpellsMod.Instance.MaxPasiveSoul = OnlySpellsMod.MaxSoul / 3;
                        break;
                    case 1:
                        OnlySpellsMod.Instance.MaxPasiveSoul = OnlySpellsMod.MaxSoul * (2 / 3);
                        break;
                    case 2:
                        OnlySpellsMod.Instance.MaxPasiveSoul = OnlySpellsMod.MaxSoul;
                        break;
                    default:
                        OnlySpellsMod.Instance.MaxPasiveSoul = OnlySpellsMod.MaxSoul;
                        OnlySpellsMod.Instance.AllowVessels = true;
                        break;
                }
                if (PlayerData.instance.equippedCharm_6) // Furia
                {
                    if (PlayerData.instance.health == 1)
                    {
                        OnlySpellsMod.Instance.SpellDmgModifier += PiedraChamanModifierBase;
                    }
                }

                if (PlayerData.instance.equippedCharm_33) // tuerce hechizos
                {
                    OnlySpellsMod.Instance.SpellDmgModifier -= PiedraChamanModifierNew;
                }

                if (PlayerData.instance.equippedCharm_19) // Piedra chamán
                {
                    OnlySpellsMod.Instance.SpellDmgModifier += PiedraChamanModifierNew - PiedraChamanModifierBase;
                }

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
                    if (++refresh_count % 10 == 0 && PlayerData.instance.MPCharge <= OnlySpellsMod.Instance.MaxPasiveSoul)
                    {
                        int mpCharge = 1;
                        if (PlayerData.instance.equippedCharm_20)
                        {
                            mpLoadout += 0.5;
                        }

                        if (mpLoadout % 1 == 0 && mpLoadout != 0)
                        {
                            mpCharge += (int)mpLoadout;
                            mpLoadout -= (int)mpLoadout;
                        }
                        HeroController.instance.AddMPCharge(mpCharge);
                        refresh_count = 0;
                    }
                    last_refresh = now;
                }

                if (Input.GetKeyUp(KeyCode.Joystick1Button1))
                {
                    OnlySpellsMod.Instance.IsFocusing = false;
                }
                else if (Input.GetKeyDown(KeyCode.Joystick1Button1) && HeroController.instance.CanFocus())
                {
                    OnlySpellsMod.Instance.IsFocusing = true;
                }
            }
        }
    }
    public class OnlySpellsMod : Mod
    {
        public override string GetVersion() => "1.1.0";
        internal static OnlySpellsMod Instance;
        int old_value = 0;
        int reserve_old_value = 0;
        internal int MaxPasiveSoul = 33;
        internal const int MaxSoul = 100;
        internal bool IsFocusing = false;
        internal bool AllowVessels = false;
        internal int SpellDmgModifier = 80;
        internal BehaviourManager behaviour;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;

            var go = new GameObject();
            behaviour = go.AddComponent<BehaviourManager>();
            GameObject.DontDestroyOnLoad(go);

            ModHooks.Instance.SetPlayerIntHook += CheckFocus;
            ModHooks.Instance.HitInstanceHook += AttackHandle;
        }

        private HitInstance AttackHandle(Fsm owner, HitInstance hit)
        {
            if (hit.AttackType == AttackTypes.Spell)
            {
                int damage = hit.DamageDealt;

                double modifier = SpellDmgModifier / 100.0;
                int result = (int)(damage * modifier);
                hit.DamageDealt = result;
            }
            else if (hit.AttackType == AttackTypes.Nail)
            {
                hit.DamageDealt = 0;
            }
            return hit;
        }

        private void CheckFocus(string intName, int value)
        {
            int newValue = value;
            if ((intName == "MPCharge") && IsFocusing)
            {
                if (PlayerData.instance.maxMP >= value)
                {
                    var diff = value - old_value;
                    if (diff < 0)
                    {
                        newValue = old_value - diff;
                    }
                }
            }
            else if ((intName == "MPReserve") && IsFocusing)
            {
                if (PlayerData.instance.MPReserveMax >= value)
                {
                    var diff = value - reserve_old_value;
                    if (diff < 0)
                    {
                        newValue = reserve_old_value - diff;
                    }
                }
            }

            if (intName == "MPReserve")
            {
                reserve_old_value = newValue;
            }
            else if (intName == "MPCharge")
            {
                old_value = newValue;
            }
            PlayerData.instance.SetIntInternal(intName, newValue);
        }
    }
}