using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using OC = Oracle.Program;

namespace Oracle.Extensions
{
    internal static class Defensives
    {
        private static Menu _mainMenu, _menuConfig;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        public static void Initialize(Menu root)
        {
            Game.OnGameUpdate += Game_OnGameUpdate;

            _mainMenu = new Menu("防御 物品", "dmenu");
            _menuConfig = new Menu("防御物品 配置", "dconfig");

            foreach (var x in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly))
                _menuConfig.AddItem(new MenuItem("DefenseOn" + x.SkinName, "Use for " + x.SkinName)).SetValue(true);
            _mainMenu.AddSubMenu(_menuConfig);

            CreateMenuItem("兰顿之兆", "兰顿之兆", "selfcount", 40, 40, true);
            CreateMenuItem("炽天使之拥", "炽天使之拥",  "selfhealth", 55, 40);
            CreateMenuItem("中亚的沙漏", "中亚的沙漏", "selfzhonya", 35, 40);
            CreateMenuItem("崇山圣盾", "崇山圣盾", "allyhealth", 20, 40);
            CreateMenuItem("钢铁烈阳之匣", "钢铁烈阳之匣", "allyhealth", 45, 40);

            var tMenu = new Menu("升华护符", "tboost");
            tMenu.AddItem(new MenuItem("useTalisman", "使用 升华护符")).SetValue(true);
            tMenu.AddItem(new MenuItem("useAllyPct", "使用队友血量 %")).SetValue(new Slider(50, 1));
            tMenu.AddItem(new MenuItem("useEnemyPct", "使用敌人血量 %")).SetValue(new Slider(50, 1));
            tMenu.AddItem(new MenuItem("talismanMode", "模式: ")).SetValue(new StringList(new[] {"总是", "连招"}));
            _mainMenu.AddSubMenu(tMenu);

            var bMenu = new Menu("号令之旗", "bannerc");
            bMenu.AddItem(new MenuItem("useBanner", "使用 号令之旗")).SetValue(true);
            _mainMenu.AddSubMenu(bMenu);

            CreateMenuItem("沃格莱特的女巫帽 ", "沃格莱特的女巫帽", "selfzhonya", 35, 40);
            var oMenu = new Menu("扫描透镜", "olens");
            oMenu.AddItem(new MenuItem("useOracles", "对隐形单位使用")).SetValue(true);
            oMenu.AddItem(new MenuItem("oracleMode", "模式: ")).SetValue(new StringList(new[] { "总是", "连招" }));
            _mainMenu.AddSubMenu(oMenu);

            CreateMenuItem("奥丁面纱", "奥丁面纱", "selfcount", 40, 40, true); 
       
            root.AddSubMenu(_mainMenu);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Me.IsValidTarget(300, false))
            {
                return;
            }

            UseItemCount("Odyns", 3180, 450f);
            UseItemCount("Randuins", 3143, 450f);

            if (OC.IncomeDamage >= 1)
            {
                UseItem("allyshieldlocket", "Locket", 3190, 600f);
                UseItem("selfshieldseraph", "Seraphs", 3040);
                UseItem("selfshieldzhonya", "Zhonyas", 3157);
                UseItem("allyshieldmountain", "Mountain", 3401, 700f);
                UseItem("selfshieldzhonya", "Wooglets", 3090);
            }

            // Oracle's Lens 
            if (Items.HasItem(3364) && Items.CanUseItem(3364) && _mainMenu.Item("useOracles").GetValue<bool>())
            {
                if (!OC.Origin.Item("ComboKey").GetValue<KeyBind>().Active &&
                    _mainMenu.Item("oracleMode").GetValue<StringList>().SelectedIndex == 1)
                {
                    return;
                }

                var target = OC.Friendly();
                if (target.Distance(Me.ServerPosition, true) <= 600*600 && OC.Stealth || target.HasBuff("RengarRBuff", true))
                {
                    Items.UseItem(3364, target.ServerPosition);
                }
            }

            // Banner of command (basic)
            if (Items.HasItem(3060) && Items.CanUseItem(3060) && _mainMenu.Item("useBanner").GetValue<bool>())
            {
                var minionList = MinionManager.GetMinions(Me.Position, 1000);

                foreach (
                    var minyone in 
                        minionList.Where(minion => minion.IsValidTarget(1000) && minion.BaseSkinName.Contains("MechCannon")))
                {
                    if (minyone.Health > minyone.Health/minyone.MaxHealth*50)
                        Items.UseItem(3060, minyone);
                }
            }

            // Talisman of Ascension
            if (Items.HasItem(3069) && Items.CanUseItem(3069) && _mainMenu.Item("useTalisman").GetValue<bool>())
            {
                if (!OC.Origin.Item("ComboKey").GetValue<KeyBind>().Active &&
                    _mainMenu.Item("talismanMode").GetValue<StringList>().SelectedIndex == 1)
                {
                    return;
                }

                var target = OC.Friendly();
                if (target.Distance(Me.ServerPosition, true) > 600*600)
                {
                    return;
                }

                var LowHpEnemy =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .OrderByDescending(ex => ex.Health/ex.MaxHealth*100)
                        .First(x => x.IsValidTarget(1000));

                var aHealthPercent = target.Health/target.MaxHealth*100;
                var eHealthPercent = LowHpEnemy.Health/LowHpEnemy.MaxHealth*100;

                if (LowHpEnemy.Distance(target.ServerPosition, true) <= 900 * 900 &&
                    (target.CountHerosInRange("allies") > target.CountHerosInRange("hostile") &&
                     eHealthPercent <= _mainMenu.Item("useEnemyPct").GetValue<Slider>().Value))
                {
                    Items.UseItem(3069);
                }

                if (target.CountHerosInRange("hostile") > target.CountHerosInRange("allies") &&
                    aHealthPercent <= _mainMenu.Item("useAllyPct").GetValue<Slider>().Value)
                {
                    Items.UseItem(3069);
                }
            }
        }

        private static void UseItemCount(string name, int itemId, float itemRange)
        {
            if (!Items.HasItem(itemId) || !Items.CanUseItem(itemId))
                return;

            if (_mainMenu.Item("use" + name).GetValue<bool>())
            {
                if (Me.CountHerosInRange("hostile", itemRange) >=
                    _mainMenu.Item("use" + name + "Count").GetValue<Slider>().Value)
                {
                    Items.UseItem(itemId);
                }
            }
        }

        private static void UseItem(string menuvar, string name, int itemId, float itemRange = float.MaxValue)
        {
            if (!Items.HasItem(itemId) || !Items.CanUseItem(itemId))
                return;

            if (!_mainMenu.Item("use" + name).GetValue<bool>())
                return;

            var target = itemRange > 5000 ? Me : OC.Friendly();
            if (target.Distance(Me.ServerPosition, true) > itemRange*itemRange)
                return;
            
            var aHealthPercent = (int) ((target.Health/target.MaxHealth)*100);
            var iDamagePercent = (int) (OC.IncomeDamage/target.MaxHealth*100);

            if (!_mainMenu.Item("DefenseOn" + target.SkinName).GetValue<bool>())
                return;

            if (target.CountHerosInRange("allies") >= target.CountHerosInRange("hostile") || OC.IncomeDamage >= target.Health)
            {
                if (_mainMenu.Item("use" + name + "Ults").GetValue<bool>())
                {
                    if (OC.DangerUlt && OC.AggroTarget.NetworkId == target.NetworkId)
                        Items.UseItem(itemId, target);
                }

                if (_mainMenu.Item("use" + name + "Zhy").GetValue<bool>())
                {
                    if (OC.Danger && OC.AggroTarget.NetworkId == target.NetworkId)
                        Items.UseItem(itemId, target);
                }
            }

            if (menuvar.Contains("shield"))
            {
                if (menuvar.Contains("zhonya"))
                {
                    if (_mainMenu.Item("use" + name + "Only").GetValue<bool>() || !(OC.IncomeDamage >= target.Health))
                    {
                        return;
                    }
                }

                if (aHealthPercent <= _mainMenu.Item("use" + name + "Pct").GetValue<Slider>().Value)
                {
                    if ((iDamagePercent >= 1 || OC.IncomeDamage >= target.Health))
                    {
                        if (OC.AggroTarget.NetworkId == target.NetworkId)
                            Items.UseItem(itemId, target);
                    }

                    if (!menuvar.Contains("num"))
                    {
                        if (iDamagePercent >= _mainMenu.Item("use" + name + "Dmg").GetValue<Slider>().Value)
                        {
                            if (OC.AggroTarget.NetworkId == target.NetworkId)
                                Items.UseItem(itemId, target);
                        }
                    }
                }
            }
        }

        private static void CreateMenuItem(string displayname, string name, string type, int hpvalue, int dmgvalue,bool itemcount = false)
        {
            var menuName = new Menu(displayname, name.ToLower());

            menuName.AddItem(new MenuItem("use" + name, "使用 " + name)).SetValue(true);

            if (!itemcount)
            {
                menuName.AddItem(new MenuItem("use" + name + "Dmg", "使用 敌人技能伤害 %")).SetValue(new Slider(dmgvalue));
                menuName.AddItem(new MenuItem("use" + name + "Pct", "使用 自己生命值 %")).SetValue(new Slider(hpvalue));
            }

            if (itemcount)
                menuName.AddItem(new MenuItem("use" + name + "Count", "使用 单位数量")).SetValue(new Slider(3, 1, 5));

            if (!itemcount)
            {
                menuName.AddItem(new MenuItem("use" + name + "Zhy", "危险使用 (所有技能)")).SetValue(false);
                menuName.AddItem(new MenuItem("use" + name + "Ults", "危险使用 (仅大招)")).SetValue(true);

                if (type.Contains("zhonya"))
                    menuName.AddItem(new MenuItem("use" + name + "Only", "仅在危险时候启用")).SetValue(true);           
            }
     
            _mainMenu.AddSubMenu(menuName);
        }       
    }
}