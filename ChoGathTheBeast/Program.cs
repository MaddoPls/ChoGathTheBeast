using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using SharpDX;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace ChoGathTheBeast
{
    internal class Program
    {
        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static Orbwalking.Orbwalker Orbwalker;

        private static Spell Q, W, E, R;

        private static readonly List<Spell> SpellList = new List<Spell>();

        private static Menu Menu;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Chogath")
                return;

            Q = new Spell(SpellSlot.Q, 950);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 500);
            R = new Spell(SpellSlot.R, float.MaxValue, TargetSelector.DamageType.True);
            //Q.SetSkillshot(300,50,2000, false , SkillshotType.SkillshotLine);

            SpellList.AddRange(new[] { Q, W, E, R });

            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            ;
            TargetSelector.AddToMenu(ts);

            Menu spellMenu = Menu.AddSubMenu(new Menu("Spells", "Spells"));
            ;
            spellMenu.AddItem(new MenuItem("Use Q Harass", "Use Q Harass").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Harass", "Use W Harass").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use Q Combo", "Use Q Combo").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Combo", "Use W Combo").SetValue(true));
            spellMenu.AddItem(new MenuItem("force focus selected", "force focus selected").SetValue(false));
            spellMenu.AddItem(new MenuItem("if selected in :", "if selected in :").SetValue(new Slider(1000, 1000, 1500)));
            //spellMenu.AddItem(new MenuItem("Use E Harass", "Use E Harass")).SetValue(false);
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                spellMenu.AddItem(new MenuItem("Use R" + hero.SkinName, "Use R" + hero.SkinName)).SetValue(true);
            }

            //spellMenu.AddItem(new MenuItem("Use R to farm", "Use R to farm").SetValue(true));

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;

            Game.OnUpdate += Game_OnGameUpdate;


            Game.PrintChat("This is my first asembly dont h8 me , ty :)");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var circleEntry = Menu.Item("drawRange" + spell.Slot).GetValue<Circle>();
                if (circleEntry.Active && !Player.IsDead)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, circleEntry.Color);
                }
            }
        }

        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (Menu.Item("Use Q Combo").GetValue<bool>())
                {
                    useQ();
                }
                if (Menu.Item("Use W Combo").GetValue<bool>())
                {
                    useW();
                }
                useR();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Menu.Item("Use Q Harass").GetValue<bool>())
                {
                    useQ();
                }
                if (Menu.Item("Use W Harass").GetValue<bool>())
                {
                    useW();
                }
                useR();
            }
        }

        public static bool Selected()
        {
            if (!Menu.Item("force focus selected").GetValue<bool>())
            {
                return false;
            }
            else
            {
                var target = TargetSelector.GetSelectedTarget();
                float a = Menu.Item("if selected in :").GetValue<Slider>().Value;
                if (target == null || target.IsDead || target.IsZombie)
                {
                    return false;
                }
                else
                {
                    if (Player.Distance(target.Position) > a)
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        public static void useQ()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget())
                {
                    castQ(target);
                }
            }

        }

        public static void useW()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget())
                {
                    castW(target);
                }
            }
            else
            {
                var target = TargetSelector.GetTarget(700, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget())
                {
                    castW(target);
                }
            }
        }

        public static void useR()
        {
            double dmg = new double[] { 300, 475, 650 }[R.Level - 1] + 0.7 * Player.FlatMagicDamageMod;
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                if (R.IsReady())
                {
                    if (!hero.IsDead && !hero.IsZombie)
                    {
                        if (Menu.Item("use R" + hero.SkinName).GetValue<bool>())
                        {
                            if (hero.Health <= dmg)
                            {
                                Render.Circle.DrawCircle(hero.Position, 100, Color.Green);
                                if (Player.Distance(hero.Position) <=
                                    Player.BoundingRadius + hero.BoundingRadius + 125 + 30 && hero.IsValidTarget())
                                    Player.IssueOrder(GameObjectOrder.MoveTo, hero.Position);
                                if (Player.Distance(hero.Position) <= Player.BoundingRadius + hero.BoundingRadius + 125 &&
                                    hero.IsValidTarget())
                                    R.Cast(hero);
                            }
                        }
                    }
                }
            }

        }

        public static void castQ(Obj_AI_Base target)
        {
            if (!Q.IsReady())
                return;
            var t = Prediction.GetPrediction(target, 625).CastPosition;
            float x = target.MoveSpeed;
            float y = x * 850 / 1000;
            var pos = target.Position;
            if (target.Distance(t) <= y)
            {
                pos = t;
            }
            if (target.Distance(t) > y)
            {
                pos = target.Position.Extend(t, y);
            }
            if (Player.Distance(pos) <= 949 && target.Distance(pos) >= 100)
            {
                Q.Cast(pos);
            }
            if (Player.Distance(target.Position) <= Player.BoundingRadius + Player.AttackRange + target.BoundingRadius)
            {
                Q.Cast(pos);
            }
        }



        public static void castW(Obj_AI_Base target)
        {
            if (!W.IsReady())
                return;
            if (Player.Distance(target.Position) <= 620)
            {
                W.Cast(target.Position);
            }
        }
    }
}