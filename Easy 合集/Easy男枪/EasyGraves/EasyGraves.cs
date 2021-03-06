﻿using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyJinx
{
    class EasyGraves : Champion
    {
        static void Main(string[] args)
        {
            new EasyGraves();
        }

        public EasyGraves() : base("Graves")
        {
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        protected override void InitializeSkins(ref SkinManager Skins)
        {
            Skins.Add("Graves");
            Skins.Add("Hired Gun Graves");
            Skins.Add("Jailbreak Graves");
            Skins.Add("Mafia Graves");
            Skins.Add("Riot Graves");
            Skins.Add("Pool Party Graves");
        }

        protected override void InitializeSpells(ref SpellManager Spells)
        {
            Spell Q = new Spell(SpellSlot.Q, 915f);
            Q.SetSkillshot(0.25f, 15f * 2 * (float)Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

            Spell W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            Spell E = new Spell(SpellSlot.E, 425f);

            Spell R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.25f, 100f, 2100f, true, SkillshotType.SkillshotLine);

            Spells.Add("Q", Q);
            Spells.Add("W", W);
            Spells.Add("E", E);
            Spells.Add("R", R);
        }

        protected override void InitializeMenu()
        {
            Menu.AddSubMenu(new Menu("连 招", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("Combo_q", "使用 Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("Combo_w", "使用 W").SetValue(true));

            Menu.AddSubMenu(new Menu("骚 扰", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("Harass_q", "使用 Q").SetValue(false));
            Menu.SubMenu("Harass").AddItem(new MenuItem("Harass_w", "使用 W").SetValue(false));

            Menu.AddSubMenu(new Menu("自 动", "Auto"));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_q", "使用 Q").SetValue(false));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_w", "使用 W").SetValue(false));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_wgap", "对突进者使用W").SetValue(true));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_wgapmana", "使用W最低魔量").SetValue(true));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_r", "可击杀使用 R ").SetValue(true));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_rrange", "显示R击杀范围").SetValue(true));

            Menu.AddSubMenu(new Menu("范 围", "Drawing"));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_q", "Q 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(100, 0, 255, 0))));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_e", "E 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(100, 0, 255, 0))));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_r", "R 范围").SetValue(new Circle(true, System.Drawing.Color.FromArgb(100, 0, 255, 0))));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_rdamage", "R 伤害指示器").SetValue(true));
        }

        protected override void Combo()
        {
            if (Menu.Item("Combo_q").GetValue<bool>()) Spells.CastSkillshot("Q", TargetSelector.DamageType.Physical);
            if (Menu.Item("Combo_w").GetValue<bool>()) Spells.CastSkillshot("W", TargetSelector.DamageType.Magical);
        }
        protected override void Harass()
        {
            if (Menu.Item("Harass_q").GetValue<bool>()) Spells.CastSkillshot("Q", TargetSelector.DamageType.Physical);
            if (Menu.Item("Harass_w").GetValue<bool>()) Spells.CastSkillshot("W", TargetSelector.DamageType.Magical);
        }

        protected override void Auto()
        {
            if (Menu.Item("Auto_q").GetValue<bool>()) Spells.CastSkillshot("Q", TargetSelector.DamageType.Physical);
            if (Menu.Item("Auto_w").GetValue<bool>()) Spells.CastSkillshot("W", TargetSelector.DamageType.Magical);
        }

        protected override void Draw()
        {
            DrawCircle("Drawing_q", "Q");
            DrawCircle("Drawing_e", "E");
            DrawCircle("Drawing_r", "R");

            Utility.HpBarDamageIndicator.DamageToUnit = UltimateDamage;
            Utility.HpBarDamageIndicator.Enabled = Menu.Item("Drawing_rdamage").GetValue<bool>();
        }

        protected override void Update()
        {
            if (Menu.Item("Auto_r").GetValue<bool>())
            {
                Spell R = Spells.get("R");

                Obj_AI_Hero target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (target == null) return;

                float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target) / R.Speed) * 1000));
                if (UltimateDamage(target) < predictedHealth || predictedHealth <= 0) return;

                if(Menu.Item("Auto_rrange").GetValue<bool>())
                {
                    if (Player.Distance(target) > Player.AttackRange)
                    {
                        if (R.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                            R.Cast(target, true);
                    }
                }
                else
                {
                    if (Player.Distance(target) < Player.AttackRange && target.Health < Damage.GetAutoAttackDamage(Player, target))
                        return;
                    if (R.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        R.Cast(target, true);
                }
            }
        }

        private float UltimateDamage(Obj_AI_Hero hero)
        {
            if (Spells.get("R").Level == 0) return 0;
            return (float)Damage.CalcDamage(Player, hero, Damage.DamageType.Physical, Damage.GetSpellDamage(Player, hero, SpellSlot.R));
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Menu.Item("Auto_wgapmana").GetValue<bool>() && Player.Mana < (Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost + Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.W).ManaCost + Player.Spellbook.GetSpell(SpellSlot.R).ManaCost))
                return;
            
            Spell W = Spells.get("W");

            if (Menu.Item("Auto_wgap").GetValue<bool>() && W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
                Spells.CastSkillshot("W", gapcloser.Sender, HitChance.VeryHigh);
        }
    }
}
