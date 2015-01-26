using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Oracle.Extensions;
using Oracle.Core.Skillshots;
using Oracle.Core.Targeted;
using SpellSlot = LeagueSharp.SpellSlot;

namespace Oracle
{
    //  _____             _     
    // |     |___ ___ ___| |___ 
    // |  |  |  _| .'|  _| | -_|
    // |_____|_| |__,|___|_|___|
    // Copyright © Kurisu Solutions 2015
    internal static class Program
    {

        public static Menu Origin;
        public static bool Spell;
        public static bool Stealth;
        public static bool Danger;
        public static bool DangerCC;
        public static bool DangerUlt;
        public static bool CanManamune;
        public static string ChampionName;
        public const string Revision = "207";

        public static Obj_AI_Hero Attacker;
        public static Obj_AI_Hero AggroTarget;
        public static Obj_AI_Hero CurrentTarget;
        public static float IncomeDamage, MinionDamage;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        private static void Main(string[] args)
        {
            Console.WriteLine("Oracle is loading...");
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            Game.OnGameUpdate += Game_OnGameUpdate;

            ChampionName = Me.ChampionName;
            Origin = new Menu("Oracle", "oracle", true);

            Cleansers.Initialize(Origin);
            Defensives.Initialize(Origin);
            Summoners.Initialize(Origin);
            Offensives.Initialize(Origin);
            Consumables.Initialize(Origin);
            AutoSpells.Initialize(Origin);

            var Config = new Menu("花边-神谕菜单", "oracleconfig");
            var DangerMenu = new Menu("危险 设定", "dangerconfig");

            foreach (var i in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != Me.Team))
            {
                var menu = new Menu(i.SkinName, i.SkinName + "cccmenu");
                foreach (
                    var spell in
                        TargetSpellDatabase.Spells.Where(spell => spell.ChampionName == i.ChampionName.ToLower()))
                {
                    var danger = spell.Spellslot.ToString() == "R" ||
                                    spell.CcType != CcType.No && (spell.Type == SpellType.Skillshot || spell.Type == SpellType.Targeted);

                    menu.AddItem(new MenuItem(spell.Name + "ccc", spell.Name + " | " + spell.Spellslot)).SetValue(danger);
                }

                DangerMenu.AddSubMenu(menu);
            }

            Config.AddSubMenu(DangerMenu);
         
            var CleanseMenu = new Menu("净化 Debuffs", "cdebufs");
            CleanseMenu.AddItem(new MenuItem("stun", "眩 晕")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("charm", "魅 惑")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("taunt", "嘲 讽")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("fear", "恐 惧")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("snare", "束 缚")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("silence", "沉 默")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("suppression", "压 制")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("polymorph", "变 形")).SetValue(true);
            CleanseMenu.AddItem(new MenuItem("blind", "致 盲")).SetValue(false);
            CleanseMenu.AddItem(new MenuItem("slow", "减 速")).SetValue(false);
            CleanseMenu.AddItem(new MenuItem("poison", "中 毒")).SetValue(false);
            Config.AddSubMenu(CleanseMenu);

            var DebugMenu = new Menu("调 试", "debugmenu");
            DebugMenu.AddItem(new MenuItem("dbool", "开启 控制台调试")).SetValue(false);
            Config.AddSubMenu(DebugMenu);

            Origin.AddSubMenu(Config);

            Origin.AddItem(
                new MenuItem("ComboKey", "Combo (Active)")
                    .SetValue(new KeyBind(32, KeyBindType.Press)));

            Origin.AddToMainMenu();

            // Events
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Game.PrintChat("<font color=\"#1FFF8F\">Oracle r." + Revision + " -</font> by Kurisu 花边-神谕菜单 启动成功!");
        }

        private static GameObj _satchel, _miasma, _minefield, _crowstorm, _fizzbait, _caittrap;
        private static GameObj _chaosstorm, _glacialstorm, _lightstrike, _equinox, _tormentsoil;
        private static GameObj _depthcharge, _tremors, _acidtrail;

        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            //if (obj.Name == "FizzMarinerDoomMissile" && obj.IsEnemy && GetEnemy("Fizz").IsValid)
            //{
            //    var dmg = (float)GetEnemy("Fizz").GetSpellDamage(Friendly(), SpellSlot.R);
            //    _fizzbait = new GameObj(missile.Name, missile, true, dmg, Environment.TickCount);
            //}

            if (obj.Name.Contains("Nautilus_R_sequence_impact") && GetEnemy("Nautilus").IsValid)
            {
                var dmg = (float) GetEnemy("Nautilus").GetSpellDamage(Friendly(), SpellSlot.R, 1);
                _depthcharge = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Acidtrail_buf_red") && GetEnemy("Singed").IsValid)
            {
                var dmg = (float)GetEnemy("Singed").GetSpellDamage(Friendly(), SpellSlot.Q);
                _acidtrail = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Tremors_cas") && obj.IsEnemy && GetEnemy("Rammus").IsValid)
            {
                var dmg = (float) GetEnemy("Rammus").GetSpellDamage(Friendly(), SpellSlot.R);
                _tremors = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Crowstorm_red") && GetEnemy("Fiddlesticks").IsValid)
            {
                var dmg = (float) GetEnemy("Fiddlesticks").GetSpellDamage(Friendly(), SpellSlot.R);
                _crowstorm = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("caitlyn_Base_yordleTrap_idle_red") && GetEnemy("Caitlyn").IsValid)
            {
                var dmg = (float) GetEnemy("Caitlyn").GetSpellDamage(Friendly(), SpellSlot.W);
                _caittrap = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("LuxLightstrike_tar_red") && GetEnemy("Lux").IsValid)
            {
                var dmg = (float) GetEnemy("Lux").GetSpellDamage(Friendly(), SpellSlot.E);
                _lightstrike = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Viktor_ChaosStorm_red") && GetEnemy("Viktor").IsValid)
            {
                var dmg = (float) GetEnemy("Viktor").GetSpellDamage(Friendly(), SpellSlot.R);
                _chaosstorm = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("cryo_storm_red") && GetEnemy("Anivia").IsValid)
            {
                var dmg = (float) GetEnemy("Anivia").GetSpellDamage(Friendly(), SpellSlot.R);
                _glacialstorm = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("ZiggsE_red") && GetEnemy("Ziggs").IsValid)
            {
                var dmg = (float) GetEnemy("Ziggs").GetSpellDamage(Friendly(), SpellSlot.E);
                _minefield = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("ZiggsWRingRed") && GetEnemy("Ziggs").IsValid)
            {
                var dmg = (float) GetEnemy("Ziggs").GetSpellDamage(Friendly(), SpellSlot.W);
                _satchel = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("CassMiasma_tar_red") && GetEnemy("Cassiopeia").IsValid)
            {
                var dmg = (float) GetEnemy("Cassiopeia").GetSpellDamage(Friendly(), SpellSlot.W);
                _miasma = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Soraka_Base_E_rune_RED") && GetEnemy("Soraka").IsValid)
            {
                var dmg = (float) GetEnemy("Soraka").GetSpellDamage(Friendly(), SpellSlot.E);
                _equinox = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }

            else if (obj.Name.Contains("Morgana_Base_W_Tar_red") && GetEnemy("Morgana").IsValid)
            {
                var dmg = (float) GetEnemy("Morgana").GetSpellDamage(Friendly(), SpellSlot.W);
                _tormentsoil = new GameObj(obj.Name, obj, true, dmg, Environment.TickCount);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // prevent errors before spawning to the rift
            if (!Me.IsValidTarget(300, false))
            {
                return;
            }

            // Get current target near mouse cursor.
            foreach (
                var targ in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero.IsValidTarget(2000))
                        .OrderByDescending(hero => hero.Distance(Game.CursorPos)))
            {
                CurrentTarget = targ;
            }

            // Get dangerous buff update for zhonya (vladimir R) etc)
            foreach (var buff in GameBuff.EvadeBuffs)
            {
                var buffinst = Friendly().Buffs;
                if (buffinst.Any(
                        aura => aura.Name.ToLower().Contains(buff.SpellName) || aura.Name.ToLower() == buff.BuffName))           
                {
                    Utility.DelayAction.Add(
                        buff.Delay, delegate
                        {
                            Attacker = GetEnemy(buff.ChampionName);
                            AggroTarget = Friendly();
                            IncomeDamage = (float) GetEnemy(buff.ChampionName).GetSpellDamage(AggroTarget, buff.Slot);

                            // check if we still have buff and didn't walk out of it
                            if (buffinst.Any(
                                    aura =>
                                        aura.Name.ToLower().Contains(buff.SpellName) ||
                                        aura.Name.ToLower() == buff.BuffName))
                            {
                                DangerUlt = Origin.Item(buff.SpellName + "ccc").GetValue<bool>();
                            }

                            if (Origin.Item("dbool").GetValue<bool>())
                                Console.WriteLine("Dangerous buff on " + AggroTarget.SkinName + " should zhonyas!");
                        });
                }
            }
            
            // Get ground object damage update
            if (_tremors.Included)
            {
                if (_tremors.Obj.IsValid && Friendly().Distance(_tremors.Obj.Position, true) <= 400 * 400)
                {
                    if (GetEnemy("Rammus").IsValid)
                    {
                        Attacker = GetEnemy("Rammus");
                        AggroTarget = Friendly();
                        IncomeDamage = _tremors.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Tremors (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_acidtrail.Included)
            {
                if (_acidtrail.Obj.IsValid && Friendly().Distance(_acidtrail.Obj.Position, true) <= 150 * 150)
                {
                    if (GetEnemy("Singed").IsValid)
                    {
                        Attacker = GetEnemy("Singed");
                        AggroTarget = Friendly();
                        IncomeDamage = _acidtrail.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Poison Trail (Game Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_glacialstorm.Included)
            {
                if (_glacialstorm.Obj.IsValid && Friendly().Distance(_glacialstorm.Obj.Position, true) <= 400 * 400)
                {
                    if (GetEnemy("Anivia").IsValid)
                    {
                        Attacker = GetEnemy("Anivia");
                        AggroTarget = Friendly();
                        IncomeDamage = _glacialstorm.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Glacialstorm (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_chaosstorm.Included)
            {
                if (_chaosstorm.Obj.IsValid && Friendly().Distance(_chaosstorm.Obj.Position, true) <= 400 * 400) 
                {
                    if (GetEnemy("Viktor").IsValid)
                    {
                        Attacker = GetEnemy("Viktor");
                        AggroTarget = Friendly();
                        IncomeDamage = _chaosstorm.Damage;

                        if (AggroTarget.NetworkId == Friendly().NetworkId &&
                            Origin.Item("viktorchaosstormccc").GetValue<bool>())
                        {
                            if (Friendly().CountHerosInRange("hostile") >= Friendly().CountHerosInRange("allies") ||
                                IncomeDamage >= Friendly().Health)
                            {
                                Danger = true;
                                DangerUlt = true;
                            }
                        }

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Chaostorm (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_fizzbait.Included)
            {
                if (_fizzbait.Obj.IsValid && Friendly().Distance(_fizzbait.Obj.Position, true) <= 300 * 300)
                {
                    if (Environment.TickCount - _fizzbait.Start >= 2500)
                    {
                        if (GetEnemy("Fizz").IsValid)
                        {
                            Attacker = GetEnemy("Fizz");
                            AggroTarget = Friendly();
                            IncomeDamage = _fizzbait.Damage;

                            if (Friendly().CountHerosInRange("hostile") >= Friendly().CountHerosInRange("allies") ||
                                IncomeDamage >= Friendly().Health)
                            {
                                Danger = true;
                                DangerUlt = true;
                                DangerCC = true;
                            }
                        }

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in fizz bait (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_depthcharge.Included)
            {
                if (_depthcharge.Obj.IsValid && Friendly().Distance(_depthcharge.Obj.Position, true) <= 300 * 300)
                {
                    if (Friendly().HasBuff("nautilusgrandlinetarget", true))
                    {
                        if (GetEnemy("Nautilus").IsValid)
                        {
                            Attacker = GetEnemy("Nautilus");
                            AggroTarget = Friendly();
                            IncomeDamage = _depthcharge.Damage;

                            if (Friendly().CountHerosInRange("hostile") >= Friendly().CountHerosInRange("allies") ||
                                IncomeDamage >= Friendly().Health)
                            {
                                Danger = true;
                                DangerCC = true;
                                DangerUlt = true;
                            }

                            if (Origin.Item("dbool").GetValue<bool>())
                                Console.WriteLine(
                                    "Nautilus depth charge is homing " + AggroTarget.SkinName + " for: " + IncomeDamage);
                        }
                    }
                }
            }

            if (_caittrap.Included)
            {
                if (_caittrap.Obj.IsValid && Friendly().Distance(_caittrap.Obj.Position, true) <= 150 * 150)
                {
                    if (GetEnemy("Caitlyn").IsValid)
                    {
                        Attacker = GetEnemy("Caitlyn");
                        AggroTarget = Friendly();
                        IncomeDamage = _caittrap.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in yordle trap (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_crowstorm.Included)
            {
                // 575 Fear Range
                if (_crowstorm.Obj.IsValid && Friendly().Distance(_crowstorm.Obj.Position, true) <= 575 * 575)
                {
                    if (GetEnemy("Fiddlesticks").IsValid)
                    {
                        Attacker = GetEnemy("Fiddlesticks");
                        AggroTarget = Friendly();
                        IncomeDamage = _chaosstorm.Damage;

                        if (AggroTarget.NetworkId == Friendly().NetworkId &&
                            Origin.Item("crowstormccc").GetValue<bool>())
                        {
                            if (Friendly().CountHerosInRange("hostile") >= Friendly().CountHerosInRange("allies") ||
                                IncomeDamage >= Friendly().Health)
                            {
                                Danger = true;
                                DangerUlt = true;
                                Attacker = GetEnemy("Fiddlesticks");
                            }
                        }

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Crowstorm (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_minefield.Included)
            {
                if (_minefield.Obj.IsValid && Friendly().Distance(_minefield.Obj.Position, true) <= 300 * 300)
                {
                    if (GetEnemy("Ziggs").IsValid)
                    {
                        Attacker = GetEnemy("Ziggs");
                        AggroTarget = Friendly();
                        IncomeDamage = _minefield.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Minefield (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_satchel.Included)
            {
                if (_satchel.Obj.IsValid && Friendly().Distance(_satchel.Obj.Position, true) <= 300 * 300)
                {
                    if (GetEnemy("Ziggs").IsValid)
                    {
                        Attacker = GetEnemy("Ziggs");
                        AggroTarget = Friendly();
                        IncomeDamage = _satchel.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Satchel (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_tormentsoil.Included)
            {
                if (_tormentsoil.Obj.IsValid && Friendly().Distance(_tormentsoil.Obj.Position, true) <= 300 * 300)
                {
                    if (GetEnemy("Morgana").IsValid)
                    {
                        Attacker = GetEnemy("Morgana");
                        AggroTarget = Friendly();
                        IncomeDamage = _tormentsoil.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Torment Soil (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_miasma.Included)
            {
                if (_miasma.Obj.IsValid && Friendly().Distance(_miasma.Obj.Position, true) <= 300 * 300)
                {
                    if (GetEnemy("Cassiopeia").IsValid)
                    {
                        Attacker = GetEnemy("Cassiopeia");
                        AggroTarget = Friendly();
                        IncomeDamage = _satchel.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Miasma (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_lightstrike.Included)
            {
                if (_lightstrike.Obj.IsValid && Friendly().Distance(_lightstrike.Obj.Position, true) <= 300*300)
                {
                    if (GetEnemy("Lux").IsValid)
                    {
                        Attacker = GetEnemy("Lux");
                        AggroTarget = Friendly();
                        IncomeDamage = _lightstrike.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Lightstrike (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            if (_equinox.Included)
            {
                if (_equinox.Obj.IsValid && Friendly().Distance(_equinox.Obj.Position, true) <= 300 * 300)
                {
                    if (GetEnemy("Soraka").IsValid)
                    {
                        Attacker = GetEnemy("Lux");
                        AggroTarget = Friendly();
                        IncomeDamage = _equinox.Damage;

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine(
                                AggroTarget.SkinName + " is in Equinox (Ground Object) for: " + IncomeDamage);
                    }
                }
            }

            // reset income damage/danger safely
            if (IncomeDamage >= 1)
                Utility.DelayAction.Add(Game.Ping + 50, () => IncomeDamage = 0);
            if (MinionDamage >= 1)
                Utility.DelayAction.Add(Game.Ping + 50, () => MinionDamage = 0);

            if (Danger)
                Utility.DelayAction.Add(Game.Ping + 130, () => Danger = false);
            if (DangerCC)
                Utility.DelayAction.Add(Game.Ping + 130, () => DangerCC = false);
            if (DangerUlt)
                Utility.DelayAction.Add(Game.Ping + 130, () => DangerUlt = false);
            if (Spell)
                Utility.DelayAction.Add(Game.Ping + 130, () => Spell = false);
        }

        public static Obj_AI_Hero Friendly()
        {
            Obj_AI_Hero target = null;

            foreach (
                var unit in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => x.IsAlly && x.IsValidTarget(900, false))
                        .OrderByDescending(xe => xe.Health / xe.MaxHealth * 100))
            {
                target = unit;
            }

            return target;
        }

        private static Obj_AI_Hero GetEnemy(string championnmame)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .First(enemy => enemy.Team != Me.Team && enemy.ChampionName == championnmame);
        }

        public static int CountHerosInRange(this Obj_AI_Hero target, string team, float range = 1200f)
        {
            var enemy = team == "hostile";
            var objListTeam =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        x => x.IsValidTarget(range, false));

            return objListTeam.Count(hero => enemy ? hero.Team != target.Team : hero.Team == target.Team);
        }

        public static float GetComboDamage(Obj_AI_Hero player, Obj_AI_Base target)
        {
            var ignite = player.GetSpellSlot("summonerdot");

            // todo: get damage for different spell states
            var qready = player.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Ready;
            var wready = player.Spellbook.CanUseSpell(SpellSlot.W) == SpellState.Ready;
            var eready = player.Spellbook.CanUseSpell(SpellSlot.E) == SpellState.Ready;
            var rready = player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready;
            var iready = player.Spellbook.CanUseSpell(ignite) == SpellState.Ready;


            var tmt = Items.HasItem(3077) && Items.CanUseItem(3077)
                ? Me.GetItemDamage(target, Damage.DamageItems.Tiamat)
                : 0;

            var hyd = Items.HasItem(3074) && Items.CanUseItem(3074)
                ? Me.GetItemDamage(target, Damage.DamageItems.Hydra)
                : 0;

            var bwc = Items.HasItem(3144) && Items.CanUseItem(3144)
                ? Me.GetItemDamage(target, Damage.DamageItems.Bilgewater)
                : 0;

            var brk = Items.HasItem(3153) && Items.CanUseItem(3153)
                ? Me.GetItemDamage(target, Damage.DamageItems.Botrk)
                : 0;

            var dfg = Items.HasItem(3128) && Items.CanUseItem(3128)
                ? Me.GetItemDamage(target, Damage.DamageItems.Dfg)
                : 0;

            var guise = Items.HasItem(3151)
                ? Me.GetItemDamage(target, Damage.DamageItems.LiandrysTorment)
                : 0;

            var torch = Items.HasItem(3188) && Items.CanUseItem(3188)
                ? Me.GetItemDamage(target, Damage.DamageItems.Dfg)
                : 0;

            var items = tmt + hyd + bwc + brk + dfg + torch + guise;
            var aa = player.GetAutoAttackDamage(target);

            var qq = qready ? player.GetSpellDamage(target, SpellSlot.Q) : 0;
            var ww = wready ? player.GetSpellDamage(target, SpellSlot.W) : 0;
            var ee = eready ? player.GetSpellDamage(target, SpellSlot.E) : 0;
            var rr = rready ? player.GetSpellDamage(target, SpellSlot.R) : 0;

            var ii = iready 
                ? player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) 
                : 0;

            var damage = aa + qq + ww + ee + rr + ii + items;

            return (float) damage;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                foreach (var o in SkillshotDatabase.Spells.Where(x => x.SpellName == args.SData.Name))
                {
                    foreach (var i in Damage.Spells
                        .Where(d => d.Key == o.ChampionName)
                        .SelectMany(item => item.Value).Where(i => i.Slot == (SpellSlot) o.Slot))
                    {
                        if (i.DamageType == Damage.DamageType.Physical)
                            CanManamune = true;
                    }
                }

                foreach (var o in TargetSpellDatabase.Spells.Where(x => x.Name == args.SData.Name.ToLower()))
                {
                    foreach (var i in Damage.Spells.Where(d => d.Key == o.ChampionName)
                        .SelectMany(item => item.Value).Where(i => i.Slot == (SpellSlot) o.Spellslot))
                    {
                        if (i.DamageType == Damage.DamageType.Physical)
                            CanManamune = true;
                    }
                }

                if (Me.GetSpellSlot(args.SData.Name) == SpellSlot.Unknown &&
                    (Origin.Item("ComboKey").GetValue<KeyBind>().Active || args.Target.Type == Me.Type)) 
                {
                    CanManamune = true;
                }
                else
                {
                    Utility.DelayAction.Add(400, () => CanManamune = false);
                }
            }

            Attacker = null;
            if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy)
            {
                var HeroSender = ObjectManager.Get<Obj_AI_Hero>().First(x => x.NetworkId == sender.NetworkId);
                if (HeroSender.GetSpellSlot(args.SData.Name) == SpellSlot.Unknown && args.Target.Type == Me.Type)
                {
                    Danger = false; DangerCC = false; DangerUlt = false;
                    AggroTarget = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(args.Target.NetworkId);

                    IncomeDamage = (float)HeroSender.GetAutoAttackDamage(AggroTarget);

                    if (Origin.Item("dbool").GetValue<bool>())
                        Console.WriteLine(HeroSender.SkinName + " hit (Auto Attack) " + AggroTarget.SkinName + " for: " + IncomeDamage);
                }

                Attacker = HeroSender;
                foreach (var o in TargetSpellDatabase.Spells.Where(x => x.Name == args.SData.Name.ToLower()))
                {
                    
                    Stealth = o.Stealth;
                    if (o.Type == SpellType.Skillshot)
                    {
                        continue;
                    }

                    if (o.Type == SpellType.Self)
                    {
                        Utility.DelayAction.Add((int)(o.Delay), delegate
                        {

                            AggroTarget =
                                ObjectManager.Get<Obj_AI_Hero>()
                                    .OrderBy(x => x.Distance(HeroSender.ServerPosition))
                                    .FirstOrDefault(x => x.IsAlly);

                            if (AggroTarget != null && AggroTarget.Distance(HeroSender.ServerPosition, true) <= o.Range*o.Range)
                            {
                                IncomeDamage = (float)HeroSender.GetSpellDamage(AggroTarget, (SpellSlot)o.Spellslot);

                                if (o.Wait)
                                {
                                    return;
                                }

                                Spell = true;
                                Danger = Origin.Item(o.Name.ToLower() + "ccc").GetValue<bool>();
                                DangerUlt = Origin.Item(o.Name.ToLower() + "ccc").GetValue<bool>() && o.Spellslot.ToString() == "R";
                                DangerCC = o.CcType != CcType.No && o.Type != SpellType.AutoAttack;

                                if (Origin.Item("dbool").GetValue<bool>())
                                    Console.WriteLine("Danger (Self): " + Danger);

                                if (Origin.Item("dbool").GetValue<bool>())
                                    Console.WriteLine(HeroSender.SkinName + " hit (Self Spell) " + AggroTarget.SkinName + " for: " + IncomeDamage);

                            }
                        });
                    }

                    if (o.Type == SpellType.Targeted && args.Target.Type == Me.Type)
                    {
                        Utility.DelayAction.Add((int)(o.Delay), delegate
                        {
                            AggroTarget =
                                ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(args.Target.NetworkId);

                            IncomeDamage = (float)HeroSender.GetSpellDamage(AggroTarget, (SpellSlot)o.Spellslot);

                            if (Origin.Item("dbool").GetValue<bool>())
                            {
                                Console.WriteLine("Dangerous (Targetd Spells): " + Danger);
                                Console.WriteLine(HeroSender.SkinName + " hit (Target Spell) " + AggroTarget.SkinName +
                                                    " for: " + IncomeDamage);
                            }

                            if (o.Wait)
                            {
                                return;
                            }

                            Spell = true;
                            Danger = Origin.Item(o.Name.ToLower() + "ccc").GetValue<bool>();
                            DangerUlt = Origin.Item(o.Name.ToLower() + "ccc").GetValue<bool>() && o.Spellslot.ToString() == "R";
                            DangerCC = o.CcType != CcType.No && o.Type != SpellType.AutoAttack;
                        });
                    }
                }

                foreach (var o in SkillshotDatabase.Spells.Where(x => x.SpellName == args.SData.Name))
                {
                    var skillData =
                        new SkillshotData(o.ChampionName, o.SpellName, o.Slot, o.Type, o.Delay, o.Range,
                                            o.Radius, o.MissileSpeed, o.AddHitbox, o.FixedRange, o.DangerValue);

                    var endPosition = args.Start.To2D() +
                                        o.Range * (args.End.To2D() - HeroSender.ServerPosition.To2D()).Normalized();

                    var skillShot = new Skillshot(DetectionType.ProcessSpell, skillData, Environment.TickCount,
                        HeroSender.ServerPosition.To2D(), endPosition, HeroSender);

                    var castTime = (o.DontAddExtraDuration ? 0 : o.ExtraDuration) + o.Delay +
                                    (int)(1000 * HeroSender.Distance(Friendly().ServerPosition) / o.MissileSpeed) -
                                    (Environment.TickCount - skillShot.StartTick);

                    AggroTarget =
                        ObjectManager.Get<Obj_AI_Hero>()
                            .FirstOrDefault(x => !skillShot.IsSafe(x.ServerPosition.To2D()) && x.IsAlly);

                    if (AggroTarget != null)
                    {
                        Utility.DelayAction.Add(castTime - 400, delegate
                        {
                            Spell = true;
                            Danger = Origin.Item(o.SpellName.ToLower() + "ccc").GetValue<bool>();
                            DangerUlt = Origin.Item(o.SpellName.ToLower() + "ccc").GetValue<bool>() && o.Slot.ToString() == "R";
                            IncomeDamage = (float)HeroSender.GetSpellDamage(AggroTarget, (SpellSlot)skillShot.SkillshotData.Slot);

                            if (Origin.Item("dbool").GetValue<bool>())
                            {
                                Console.WriteLine("Dangerous (Skillshot Spells): " + Danger);
                                Console.WriteLine(HeroSender.SkinName + " may hit (SkillShot) " + AggroTarget.SkinName + " for: " + IncomeDamage);
                            }
                        });
                    }
                }
            }

            if (sender.Type == GameObjectType.obj_AI_Minion && sender.IsEnemy)
            {
                if (args.Target.Type == Me.Type)
                {
                    Danger = false; DangerCC = false; DangerUlt = false;
                    AggroTarget = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(args.Target.NetworkId);

                    MinionDamage =
                        (float)sender.CalcDamage(AggroTarget, Damage.DamageType.Physical,
                            sender.BaseAttackDamage + sender.FlatPhysicalDamageMod);
                }
            }

            if (sender.Type == GameObjectType.obj_AI_Turret && sender.IsEnemy)
            {
                if (args.Target.Type == Me.Type)
                {
                    Danger = false; DangerCC = false; DangerUlt = false;
                    if (sender.Distance(Friendly().ServerPosition, true) <= 900*900)
                    {
                        AggroTarget = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(args.Target.NetworkId);

                        IncomeDamage =
                            (float)sender.CalcDamage(AggroTarget, Damage.DamageType.Physical,
                                sender.BaseAttackDamage + sender.FlatPhysicalDamageMod);

                        if (Origin.Item("dbool").GetValue<bool>())
                            Console.WriteLine("A turret hit (Turret Attack) " + AggroTarget.SkinName + " for: " + IncomeDamage);
                    }
                }
            }
        }
    }
}