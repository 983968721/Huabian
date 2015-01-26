#region

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Katarina
{
    class Program
    {
        public const string CharName = "Katarina";
        public static bool InUlt = false;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> Spells = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SpellSlot IgniteSlot;
        public static Items.Item DFG;
        public static Menu Config;
        public static Obj_AI_Hero Player = ObjectManager.Player;

        //E Cast
        private static long dtLastECast = 0;

        //Packet casting
        public static bool packetCast;

        //Items
        public static Items.Item biscuit = new Items.Item(2010, 10);
        public static Items.Item HPpot = new Items.Item(2003, 10);
        public static Items.Item Flask = new Items.Item(2041, 10);

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != CharName)
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 675);
            W = new Spell(SpellSlot.W, 375);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 550);

            IgniteSlot = Player.GetSpellSlot("summonerdot");
            DFG = new Items.Item((int)ItemId.Deathfire_Grasp, 750f);

            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);

            Config = new Menu("Smart卡特琳娜", "Katarina", true);      

            //Orbwalker Menu
            Config.AddSubMenu(new Menu("走砍", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Target Selector Menu
            var tsMenu = new Menu("目标选择", "Target Selector");
            TargetSelector.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);
            
            //Combo Menu
            Config.AddSubMenu(new Menu("智能连招设置", "combo"));
            Config.SubMenu("combo").AddItem(new MenuItem("smartR", "使用 智能 R").SetValue(true));
            Config.SubMenu("combo").AddItem(new MenuItem("useItems", "使用 物品").SetValue(true));
            Config.SubMenu("combo").AddItem(new MenuItem("wjCombo", "连招 使用 瞬眼").SetValue(true));

            //Killsteal
            Config.AddSubMenu(new Menu("抢人头 设置", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal", "智能 抢人头 模式").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("wjKS", "使用 瞬眼 抢人头").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("jumpsS", "使用 E跳跃 抢人头").SetValue(true));

            //Harass Menu
            Config.AddSubMenu(new Menu("骚扰 设置", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("harassKey", "骚扰 键位").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("harass").AddItem(new MenuItem("hMode", "骚扰 模式: ").SetValue(new StringList(new[] {"Q only", "Q+W", "Q+E+W"})));
            
            //Farm
            Config.AddSubMenu(new Menu("清线 设置", "farm"));
            Config.SubMenu("farm").AddItem(new MenuItem("smartFarm", "使用 智能 清线").SetValue(true));
            Config.SubMenu("farm").AddItem(new MenuItem("qFarm", "清线 使用 Q").SetValue(true));
            Config.SubMenu("farm").AddItem(new MenuItem("wFarm", "清线 使用 W").SetValue(true));
            Config.SubMenu("farm").AddItem(new MenuItem("eFarm", "清线 使用 E").SetValue(true));

            //Jungle Clear Menu
            Config.AddSubMenu(new Menu("清野 设置", "jungle"));
            Config.SubMenu("jungle").AddItem(new MenuItem("qJungle", "清野 使用 Q").SetValue(true));
            Config.SubMenu("jungle").AddItem(new MenuItem("wJungle", "清野 使用 W").SetValue(true));
            Config.SubMenu("jungle").AddItem(new MenuItem("eJungle", "清野 使用 E").SetValue(true));

            //Drawing Menu
            Config.AddSubMenu(new Menu("范围 设置", "drawing"));
            Config.SubMenu("drawing").AddItem(new MenuItem("mDraw", "禁用 所有 显示").SetValue(false));
            Config.SubMenu("drawing").AddItem(new MenuItem("ultiDmgDraw", "显示 大招 伤害").SetValue(false));
            Config.SubMenu("drawing").AddItem(new MenuItem("Target", "锁定 目标").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 0))));
            Config.SubMenu("drawing").AddItem(new MenuItem("QDraw", "显示 Q 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("drawing").AddItem(new MenuItem("WDraw", "显示 W 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("drawing").AddItem(new MenuItem("EDraw", "显示 E 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("drawing").AddItem(new MenuItem("RDraw", "显示 R 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            //Misc Menu
            Config.AddSubMenu(new Menu("杂项 设置", "misc"));
            Config.SubMenu("misc").AddItem(new MenuItem("usePackets", "使用 技能 封包").SetValue(false));
            Config.SubMenu("misc").AddItem(new MenuItem("autolvlup", "自动 加点").SetValue(new StringList(new[] { "W->E->Q", "Q>W>E" }, 0)));
            Config.SubMenu("misc").AddItem(new MenuItem("PlayLegit", "正常 E").SetValue(false));
            Config.SubMenu("misc").AddItem(new MenuItem("LegitCastDelay", "正常 E 延迟").SetValue(new Slider(1000, 0, 2000)));

            //Wardjump Menu
            Config.AddSubMenu(new Menu("瞬眼 设置", "wardjump"));
            Config.SubMenu("wardjump").AddItem(new MenuItem("wardjumpkey", "瞬眼 键位").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("wardjump").AddItem(new MenuItem("wjpriority", "瞬眼 优先级").SetValue(new StringList(new[] { "立刻", "延迟" }, 0)));

            //AutoPots menu
            Config.AddSubMenu(new Menu("自动 喝药", "AutoPot"));
            Config.SubMenu("AutoPot").AddItem(new MenuItem("AutoPot", "启用").SetValue(true));
            Config.SubMenu("AutoPot").AddItem(new MenuItem("AP_H", "喝 红药").SetValue(true));
            Config.SubMenu("AutoPot").AddItem(new MenuItem("AP_H_Per", "生命值 %").SetValue(new Slider(35, 1)));
            Config.SubMenu("AutoPot").AddItem(new MenuItem("AP_Ign", "点燃时自动喝药").SetValue(true));

            if (IgniteSlot != SpellSlot.Unknown)
            {
                Config.SubMenu("misc").AddItem(new MenuItem("autoIgnite", "可击杀 自动点燃").SetValue(true));
            }

            //Make menu visible
            Config.AddToMainMenu();

            packetCast = Config.Item("usePackets").GetValue<bool>();

            //Damage Drawer
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            //Necessary Stuff
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            
            //Announce that the assembly has been loaded
            Game.PrintChat("<font color=\"#00BFFF\">Smart鍗＄壒鐞冲 -</font> <font color=\"#FFFFFF\">鍔犺級鎴愬姛锛佹饥鍖朾y浜岀嫍锛丵Q缇361630847</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Select default target
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            

            //Main features with Orbwalker
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass(target);
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm();
                    break;
                default:
                    break;
            }

            AutoPot();
            KillSteal();
            Harass(target);

            //Ultimate fix
            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            if (InUlt)
            {
                Orbwalker.SetAttack(false);
                Orbwalker.SetMovement(false);
                return;
            }
            else
            {
                Orbwalker.SetAttack(true);
                Orbwalker.SetMovement(true);
            }

            // WardJump
            //if (wardjumpKey != null)
            //{
                //wardjump();
            //}           
        }

        //E Humanizer
        public static void CastE(Obj_AI_Base unit)
        {
            var PlayLegit = Config.Item("PlayLegit").GetValue<bool>();
            var LegitCastDelay = Config.Item("LegitCastDelay").GetValue<Slider>().Value;

            if (PlayLegit)
            {
                if (Environment.TickCount > dtLastECast + LegitCastDelay)
                {
                    E.CastOnUnit(unit, packetCast);
                    dtLastECast = Environment.TickCount;
                }
            }
            else
            {
                E.CastOnUnit(unit, packetCast);
                dtLastECast = Environment.TickCount;
            }
        }

        //Ultimate fix
        private static void PlayAnimation(GameObject sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation == "Spell4")
                {
                    InUlt = true;
                }
                else if (args.Animation == "Run" || args.Animation == "Idle1" || args.Animation == "Attack2" || args.Animation == "Attack1")
                {
                    InUlt = false;
                }
            }
        }

        //Drawing
        private static void Drawing_OnDraw(EventArgs args)
        {
            //Main drawing switch
            if (Config.Item("mDraw").GetValue<bool>())
            {
                return;
            }
            
            //Spells drawing
            foreach (var spell in Spells.Where(spell => Config.Item(spell.Slot + "Draw").GetValue<Circle>().Active))
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, spell.IsReady() ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            }

            //Target Drawing
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (Config.Item("Target").GetValue<Circle>().Active && target != null)
            {
                Render.Circle.DrawCircle(target.Position, 50, Config.Item("Target").GetValue<Circle>().Color);
            }
        }

        /*
        //JumpKS
        private static void JumpKs(Obj_AI_Hero hero)
        {
            foreach (Obj_AI_Minion ward in ObjectManager.Get<Obj_AI_Minion>().Where(ward =>
                E.IsReady() && Q.IsReady() && ward.Name.ToLower().Contains("ward") &&
                ward.Distance(hero.ServerPosition) < Q.Range && ward.Distance(Player) < E.Range))
            {
                E.Cast(ward);
                return;
            }

            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(jumph =>
                E.IsReady() && Q.IsReady() && jumph.Distance(hero.ServerPosition) < Q.Range &&
                jumph.Distance(Player) < E.Range && jumph.IsValidTarget(E.Range)))
            {
                E.Cast(jumph);
                return;
            }

            foreach (Obj_AI_Minion minion in ObjectManager.Get<Obj_AI_Minion>().Where(jumpm =>
                E.IsReady() && Q.IsReady() && jumpm.Distance(hero.ServerPosition) < Q.Range &&
                jumpm.Distance(Player) < E.Range && jumpm.IsValidTarget(E.Range)))
            {
                E.Cast(jumpm);
                return;
            }
        }
        */
      
        //Killsteal
        private static void KillSteal()
        {
            if (Config.Item("KillSteal").GetValue<bool>())
            {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(
                    hero => 
                    ObjectManager.Player.Distance(hero.ServerPosition) <= E.Range 
                    && !hero.IsMe
                    && hero.IsValidTarget() 
                    && hero.IsEnemy 
                    && !hero.IsInvulnerable
                ))
            {
                //Variables
                var Qdmg = Q.GetDamage(hero);
                var Wdmg = W.GetDamage(hero);
                var Edmg = E.GetDamage(hero);
                var MarkDmg = Damage.CalcDamage(Player, hero, Damage.DamageType.Magical , Player.FlatMagicDamageMod * 0.15 + Player.Level * 15);
                float Ignitedmg;

                //Ignite Damage
                if (IgniteSlot != SpellSlot.Unknown)
                {
                    Ignitedmg = (float)Damage.GetSummonerSpellDamage(Player, hero, Damage.SummonerSpell.Ignite);
                }

                else 

                { 
                    Ignitedmg = 0f; 
                }

                //W + Mark
                if (hero.HasBuff("katarinaqmark") && hero.Health - Wdmg - MarkDmg < 0 && W.IsReady() && W.IsInRange(hero))
                {
                    W.Cast(packetCast);
                }
                //Ignite
                if (hero.Health - Ignitedmg < 0 && IgniteSlot.IsReady())
                {
                    Player.Spellbook.CastSpell(IgniteSlot, hero);
                }
                // E
                if (hero.Health - Edmg < 0 && E.IsReady())
                {
                    E.Cast(hero, packetCast);
                }
                // Q
                if (hero.Health - Qdmg < 0 && Q.IsReady() && Q.IsInRange(hero))                   
                {
                    Q.Cast(hero, packetCast);
                }
                /*else if (Q.IsReady() && E.IsReady() && Player.Distance(hero.ServerPosition) <= 1375 && Config.Item("jumpKs", true).GetValue<bool>())
                    {
                        JumpKs(hero);
                        Q.Cast(hero, packetCast);
                        return;
                    } */
                // E + W
                if (hero.Health - Edmg - Wdmg < 0 && E.IsReady() && W.IsReady())
                {
                    CastE(hero);
                    W.Cast(packetCast);
                }
                // E + Q
                if (hero.Health - Edmg - Qdmg < 0 && E.IsReady() && Q.IsReady())
                {
                    CastE(hero);
                    Q.Cast(hero, packetCast);
                }
                // E + Q + W (don't proc Mark)
                if (hero.Health - Edmg - Wdmg - Qdmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady())
                {
                    CastE(hero);
                    Q.Cast(hero, packetCast);
                    W.Cast(packetCast);
                }
                // E + Q + W + Mark
                if (hero.Health - Edmg - Wdmg - Qdmg - MarkDmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady())
                {
                    CastE(hero);
                    Q.Cast(hero, packetCast);
                    W.Cast(packetCast);
                }
                // E + Q + W + Ignite
                if (hero.Health - Edmg - Wdmg - Qdmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady() && IgniteSlot.IsReady())
                {
                    CastE(hero);
                    Q.Cast(hero, packetCast);
                    W.Cast(packetCast);
                    Player.Spellbook.CastSpell(IgniteSlot, hero);
                }
            }

            foreach (Obj_AI_Base target in ObjectManager.Get<Obj_AI_Base>().Where(
                    target =>
                    ObjectManager.Player.Distance(target.ServerPosition) <= E.Range
                    && !target.IsMe
                    && target.IsTargetable
                    && !target.IsInvulnerable
                ))
            {
                foreach (Obj_AI_Hero focus in ObjectManager.Get<Obj_AI_Hero>().Where(
                    focus =>
                    focus.Distance(target.ServerPosition) <= Q.Range
                    && focus.IsEnemy
                    && !focus.IsMe
                    && !focus.IsInvulnerable
                    && focus.IsValidTarget()
                ))
                {
                    //Variables
                    var Qdmg = Q.GetDamage(focus);
                    var Wdmg = W.GetDamage(focus);
                    float Ignitedmg;

                    //Ignite Damage
                    if (IgniteSlot != SpellSlot.Unknown)
                    {
                        Ignitedmg = (float)Damage.GetSummonerSpellDamage(Player, focus, Damage.SummonerSpell.Ignite);
                    }
                    else
                    {
                        Ignitedmg = 0f;
                    }

                    //Mark Damage
                    var MarkDmg = Damage.CalcDamage(Player, focus, Damage.DamageType.Magical, Player.FlatMagicDamageMod * 0.15 + Player.Level * 15);

                    //Q
                    if (focus.Health - Qdmg < 0 && E.IsReady() && Q.IsReady() && focus.Distance(target.ServerPosition) <= Q.Range)
                    {
                        CastE(target);
                        Q.Cast(focus, packetCast);
                    }
                    // Q + W
                    if (focus.Distance(target.ServerPosition) <= W.Range && focus.Health - Qdmg - Wdmg < 0 && E.IsReady() && Q.IsReady())
                    {
                        CastE(target);
                        Q.Cast(focus, packetCast);
                        W.Cast(packetCast);
                    }
                    // Q + W + Mark
                    if (focus.Distance(target.ServerPosition) <= W.Range && focus.Health - Qdmg - Wdmg - MarkDmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady())
                    {
                        CastE(target);
                        Q.Cast(focus, packetCast);
                        W.Cast(packetCast);
                    }
                    // Q + Ignite
                    if (focus.Distance(target.ServerPosition) <= 600 && focus.Health - Qdmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() && IgniteSlot.IsReady())
                    {
                        CastE(target);
                        Q.Cast(focus, packetCast);
                        Player.Spellbook.CastSpell(IgniteSlot, focus);
                    }
                    // Q + W + Ignite
                    if (focus.Distance(target.ServerPosition) <= W.Range && focus.Health - Qdmg - Wdmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady() && IgniteSlot.IsReady())
                    {
                        CastE(target);
                        Q.Cast(focus, packetCast);
                        W.Cast(packetCast);
                        Player.Spellbook.CastSpell(IgniteSlot, focus);
                    }

                }

            }
            }
        }

        // Auto pot
        private static void AutoPot()
        {
            if (Config.Item("AutoPot").GetValue<bool>())
            {
                if (Config.SubMenu("AutoPot").Item("AP_Ign").GetValue<bool>())
                    if (Player.HasBuff("summonerdot") || Player.HasBuff("MordekaiserChildrenOfTheGrave"))
                    {
                        if (!Player.InFountain())

                            if (Items.HasItem(biscuit.Id) && Items.CanUseItem(biscuit.Id) && !Player.HasBuff("ItemMiniRegenPotion"))
                            {
                                biscuit.Cast(Player);
                            }
                            else if (Items.HasItem(HPpot.Id) && Items.CanUseItem(HPpot.Id) && !Player.HasBuff("RegenerationPotion") && !Player.HasBuff("Health Potion"))
                            {
                                HPpot.Cast(Player);
                            }
                            else if (Items.HasItem(Flask.Id) && Items.CanUseItem(Flask.Id) && !Player.HasBuff("ItemCrystalFlask"))
                            {
                                Flask.Cast(Player);
                            }
                    }

                if (ObjectManager.Player.HasBuff("Recall") || Player.InFountain() && Player.InShop())
                {
                    return;
                }

                //Health Pots
                if (Player.Health / 100 <= Config.Item("AP_H_Per").GetValue<Slider>().Value && !Player.HasBuff("RegenerationPotion", true))
                {
                    Items.UseItem(2003);
                }
            }
        }

        //Combo
        private static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var Rdmg = R.GetDamage(target, 1);

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target.IsValidTarget() && !InUlt)
            {
                //Cast DFG
                if (Config.Item("useItems").GetValue<bool>() && DFG.IsReady())
                {
                    DFG.Cast(target);
                }

                //Smart Q->E
                if (Q.IsInRange(target))
                {
                    if (Q.IsReady())
                    {
                        Q.Cast(target, packetCast);
                    }
                    if (E.IsReady())
                    {
                        CastE(target);
                    }
                }

                else

                {
                    if (E.IsReady())
                    {
                        CastE(target);
                    }
                    if (Q.IsReady())
                    {
                        Q.Cast(target, packetCast);
                    }
                }

                //Cast W
                if (W.IsReady() && W.IsInRange(target))
                {
                    Orbwalker.SetAttack(false);
                    Orbwalker.SetMovement(false);
                    W.Cast(packetCast);
                    return;
                }

                //Smart R
                if (Config.Item("smartR").GetValue<bool>())
                {
                    if (R.IsReady() && target.Health - Rdmg < 0 && !InUlt && !E.IsReady())
                    {
                        Orbwalker.SetAttack(false);
                        Orbwalker.SetMovement(false);
                        InUlt = true;
                        R.Cast(packetCast);
                        return;
                    }
                }

                else if (R.IsReady() && !InUlt && !E.IsReady())

                {
                    Orbwalker.SetAttack(false);
                    Orbwalker.SetMovement(false);
                    InUlt = true;
                    R.Cast(packetCast);
                    return;
                }

            }
        }

        //Harass
        private static void Harass(Obj_AI_Base target)
        {
            var harassKey = Config.Item("harassKey").GetValue<KeyBind>().Active;
            var menuItem = Config.Item("hMode").GetValue<StringList>().SelectedIndex; //Select the Harass Mode

            if (harassKey && target != null)
            {

                switch (menuItem)
                {
                    case 0: //1st mode: Q only
                        if (Q.IsReady())
                        {
                            Q.CastOnUnit(target, packetCast);
                            return;
                        }
                        break;
                    case 1: //2nd mode: Q and W
                        if (Q.IsReady() && W.IsReady())
                        {
                            Q.Cast(target, packetCast);
                            if (W.IsInRange(target))
                            {
                                W.Cast(target, packetCast);
                            }
                            return;
                        }
                        break;
                    case 2: //3rd mode: Q, E and W
                        if (Q.IsReady() && W.IsReady() && E.IsReady())
                        {
                            Q.Cast(target, packetCast);
                            CastE(target);
                            W.Cast(target, packetCast);
                            return;
                        }
                        break;

                }
            }
        }
        
        //Farm
        private static void Farm()
        {

            if (Config.Item("smartFarm").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget() && minion.IsEnemy && minion.Distance(Player.ServerPosition) < E.Range))
                {
                    var Qdmg = Q.GetDamage(minion);
                    var Wdmg = W.GetDamage(minion);
                    var Edmg = E.GetDamage(minion);
                    var MarkDmg = Damage.CalcDamage(Player, minion, Damage.DamageType.Magical, Player.FlatMagicDamageMod * 0.15 + Player.Level * 15);

                    //Killable with Q
                    if (minion.Health - Qdmg <= 0 && minion.Distance(Player.ServerPosition) <= Q.Range && Q.IsReady() && (Config.Item("wFarm").GetValue<bool>()))
                    {
                        Q.Cast(minion, packetCast); 
                    }

                    //Killable with W
                    if (minion.Health - Wdmg <= 0 && minion.Distance(Player.ServerPosition) <= W.Range && W.IsReady() && (Config.Item("wFarm").GetValue<bool>()))
                    { 
                        W.Cast(packetCast); 
                    }

                    //Killable with E
                    if (minion.Health - Edmg <= 0 && minion.Distance(Player.ServerPosition) <= E.Range && E.IsReady() && (Config.Item("eFarm").GetValue<bool>()))
                    {
                        CastE(minion);
                    }

                    //Killable with Q and W
                    if (minion.Health - Wdmg - Qdmg <= 0 && minion.Distance(Player.ServerPosition) <= W.Range && Q.IsReady() && W.IsReady() && (Config.Item("qFarm").GetValue<bool>()) && (Config.Item("wFarm").GetValue<bool>())) 
                    { 
                        Q.Cast(minion, packetCast);
                        W.Cast(packetCast); 
                    }

                    //Killable with Q, W and Mark
                    if (minion.Health - Wdmg - Qdmg - MarkDmg <= 0 && minion.Distance(Player.ServerPosition) <= W.Range && Q.IsReady() && W.IsReady() && (Config.Item("qFarm").GetValue<bool>()) && (Config.Item("wFarm").GetValue<bool>()))
                    {
                        Q.Cast(minion, packetCast);
                        W.Cast(packetCast);
                    }

                    //Killable with Q, W, E and Mark
                    if (minion.Health - Wdmg - Qdmg - MarkDmg - Edmg <= 0 && minion.Distance(Player.ServerPosition) <= W.Range && E.IsReady() && Q.IsReady() && W.IsReady() && (Config.Item("qFarm").GetValue<bool>()) && (Config.Item("wFarm").GetValue<bool>()) && (Config.Item("eFarm").GetValue<bool>()))
                    {
                        CastE(minion);
                        Q.Cast(minion, packetCast);
                        W.Cast(packetCast);
                    }

                }
            }
        }

        //Jungleclear
        private static void JungleClear() 
        {
            // Get mobs in range, try to order them by max health to get the big ones
            var mobs = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0)
            {
                return;
            }

            var mob = mobs[0];
            if (mob == null)
            {
                return;
            }

            if (Config.Item("qJungle").GetValue<bool>() && Q.IsReady())
            {
                Q.CastOnUnit(mob, packetCast);
            }

            if (Config.Item("wJungle").GetValue<bool>() && W.IsReady())
            {
                W.CastOnUnit(mob, packetCast);
            }

            if (Config.Item("eJungle").GetValue<bool>() && E.IsReady())
            {
                E.CastOnUnit(mob, packetCast);
            }
        }

        //Combo Damage calculating
        private static float ComboDamage(Obj_AI_Base target)
        {
            var dmg = 0d;

            if (Q.IsReady())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (Config.Item("ultiDmgDraw").GetValue<bool>())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.R, 1);
            }
            return (float) dmg;
        }
    }
}