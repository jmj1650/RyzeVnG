using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;


namespace RyzeVnG
{
    class Program
    {
        private static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        private static Orbwalking.Orbwalker Orbwalker;

        private static Spell Q, W, E, R;

        private static Menu Menu;

        public static BuffInstance RyzePassive, Ryzepassivecharged;

        public static int Stack = 0;

        public static float SpeedTime = 0;

        public static double RyzeQ(Obj_AI_Base Target)
        {
            { return Player.GetSpellDamage(Target, SpellSlot.Q); }
        }
        public static double RyzeW(Obj_AI_Base Target)
        {
            { return Player.GetSpellDamage(Target, SpellSlot.W); }
        }
        public static double RyzeE(Obj_AI_Base Target)
        {
            { return Player.GetSpellDamage(Target, SpellSlot.E); }
        }

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Ryze")
                return;

            Q = new Spell(SpellSlot.Q, 850);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 50f, 1700, true, SkillshotType.SkillshotLine);


            Menu = new Menu("RyzeVnG", "RyzeVnG", true);
            Menu.SetFontStyle(System.Drawing.FontStyle.Bold, SharpDX.Color.LightGreen);
            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);
            Menu ComboMenu = Menu.AddSubMenu(new Menu("ComboSpeed", "Combospd"));
            Menu HarassMenu = Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu LaneMenu = Menu.AddSubMenu(new Menu("Lane/ JungleClear", "LaneClear"));
            Menu LasthitMenu = Menu.AddSubMenu(new Menu("Last Hit", "lasthit"));
            Menu MiscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            Menu DrawMenu = Menu.AddSubMenu(new Menu("Drawing", "Draw"));
            


            HarassMenu.AddItem(new MenuItem("QH", "Use Q").SetValue(true));
            HarassMenu.AddItem(new MenuItem("EH", "Use E").SetValue(true));
            HarassMenu.AddItem(new MenuItem("Mana", "Mana Manager").SetValue(new Slider(0, 0, 100)));

            LaneMenu.AddItem(new MenuItem("QL", "UseQ").SetValue(true));
            LaneMenu.AddItem(new MenuItem("WL", "UseW").SetValue(true));
            LaneMenu.AddItem(new MenuItem("EL", "UseE").SetValue(true));
            LaneMenu.AddItem(new MenuItem("RL", "UseR").SetValue(true));
            LaneMenu.AddItem(new MenuItem("BL", "Burster").SetValue(true));
            LaneMenu.AddItem(new MenuItem("Mana", "Mana Manager").SetValue(new Slider(0, 0, 100)));

            LasthitMenu.AddItem(new MenuItem("QL", "Q Last Hit").SetValue(true));

            MiscMenu.AddItem(new MenuItem("GapW", "W on AntiGap with smooth combo").SetValue(true));
            MiscMenu.AddItem(new MenuItem("FGapW","Force W Gapcloser").SetValue(false));
            MiscMenu.AddItem(new MenuItem("EC", "combo logic").SetValue(true)); ;
            MiscMenu.AddItem(new MenuItem("SC", "Smart Stack Charger").SetValue(new KeyBind('G',KeyBindType.Toggle,true)));
            MiscMenu.AddItem(new MenuItem("Mana", "Stack Charger Mana Manager").SetValue(new Slider(70, 0, 100)));

            DrawMenu.AddItem(new MenuItem("DAO", "Draw All Off").SetValue(false));
            DrawMenu.AddItem(new MenuItem("QD", "Draw Q").SetValue(true));
            DrawMenu.AddItem(new MenuItem("WD", "Draw W").SetValue(true));
            DrawMenu.AddItem(new MenuItem("ED", "Draw E").SetValue(true));

            ComboMenu.AddItem(new MenuItem("Speed", "Combo Speed").SetValue(new StringList(new[] { "Insane", "Fast", "Nomal", "Random" })));




            Menu.AddToMainMenu();

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.BeforeAttack += Beforeattack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;


            Notifications.AddNotification("RyzeVnG", 5000);
            Game.PrintChat("<font color = '#00ffa8'>RyzeVnG Loaded! by vengee</font>");
        }
        public static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            RyzePassive = ObjectManager.Player.Buffs.Find(DrawFX => DrawFX.Name == "ryzepassivestack" && DrawFX.IsValidBuff());
            Ryzepassivecharged = ObjectManager.Player.Buffs.Find(DrawFX => DrawFX.Name == "ryzepassivecharged" && DrawFX.IsValidBuff());
            var TargetQ = TargetSelector.GetTarget(900, TargetSelector.DamageType.Magical);
            var TargetW = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);
            var TargetE = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);
            var Target = TargetSelector.GetTarget(800, TargetSelector.DamageType.Magical);
            var TargetM = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.NotAlly);
            var QL = Menu.SubMenu("LaneClear").Item("QL").GetValue<bool>();
            var WL = Menu.SubMenu("LaneClear").Item("WL").GetValue<bool>();
            var EL = Menu.SubMenu("LaneClear").Item("EL").GetValue<bool>();
            var RL = Menu.SubMenu("LaneClear").Item("RL").GetValue<bool>();
            var BL = Menu.SubMenu("LaneClear").Item("BL").GetValue<bool>();
            var QLL = Menu.SubMenu("lasthit").Item("QL").GetValue<bool>();
            var EC = Menu.SubMenu("Misc").Item("EC").GetValue<bool>();

            if (RyzePassive != null)
            {
                Stack = RyzePassive.Count;
            }
            else
            {
                Stack = 0;
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (SpeedTime > Environment.TickCount)
                    return;

                if (W.IsReady() && W.Level >= 1)
                {
                    Orbwalker.SetAttack(false);
                }
                if (Ryzepassivecharged == null)
                {
                    if (Stack == 0 && (TargetW.Health <= Player.GetAutoAttackDamage(Target) + RyzeQ(Target) + RyzeE(Target) + RyzeW(Target)))
                    {
                        if (W.IsReady() && Target.IsValidTarget(W.Range))
                        {
                            W.CastOnUnit(TargetE);
                        }
                        if (E.IsReady() && Target.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(TargetE);
                        }
                        if (Q.IsReady() && Target.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(TargetE);
                        }
                        if (W.Cooldown >= 4.5)
                            Orbwalker.SetAttack(true);
                    }
                    Orbwalker.SetAttack(true);
                }
                if (EC)
                {
                    if (Ryzepassivecharged == null)
                    {
                        if (Stack < 5)
                        {
                            if (Q.IsReady() && W.IsReady() && E.IsReady() && R.IsReady() && TargetQ.IsValidTarget(W.Range) && Stack == 1)
                            {
                                W.CastOnUnit(TargetW);
                                E.CastOnUnit(TargetE);
                                Q.CastOnUnit(TargetQ);
                                R.CastOnUnit(Player);
                                return;
                            }
                            if (Q.IsReady() && E.IsReady() && W.IsReady() && R.IsReady() && TargetW.IsValidTarget(W.Range) && Stack == 2)
                            {
                                if (Q.IsReady())
                                {
                                    W.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    E.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    R.CastOnUnit(Player);
                                    Q.CastOnUnit(TargetW);
                                    E.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    return;
                                }
                            }

                            if (W.IsReady() && TargetW.IsValidTarget(W.Range) && (Stack <= 2 || (Stack == 3 && Q.IsReady() && (E.Cooldown <= 3.0 || R.Cooldown <= 3.0)) || (Stack == 4 && (E.Cooldown <= 5.5 || R.Cooldown <= 5.5))))
                                W.CastOnUnit(TargetW);
                            if (E.IsReady() && TargetW.IsValidTarget(W.Range) && (Stack <= 2 || (((Stack == 3 && Q.IsReady()) || Stack == 4) && ((W.Cooldown < 3.0 || R.Cooldown < 3.0) || ((W.Cooldown < 5.5 || R.Cooldown < 5.5) && Q.Cooldown < 2.6)))))
                                E.CastOnUnit(TargetW);
                            if (Q.IsReady() && TargetW.IsValidTarget(Q.Range) && ((Stack != 4 || ((TargetW.IsValidTarget(W.Range) && (E.Cooldown < 2.6 || W.Cooldown < 2.6 || R.Cooldown < 2.6))))))
                                Q.CastOnUnit(TargetW);
                            if (R.IsReady() && TargetW.IsValidTarget(675) && ((Stack >= 1 && W.IsReady()) || Stack == 4))
                            {
                                R.CastOnUnit(Player);
                                return;
                            }
                            if (!W.IsReady())
                                Orbwalker.SetAttack(true);
                        }

                    }
                    if (Ryzepassivecharged != null)
                    {
                        if (TargetW.IsValidTarget(W.Range) && Q.IsReady())
                        {
                            if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 0.7)
                                return;
                            Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(650) && R.IsReady())
                        {
                            if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 4.0)
                                return;
                            R.CastOnUnit(Player);
                            if (TargetW.IsValidTarget(W.Range + 20) && Q.IsReady())
                                Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(W.Range) && W.IsReady())
                        {
                            if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 1.3)
                                return;
                            W.CastOnUnit(TargetW);
                            if (TargetW.IsValidTarget(W.Range) && Q.IsReady())
                                Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(W.Range) && E.IsReady())
                        {
                            E.CastOnUnit(TargetW);
                            if (TargetW.IsValidTarget(W.Range) && Q.IsReady())
                                Q.CastOnUnit(TargetW);
                            return;
                        }

                    }
                }


                if (!EC)
                {
                    if (Stack == 0)
                        if (Ryzepassivecharged == null)
                        {
                            if (Q.IsReady() && TargetQ.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(TargetQ);
                                if (E.IsReady() && TargetE.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(TargetE);
                                }
                                return;
                            }
                        }
                    if (Stack == 1)
                        if (Ryzepassivecharged == null)
                        {
                            if (Q.IsReady() && W.IsReady() && E.IsReady() && R.IsReady() && TargetQ.IsValidTarget(Q.Range))
                            {
                                R.CastOnUnit(Player);
                                E.CastOnUnit(TargetE);
                                W.CastOnUnit(TargetW);
                                Q.CastOnUnit(TargetQ);
                                return;
                            }
                            else if (Q.IsReady() && TargetQ.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(TargetQ);
                                return;
                            }

                            return;
                        }
                    if (Stack == 2)
                        if (Ryzepassivecharged == null)
                        {
                            if (Q.IsReady() && E.IsReady() && W.IsReady() && R.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                if (Q.IsReady())
                                {
                                    W.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    E.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    R.CastOnUnit(Player);
                                    Q.CastOnUnit(TargetW);
                                    E.CastOnUnit(TargetW);
                                    Q.CastOnUnit(TargetW);
                                    return;
                                }

                            }
                            else if (Q.IsReady() && TargetQ.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(TargetQ);
                                return;
                            }
                        }
                    if (Stack == 3)
                        if (Ryzepassivecharged == null)
                        {
                            if (R.IsReady() && W.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                W.CastOnUnit(TargetW);
                                R.CastOnUnit(Player);
                            }
                            else if (W.IsReady() && Q.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                W.CastOnUnit(TargetW);
                                Q.CastOnUnit(TargetQ);
                            }
                            else if (Q.IsReady() && E.IsReady() && R.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                Q.CastOnUnit(TargetQ);
                                E.CastOnUnit(TargetW);
                            }

                            return;
                        }
                    if (Stack == 4)
                        if (Ryzepassivecharged == null)
                        {
                            if (W.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                W.CastOnUnit(TargetW);
                            }
                            else if (R.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                R.CastOnUnit(Player);
                            }
                            else if (E.IsReady() && TargetW.IsValidTarget(W.Range))
                            {
                                E.CastOnUnit(TargetE);
                            }
                            return;
                        }
                    if (Ryzepassivecharged != null)
                    {
                        if (TargetW.IsValidTarget(W.Range) && Q.IsReady())
                        {
                            Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(Q.Range) && R.IsReady())
                        {
                            R.CastOnUnit(Player);
                            if (TargetW.IsValidTarget(W.Range) && Q.IsReady())
                                Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(W.Range) && W.IsReady())
                        {
                            W.CastOnUnit(TargetW);
                            Q.CastOnUnit(TargetW);
                            return;
                        }
                        if (TargetW.IsValidTarget(E.Range) && E.IsReady())
                        {
                            E.CastOnUnit(TargetW);
                            Q.CastOnUnit(TargetW);
                            return;
                        }


                    }
                }
            }
            else
                Orbwalker.SetAttack(true);
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (SpeedTime > Environment.TickCount)
                    return;
                var mana = Menu.SubMenu("Harass").Item("Mana").GetValue<Slider>().Value;
                if (mana > Player.ManaPercent)
                    return;
                if (Q.IsReady() && TargetQ.IsValidTarget(Q.Range) && Menu.SubMenu("Harass").Item("QH").GetValue<bool>())
                    Q.CastOnUnit(TargetQ);
                if (E.IsReady() && TargetE.IsValidTarget(E.Range) && Menu.SubMenu("Harass").Item("EH").GetValue<bool>())
                {
                    E.CastOnUnit(TargetE);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, TargetE);
                }
            }


            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (SpeedTime > Environment.TickCount)
                    return;
                if (TargetM != null && Menu.SubMenu("LaneClear").Item("Mana").GetValue<Slider>().Value < Player.ManaPercent)
                {
                    foreach (var minion in TargetM)
                    {


                        if (Ryzepassivecharged == null)
                        {
                            if (Stack < 5)
                            {
                                if (EL && E.IsReady() && minion.IsValidTarget(W.Range) && (Stack <= 2 || (((Stack == 3 && Q.IsReady()) || Stack == 4) && ((W.Cooldown < 3.0 || R.Cooldown < 3.0) || ((W.Cooldown < 5.5 || R.Cooldown < 5.5) && Q.Cooldown < 2.6)))))
                                    E.CastOnUnit(minion, true);
                                if (WL && W.IsReady() && minion.IsValidTarget(W.Range) && (Stack <= 2 || (Stack == 3 && Q.IsReady() && (E.Cooldown <= 3.0 || R.Cooldown <= 3.0)) || (Stack == 4 && (E.Cooldown <= 5.5 || R.Cooldown <= 5.5))))
                                    W.CastOnUnit(minion, true);
                                if (QL && Q.IsReady() && minion.IsValidTarget(Q.Range) && ((Stack != 4 || ((minion.IsValidTarget(W.Range) && (E.Cooldown < 2.6 || W.Cooldown < 2.6 || R.Cooldown < 2.6))))))
                                    Q.CastOnUnit(minion, true);
                                if (RL && R.IsReady() && minion.IsValidTarget(675) && ((Stack >= 1 && W.IsReady()) || Stack == 4))
                                {
                                    R.CastOnUnit(Player);
                                    return;
                                }
                                if (!W.IsReady())
                                    Orbwalker.SetAttack(true);
                            }

                        }
                        if (Ryzepassivecharged != null)
                        {
                            if (BL)
                            {
                                if (minion.IsValidTarget(W.Range + 20) && Q.IsReady())
                                {
                                    if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 0.7)
                                        return;
                                    Q.CastOnUnit(minion, true);
                                    return;
                                }
                                if (RL && minion.IsValidTarget(650) && R.IsReady())
                                {
                                    if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 4.0)
                                        return;
                                    R.CastOnUnit(Player);
                                    if (minion.IsValidTarget(W.Range + 20) && Q.IsReady())
                                        Q.CastOnUnit(minion, true);
                                    return;
                                }
                                if (minion.IsValidTarget(W.Range) && W.IsReady())
                                {
                                    if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 1.3)
                                        return;
                                    W.CastOnUnit(minion, true);
                                    if (minion.IsValidTarget(W.Range + 20) && Q.IsReady())
                                        Q.CastOnUnit(minion, true);
                                    return;
                                }
                                if (minion.IsValidTarget(E.Range) && E.IsReady())
                                {
                                    E.CastOnUnit(minion, true);
                                    if (minion.IsValidTarget(W.Range + 20) && Q.IsReady())
                                        Q.CastOnUnit(minion, true);
                                    return;
                                }
                            }


                        }
                    }
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                var LM = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                if (TargetM != null && Menu.SubMenu("LaneClear").Item("Mana").GetValue<Slider>().Value < Player.ManaPercent)
                {
                    foreach (var minion in TargetM)
                    {
                        if (Ryzepassivecharged == null && Stack < 4 && QLL)
                        {
                            if (minion.Health < RyzeQ(minion) && minion.IsValidTarget(Q.Range))
                            {
                                Q.Cast(minion, true);
                            }

                        }
                        if (Ryzepassivecharged != null && QLL)
                        {
                            if (minion.Health < RyzeQ(minion) && minion.IsValidTarget(Q.Range))
                            {
                                Q.Cast(minion, true);
                            }
                        }
                    }
                }
            }
            if (Menu.SubMenu("Misc").Item("Mana").GetValue<Slider>().Value < Player.ManaPercent && Menu.SubMenu("Misc").Item("SC").GetValue<KeyBind>().Active && (Q.Level > 0 && W.Level > 0 && E.Level > 0))
            {
                var LM = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                if (W.IsReady() && E.IsReady() && R.IsReady() && (Stack == 0 || Stack == 1) && RyzePassive.EndTime - Game.ClockTime < 0.8 && R.Level > 0)
                {
                    if (LM != null)
                    {
                        foreach (var minion in TargetM)
                        {
                            if (Ryzepassivecharged == null)
                            {
                                if (minion.Health < RyzeQ(minion) && minion.IsValidTarget(Q.Range) && Q.IsReady())
                                {
                                    Q.Cast(minion, true);
                                }

                            }
                        }
                    }
                    else if (TargetQ != null && TargetQ.IsValidTarget(Q.Range))
                    {
                        if (Q.IsReady())
                            Q.CastOnUnit(TargetQ);
                    }
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
                else if ((W.IsReady() || R.IsReady()) && E.IsReady() && (Stack == 1 || Stack == 2) && RyzePassive.EndTime - Game.ClockTime < 0.8)
                {
                    if (LM != null)
                    {
                        foreach (var minion in TargetM)
                        {
                            if (Ryzepassivecharged == null)
                            {
                                if (minion.Health < RyzeQ(minion) && minion.IsValidTarget(Q.Range) && Q.IsReady())
                                {
                                    Q.Cast(minion, true);
                                }

                            }
                        }
                    }
                    else if (TargetQ != null && TargetQ.IsValidTarget(Q.Range))
                    {
                        if (Q.IsReady())
                            Q.CastOnUnit(TargetQ);
                    }
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
                else if ((W.IsReady() || R.IsReady() || (E.IsReady() && (R.Cooldown < 7.5 || W.Cooldown < 7.5)) && Stack == 2) || ((W.IsReady() || R.IsReady()) || (E.IsReady() && (R.Cooldown < 5.0 || W.Cooldown < 5.0) && Stack == 3)) && RyzePassive.EndTime - Game.ClockTime < 0.8)
                {
                    if (LM != null)
                    {
                        foreach (var minion in TargetM)
                        {
                            if (Ryzepassivecharged == null)
                            {
                                if (minion.Health < RyzeQ(minion) && minion.IsValidTarget(Q.Range) && Q.IsReady())
                                {
                                    Q.Cast(minion, true);
                                }

                            }
                        }
                    }
                    else if (TargetQ != null && TargetQ.IsValidTarget(Q.Range))
                    {
                        if (Q.IsReady())
                            Q.CastOnUnit(TargetQ);
                    }
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
            }
        }
        public static void Beforeattack(Orbwalking.BeforeAttackEventArgs args)
        {
            /*  if (args.Unit.IsMe)
              {
                  if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                  {
                      if (Q.IsReady() || W.IsReady() || E.IsReady())
                          args.Process = false;
                      else
                          args.Process = true;
                  }
              }
             */
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            var DrawQ = Menu.SubMenu("Draw").Item("QD").GetValue<bool>();
            var DrawW = Menu.SubMenu("Draw").Item("WD").GetValue<bool>();
            var DrawE = Menu.SubMenu("Draw").Item("ED").GetValue<bool>();
            var AllOff = Menu.SubMenu("Draw").Item("DAO").GetValue<bool>();

            if (AllOff)
                return;

            if (DrawQ)
            {
                if (Q.IsReady())
                {

                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.White);
                }
                else
                {

                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.DarkRed);
                }
            }

            if (DrawW || DrawE)
            {
                if (DrawW && DrawE)
                {
                    if (W.IsReady() && E.IsReady())
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.White);
                    }
                    else if (W.IsReady() || E.IsReady())
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.Orange);
                    }
                    else
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.DarkRed);
                    }
                }
                else if (DrawE)
                {
                    if (E.IsReady())
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.White);
                    }
                    else
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.DarkRed);
                    }
                }
                else if (DrawW)
                {
                    if (W.IsReady())
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.White);
                    }
                    else
                    {

                        Render.Circle.DrawCircle(Player.Position, W.Range, Color.DarkRed);
                    }
                }

            }
        }
        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Program.Player.IsDead && Menu.SubMenu("Misc").Item("GapW").GetValue<bool>() && Program.W.CanCast(gapcloser.Sender))
            {
                if (Ryzepassivecharged == null)
                {
                    if (Stack < 5)
                    {
                        if (Q.IsReady() && W.IsReady() && E.IsReady() && R.IsReady() && gapcloser.Sender.IsValidTarget(W.Range) && Stack == 1)
                        {
                            W.CastOnUnit(gapcloser.Sender);
                            return;
                        }
                        if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range) && (Stack <= 2 || (Stack == 3 && Q.IsReady() && (E.Cooldown <= 3.0 || R.Cooldown <= 3.0)) || (Stack == 4 && (E.Cooldown <= 5.5 || R.Cooldown <= 5.5))))
                            W.CastOnUnit(gapcloser.Sender);

                        if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range) && Menu.SubMenu("Misc").Item("FGapW").GetValue<bool>())
                        {
                            W.CastOnUnit(gapcloser.Sender);
                        }
                    }
                    if (Ryzepassivecharged != null)
                    {
                        if (gapcloser.Sender.IsValidTarget(W.Range + 20) && Q.IsReady())
                        {
                            if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 0.7)
                                return;
                            Q.CastOnUnit(gapcloser.Sender);
                            return;
                        }
                        if (gapcloser.Sender.IsValidTarget(W.Range) && W.IsReady())
                        {
                            if (R.Level == 3 && Ryzepassivecharged.EndTime - Game.ClockTime <= 1.3)
                                return;
                            W.CastOnUnit(gapcloser.Sender);
                            if (gapcloser.Sender.IsValidTarget(W.Range + 20) && Q.IsReady())
                                Q.CastOnUnit(gapcloser.Sender);
                            return;
                        }

                    }
                }
            }
        }
        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsMe)
                {
                    switch (Menu.SubMenu("Combospd").Item("Speed").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            break;
                        case 1:
                            SpeedTime = TickCount(200);
                            break;
                        case 2:
                            SpeedTime = TickCount(400);
                            break;
                        case 3:
                            var ran = new Random().Next(0, 2);
                            if (ran == 1)
                                SpeedTime = TickCount(325);
                            else if (ran == 2)
                                SpeedTime = TickCount(475);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                Game.PrintChat("It's seem somethings wrong, it's not notice you normally, plz send pm or post to vengee.");
            }
        }
        private static int TickCount(int time)
        {
            return Environment.TickCount + time;
        }
    }

}