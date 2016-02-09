﻿using System;
using System.Linq;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using SharpDX;
using Color = System.Drawing.Color;
namespace SamRiven2
{
    public class Program
    {
        private static AIHeroClient _Player
        {
            get { return Player.Instance; }
        }
        private static Item Hydra
        {
            get { return ItemManager.Hydra; }
        }
        private static Item Titan
        {
            get { return ItemManager.Titanic; }
        }
        private static bool CanCastHydra()
        {
            return Hydra != null && Hydra.IsReady();
        }

        private static Orbwalker.ActiveModes Mode
        {
            get { return Orbwalker.ActiveModesFlags; }
        }
        private static bool CanCastTitan()
        {
            return Titan != null && Titan.IsReady();
        }
        public static string AssemblyName = "SamRiven";
        public static Spell.Active Q = new Spell.Active(SpellSlot.Q, 275);
        public static Spell.Active E = new Spell.Active(SpellSlot.E, 325);
        public static Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600, 45);
        public static Spell.Active W = new Spell.Active(SpellSlot.W, (uint)(70 + Player.Instance.BoundingRadius + 120));
        private static string R1 = "RivenFengShuiEngine";
        static void Main(string[] args)
        {
            Hacks.RenderWatermark = false;
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (_Player.ChampionName != "Riven") return;
            Config.Init();
            ItemManager.Init();

            //var Orbwalk = new Orb.Orbwalking.Orbwalker();
            //SamOrb.Init();
            //Game.OnUpdate += Game_OnUpdate;
            Game.OnUpdate += Game_OnUpdate2;
            //Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnSpellCast += AfterAttack;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            Utils.Debug("Pre attack");
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            var pos = Player.Instance.Position.WorldToScreen();
            pos.Y += 20;
            Drawing.DrawText(pos, Color.AliceBlue, "ForceR: "+ Config.ForceR, 20);

        }

        

        static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            //Utils.Debug(args.SData.Name);
            if (args.SData.Name == R1)
            {
                forceR1 = false;
            }
            if (args.Slot == SpellSlot.R && args.SData.Name != R1)
            {
                forceR2 = false;
            }
            
            if (!Q.IsReady()) return;
            if (args.Slot == SpellSlot.W)
            {
                forceW = false;
                if (Orbwalker.LastTarget != null && Orbwalker.LastTarget.IsValidTarget())
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Q, Orbwalker.LastTarget.Position), 1);
                    }
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Q, Orbwalker.LastTarget.Position), 1);
                    }
                }
                return;
            }
            if (args.SData.Name == "ItemTiamatCleave")
            {

                if (Orbwalker.LastTarget != null && Orbwalker.LastTarget.IsValidTarget())
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        //Utils.Debug("CAST Q");
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Q, Orbwalker.LastTarget.Position), 1);
                    }
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        //Utils.Debug("CAST Q");
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Q, Orbwalker.LastTarget.Position), 1);
                    }
                }
                return;
            }
            
            
        }

        private static int lastW;
        private static int lastHydra;
        private static bool forceW = false;
        private static bool forceR1 = false;
        private static bool forceR2 = false;
        private static AIHeroClient R2Target;
        // Hydra -> E is faster
        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            
            //Utils.Debug(args.SData.Name);
            if (args.SData.Name == "ItemTiamatCleave" && W.IsReady())
            {
                
                forceW = true;
                if(W.IsReady())
                    W.Cast();
                Core.DelayAction(()=>forceW = false,500);
                return;
            }
            if (args.Slot == SpellSlot.W)
            {
                forceW = false;
                lastW = Core.GameTickCount;
                if (CanCastHydra())
                {
                    Hydra.Cast();
                    return;
                }

                if (R.IsReady() && R.Name != R1)
                {
                    var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(h => (h.Distance(Player.Instance) < R.Range - 50) && RDamage(h) > h.Health && h.IsValidTarget());
                    if (enemy != null)
                    {
                        Utils.Debug("TRYING TO CAST R2 BIATHC");
                        forceR2 = true;
                        R2Target = enemy;
                        Core.DelayAction(()=>forceR2 = false,750);
                        return;
                    }
                }
                //Utils.Debug(lastW - lastHydra);
                return;

            }
            if (args.Slot == SpellSlot.E)
            {
                if (Config.ForceR && R.Name == R1 && R.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    forceR1 = true;
                    Player.CastSpell(SpellSlot.R);
                    Core.DelayAction(()=>forceR1 = false, 500);
                    return;
                }
                if (R.IsReady() && R.Name != R1)
                {
                    var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(h => (h.Distance(Player.Instance) < R.Range - 50) && RDamage(h) > h.Health && h.IsValidTarget());
                    if (enemy != null)
                    {
                        Utils.Debug("TRYING TO CAST R2 BIATHC");
                        forceR2 = true;
                        R2Target = enemy;
                        Core.DelayAction(() => forceR2 = false, 750);
                        return;
                    }
                }
            }
            if (args.Slot == SpellSlot.Q && QNum == 2)
            {
                if (R.IsReady() && R.Name != R1)
                {
                    var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(h => (h.Distance(Player.Instance) < R.Range - 50) && RDamage(h) > h.Health && h.IsValidTarget());
                    if (enemy != null)
                    {
                        Utils.Debug("TRYING TO CAST R2 BIATHC");
                        forceR2 = true;
                        R2Target = enemy;
                        Core.DelayAction(() => forceR2 = false, 750);
                        return;
                    }
                }
            }
            if (args.Slot == SpellSlot.Q && QNum != 2)
            {
                if (Orbwalker.LastTarget != null && Orbwalker.LastTarget.IsValidTarget())
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Orbwalker.LastTarget);
                    return;
                }
                if (args.Target != null && args.Target.IsValid)
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, args.Target);
                    return;
                }
            }

        }




        private static int lastQ;
        private static int lastQDelay;
        private static int QNum = 0;
        static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (_Player.IsDead) return;
            if (!sender.IsMe) return;
            int delay = 0;
            switch (args.Animation)
            {
                case "Spell1a":
                    delay = Config.Q1Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 1;
                    break;
                case "Spell1b":
                    delay = Config.Q2Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 2;
                    break;
                case "Spell1c":
                    delay = Config.Q3Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 3;
                    break;
                case "Dance":
                    if (lastQ > Core.GameTickCount - 500)
                    {
                        
                        //Orbwalker.ResetAutoAttack();
                        //Utils.Debug("reset");
                    }
                        
                    break;
            }
            
            if (delay != 0 && (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None || Config.AlwaysCancel))
            {
                lastQDelay = delay;
                Orbwalker.ResetAutoAttack();
                Core.DelayAction(DanceIfNotAborted, delay - Game.Ping);       
                //Utils.Debug("reset"); 
            }

                
        }

        private static void ForceQ()
        {
            Utils.Debug("delay " + Core.GameTickCount);
            if (Q.IsReady())
                Player.CastSpell(SpellSlot.Q, Game.CursorPos);
        }
        private static void DanceIfNotAborted()
        {
            Player.DoEmote(Emote.Dance);
            //if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
            //    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Instance.Position + (new Vector3(1.0f, 0, -1.0f)));
            //Orbwalker.ResetAutoAttack();
            /*if (ComboTarget != null && ComboTarget.IsValidTarget(_Player.AttackRange))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, ComboTarget);
                return;
            }*/
            /*if (JCTarget != null && JCTarget.IsValidTarget(_Player.AttackRange))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, ComboTarget);
                return;
            }*/
        }
        private static int lastAA;
        static void Orbwalker_OnAttack(AttackableUnit target, EventArgs args)
        {
            lastAA = Core.GameTickCount;
            if (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None)
            {
                lastAA = Core.GameTickCount;
            }
        }
        private static void AfterAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name == "ItemTitanicHydraCleave")
            {
                // because we want another auto after this
                Orbwalker.ResetAutoAttack();
                return;
            }
            if (!args.IsAutoAttack()) return;
            Utils.Debug("ondos " + Core.GameTickCount);
            var target = args.Target as AttackableUnit;
            if (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None && target != null && target.IsValidTarget())
            {
                if (!Q.IsReady() && !W.IsReady() && CanCastHydra())
                    Hydra.Cast();
                if (Cooldown(Player.Spells[0]) > 1.0f && !W.IsReady() && CanCastTitan())
                    Titan.Cast();
                lastAA = Core.GameTickCount;
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                    {
                        if (QNum >= 1 && W.IsReady())
                        {
                            

                            var target2 = (ComboTarget != null && ComboTarget.IsValidTarget(WRange)) ? ComboTarget :
                            TargetSelector.GetTarget(WRange, DamageType.Physical, null, true);
                            if (target2 != null && target2.IsValidTarget() && CanCastTitan())
                            {
                                Titan.Cast();
                                Player.IssueOrder(GameObjectOrder.AttackUnit, target2);
                                return;
                            }
                            if (target2 != null && target2.IsValidTarget() && !CanCastHydra())
                            {
                                Player.CastSpell(SpellSlot.W);
                                return;
                            }
                            if (CanCastHydra())
                            {
                                Hydra.Cast();
                                return;
                            }
                            if (R.IsReady() && R.Name != R1)
                            {
                                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(h => (h.Distance(Player.Instance) < R.Range - 50) && RDamage(h) > h.Health && h.IsValidTarget());
                                if (enemy != null)
                                {
                                    forceR2 = true;
                                    R2Target = enemy;
                                    Core.DelayAction(() => forceR2 = false, 750);
                                }
                            }

                        }
                    }
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        
                        if (QNum >= 1 && W.IsReady())
                        {
                            var target2 = (JCTarget != null && JCTarget.IsValidTarget(WRange)) ? JCTarget : EntityManager.MinionsAndMonsters.GetJungleMonsters(null, WRange, true).FirstOrDefault();
                            if (target2 != null && target2.IsValidTarget() && CanCastTitan())
                            {
                                Titan.Cast();
                                Player.IssueOrder(GameObjectOrder.AttackUnit, target2);
                                return;
                            }
                            if (target2 != null && target2.IsValidTarget() && !CanCastHydra())
                            {
                                Player.CastSpell(SpellSlot.W);
                                return;
                            }
                            if (CanCastHydra())
                                Hydra.Cast();

                        }
                    }


                    if (Mode != Orbwalker.ActiveModes.None && target.IsValidTarget(Q.Range) && target.IsValid && !target.IsDead && !target.IsZombie)
                    {
                        if (Q.IsReady())
                        {
                            Player.CastSpell(SpellSlot.Q, target.Position);
                            return;
                        }
                    }
                    if (Cooldown(Player.Spells[0]) > 1.0f && W.IsReady() && Player.Instance.CountEnemiesInRange(WRange) > 0)
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }

            }
        }
        static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None && target != null && target.IsValidTarget())
            {
                if (!Q.IsReady() && !W.IsReady() && CanCastHydra())
                    Hydra.Cast();
                if (!Q.IsReady() && !W.IsReady() && CanCastTitan())
                    Titan.Cast();
                lastAA = Core.GameTickCount;
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    if((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                    {
                        if (QNum == 2 && W.IsReady())
                    {
                        var target2 = (ComboTarget != null && ComboTarget.IsValidTarget(WRange)) ? ComboTarget :
                        TargetSelector.GetTarget(WRange, DamageType.Physical, null, true);
                        if (target2 != null && target2.IsValidTarget() && !CanCastHydra())
                        {
                            Player.CastSpell(SpellSlot.W);
                            return;
                        }
                        if (CanCastHydra())
                            Hydra.Cast();

                    }
                    }
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        if (QNum == 2 && W.IsReady())
                        {
                            var target2 = (JCTarget != null && JCTarget.IsValidTarget(WRange)) ? JCTarget : EntityManager.MinionsAndMonsters.GetJungleMonsters(null, WRange, true).FirstOrDefault();
                            if (target2 != null && target2.IsValidTarget() && !CanCastHydra())
                            {
                                Player.CastSpell(SpellSlot.W);
                                return;
                            }
                            if(CanCastHydra())
                                Hydra.Cast();

                        }
                    }

                    
                    if (target.IsValidTarget(Q.Range) && target.IsValid && !target.IsDead && !target.IsZombie)
                    {
                        if(QNum != 2 || !W.IsReady())
                            Player.CastSpell(SpellSlot.Q, target.Position);
                    }
                }

            }
            
        }
        public static uint WRange
        {
            get
            {
                return (uint)
                        (70 + _Player.BoundingRadius +
                         (_Player.HasBuff("RivenFengShuiEngine") ? 195 : 120));
            }
        }
        private static void Game_OnUpdate2(EventArgs args)
        {
            if (lastQ + 3650 < Core.GameTickCount)
                QNum = 0;
            if (forceW)
            {
                Player.CastSpell(SpellSlot.W);
                return;
            }
            if (forceR1 && R.Name == R1 && R.IsReady()) // add cast before goes on cd
            {
                Player.CastSpell(SpellSlot.R);
                return;
            }
            if (forceR2 && R.Name != R1 && R.IsReady()
                && R2Target != null && R2Target.IsValidTarget(R.Range))
            {
                Player.CastSpell(SpellSlot.R, R2Target.ServerPosition);
                return;
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)) JungleClear();
        }

        

        static void Game_OnUpdate(EventArgs args)
        {
            if (forceW)
            {
                Player.CastSpell(SpellSlot.W);
            }
            //if (SamOrb.ActiveModes.HasFlag(SamOrb.Modes.Combo)) Combo();
        }

        private static Obj_AI_Minion JCTarget;
        static void JungleClear()
        {

            if (Q.IsReady() && QNum == 0 && !Config.AAFirst)
            {
                var target = (JCTarget != null && JCTarget.IsValidTarget(Q.Range + Player.Instance.BoundingRadius)) ? JCTarget : EntityManager.MinionsAndMonsters.GetJungleMonsters(null, Q.Range, true).FirstOrDefault();
                if (target != null && target.IsValidTarget())
                {
                    JCTarget = target;
                    Player.CastSpell(SpellSlot.Q, target.ServerPosition);
                }
            }
        }


        private static AIHeroClient ComboTarget;
        static void Combo()
        {
            
            if (!Q.IsReady()) return; 
            if (Q.IsReady())
            {
                var target = (ComboTarget != null && ComboTarget.IsValidTarget(Q.Range + Player.Instance.AttackRange + Player.Instance.BoundingRadius)) ? ComboTarget :
                TargetSelector.GetTarget(Q.Range, DamageType.Physical, null, true);
                if (target != null && target.IsValidTarget() && ((lastQ < lastAA && QNum != 0) || !Config.AAFirst || !Player.Instance.IsInAutoAttackRange(target) && (Config.QGapclose > QNum)))
                {
                    ComboTarget = target;
                    Player.CastSpell(SpellSlot.Q, target.ServerPosition);
                }
            }
            if (E.IsReady() && Q.IsReady())
            {
                var target = (ComboTarget != null && ComboTarget.IsValidTarget(E.Range + Q.Range + Player.Instance.BoundingRadius)) ? ComboTarget :
                TargetSelector.GetTarget(Q.Range, DamageType.Physical, null, true);
                if (target != null)
                {
                    Player.CastSpell(SpellSlot.E, Game.CursorPos);
                    return;
                }
                
            }
            if (E.IsReady())
            {
                
                if (_Player.CountEnemiesInRange(E.Range + Q.Range + Player.Instance.AttackRange) > 1)
                {
                    Player.CastSpell(SpellSlot.E, Game.CursorPos);
                    return;
                }
                    
            }
            if (R.IsReady() && R.Name != R1)
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(h => (h.Distance(Player.Instance) < R.Range - 50) && RDamage(h) > h.Health && h.IsValidTarget());
                if (enemy != null)
                {
                    forceR2 = true;
                    R2Target = enemy;
                    Core.DelayAction(() => forceR2 = false, 750);
                }
            }

        }
        private static double RDamage(Obj_AI_Base target)
        {
            
            if (target != null && R.IsReady())
            {
                float missinghealth = (target.MaxHealth - target.Health) / target.MaxHealth > 0.75f
                    ? 0.75f
                    : (target.MaxHealth - target.Health) / target.MaxHealth;
                float pluspercent = missinghealth * (2.666667F); // 8/3
                float rawdmg = new float[] { 80, 120, 160 }[R.Level - 1] + 0.6f * _Player.FlatPhysicalDamageMod;
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, rawdmg * (1 + pluspercent));
            }
            return 0;
        }

        private static float Cooldown(SpellDataInst spell)
        {
            return Player.Spells[0].CooldownExpires - Game.Time;
        }
    }
}