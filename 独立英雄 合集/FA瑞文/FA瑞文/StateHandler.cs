﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Assemblies;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using LeagueSharp;
using LeagueSharp.Common;
using SH = FuckingAwesomeRiven.SpellHandler;
using CH = FuckingAwesomeRiven.CheckHandler;

namespace FuckingAwesomeRiven
{
    class StateHandler
    {
        public static Obj_AI_Hero Target;

        public static Obj_AI_Hero Player;

        public static void tick()
        {
            Target = TargetSelector.GetTarget(800, TargetSelector.DamageType.Physical);
            Player = ObjectManager.Player;
        }

        public static void lastHit()
        {
            var minion = MinionManager.GetMinions(Player.Position, SH.QRange).FirstOrDefault();

            SH.animCancel(minion);

            if (SH._spells[SpellSlot.W].IsReady() && MenuHandler.getMenuBool("WLH") && CH.CanW && Environment.TickCount - CH.LastE >= 250 && minion.IsValidTarget(SH._spells[SpellSlot.W].Range) && SH._spells[SpellSlot.W].GetDamage(minion) > minion.Health)
            {
                SH.CastW();
            }

            if (SH._spells[SpellSlot.Q].IsReady() && MenuHandler.getMenuBool("QLH") && Environment.TickCount - CH.LastE >= 250 && (SH._spells[SpellSlot.Q].GetDamage(minion) > minion.Health))
            {
                if (minion.IsValidTarget(SH.QRange) && CH.CanQ)
                {
                    SH.CastQ(minion);
                }
            }
        }

        public static void laneclear()
        {
            var minion = MinionManager.GetMinions(Player.Position, SH.QRange).FirstOrDefault();

            if (HealthPrediction.GetHealthPrediction(minion, (int) (ObjectManager.Player.AttackCastDelay * 1000)) > 0 &&
                Player.GetAutoAttackDamage(minion) >
                HealthPrediction.GetHealthPrediction(minion, (int) (ObjectManager.Player.AttackCastDelay * 1000)))
            {
                SH.Orbwalk(minion);
            }


            if (SH._spells[SpellSlot.W].IsReady() && MenuHandler.getMenuBool("WWC") && CH.CanW && Environment.TickCount - CH.LastE >= 250 && minion.IsValidTarget(SH._spells[SpellSlot.W].Range) && SH._spells[SpellSlot.W].GetDamage(minion) > minion.Health)
            {
                SH.CastW();
                SH.castItems(minion);
            }

            if (SH._spells[SpellSlot.Q].IsReady() && MenuHandler.getMenuBool("QWC") && Environment.TickCount - CH.LastE >= 250 && (SH._spells[SpellSlot.Q].GetDamage(minion) + Player.GetAutoAttackDamage(minion) > minion.Health && MenuHandler.getMenuBool("QWC-AA")) || (SH._spells[SpellSlot.Q].GetDamage(minion) > minion.Health && MenuHandler.getMenuBool("QWC-LH")))
            {
                if (minion.IsValidTarget(SH.QRange) && CH.CanQ)
                {
                    SH.CastQ(minion);
                }
            }
        }

        public static void JungleFarm()
        {
            var minion =
                MinionManager.GetMinions(Player.Position, SH.QRange, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (!minion.IsValidTarget())
                return;

            SH.Orbwalk(minion);

            SH.animCancel(minion);

            if (SH._spells[SpellSlot.E].IsReady() && CH.CanE && MenuHandler.getMenuBool("EJ"))
            {
                if (minion.IsValidTarget(SH._spells[SpellSlot.E].Range))
                {
                    SH._spells[SpellSlot.E].Cast(minion.Position);
                }
            }

            if (SH._spells[SpellSlot.W].IsReady() && CH.CanW && Environment.TickCount - CH.LastE >= 250 && minion.IsValidTarget(SH._spells[SpellSlot.W].Range) && MenuHandler.getMenuBool("WJ"))
            {
                SH.CastW();
                SH.castItems(minion);
            }
            SH.castItems(minion);
            if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.LastE >= 250 && MenuHandler.getMenuBool("QJ"))
            {
                if (minion.IsValidTarget(SH.QRange) && CH.CanQ)
                {
                    SH.CastQ(minion);
                    return;
                }
            }
        }

        public static bool startedRCombo;
        public static bool startedR2Combo;

        public static void mainCombo()
        {
            SH.Orbwalk(Target);

            if (MenuHandler.getMenuBool("CR"))
            {
                if (SH._spells[SpellSlot.E].IsReady() && SH._spells[SpellSlot.R].IsReady() &&
                    SH._spells[SpellSlot.Q].IsReady() &&
                    (ItemData.Tiamat_Melee_Only.GetItem().IsReady() ||
                     ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady()) &&
                    Target.Distance(Player) < SH._spells[SpellSlot.E].Range + SH.QRange &&
                    (SH._spells[SpellSlot.Q].GetDamage(Target) * 3 + SH._spells[SpellSlot.W].GetDamage(Target) +
                     Player.GetAutoAttackDamage(Target) * 3 + SH._spells[SpellSlot.R].GetDamage(Target) > Target.Health) ||
                    startedRCombo)
                {
                    startedRCombo = true;
                    SH.CastE(Target.Position);
                    if (Environment.TickCount - CH.LastE >= 100)
                    {
                        if (ItemData.Tiamat_Melee_Only.GetItem().IsReady())
                        {
                            ItemData.Tiamat_Melee_Only.GetItem().Cast();
                            CH.lastTiamat = Environment.TickCount;
                        }
                        if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
                        {
                            ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
                            CH.lastTiamat = Environment.TickCount;
                        }
                    }
                    if (Environment.TickCount - CH.lastTiamat >= 100)
                    {
                        SH.CastR();
                    }
                    if (Environment.TickCount - CH.LastFR >= 100)
                    {
                        SH.CastQ(Target);
                        startedRCombo = false;
                    }
                    return;
                }

                if (SH._spells[SpellSlot.Q].GetDamage(Target) * 3 + SH._spells[SpellSlot.W].GetDamage(Target) +
                    Player.GetAutoAttackDamage(Target) * 3 + SH._spells[SpellSlot.R].GetDamage(Target) > Target.Health)
                {
                    if (SH._spells[SpellSlot.E].IsReady())
                    {
                        SH.CastE(Target.Position);
                    }
                    if (SH._spells[SpellSlot.R].IsReady() && Environment.TickCount - CH.LastE >= 200)
                    {
                        SH.CastR();
                    }
                }
            }

            if (SH._spells[SpellSlot.R].IsReady() && CH.RState && MenuHandler.getMenuBool("CR2") && SH._spells[SpellSlot.R].GetDamage(Target) + (SH._spells[SpellSlot.Q].IsReady() && Target.IsValidTarget(SH.QRange) ? SH._spells[SpellSlot.Q].GetDamage(Target) : 0) > Target.Health || startedR2Combo)
            {
                startedR2Combo = true;
                if (CH.RState)
                {
                    SH.CastR2(Target);
                    if (!SH._spells[SpellSlot.Q].IsReady() || !Target.IsValidTarget(SH.QRange))
                    {
                        startedR2Combo = false;
                    }
                }
                if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.lastR2 >= 100)
                {
                    SH.CastQ(Target);
                    startedR2Combo = false;
                }
                return;
            }

            if (CH.RState)
            {

                if (MenuHandler.getMenuBool("QWR2KS") && Target.IsValidTarget(SH.QRange) && SH._spells[SpellSlot.W].IsReady() && SH._spells[SpellSlot.Q].IsReady() &&
                    SH._spells[SpellSlot.Q].GetDamage(Target) + (CH.RState && SH._spells[SpellSlot.R].IsReady() ? SH._spells[SpellSlot.R].GetDamage(Target) : 0) >
                    Target.Health)
                {
                    SH.CastQ(Target);
                    Utility.DelayAction.Add(250, () => SH.CastW());
                    Utility.DelayAction.Add(515, () => SH.CastR2(Target));
                }
                else if (MenuHandler.getMenuBool("QR2KS") && Target.IsValidTarget(SH.QRange) && SH._spells[SpellSlot.Q].IsReady() &&
                         SH._spells[SpellSlot.Q].GetDamage(Target) + (CH.RState && SH._spells[SpellSlot.R].IsReady() ? SH._spells[SpellSlot.R].GetDamage(Target) : 0) > Target.Health)
                {
                    SH.CastQ(Target);
                    Utility.DelayAction.Add(515, () => SH.CastR2(Target));
                }
                else if (MenuHandler.getMenuBool("WR2KS") && Target.IsValidTarget(SH.WRange) && CH.CanW && SH._spells[SpellSlot.W].IsReady() &&
                         SH._spells[SpellSlot.W].GetDamage(Target) + (CH.RState && SH._spells[SpellSlot.R].IsReady() ? SH._spells[SpellSlot.R].GetDamage(Target) : 0) > Target.Health)
                {
                    SH.CastW();
                    Utility.DelayAction.Add(515, () => SH.CastR2(Target));
                }
            }
            else
            {
                if (MenuHandler.getMenuBool("QWKS") && Target.IsValidTarget(SH.QRange) && SH._spells[SpellSlot.Q].IsReady() && SH._spells[SpellSlot.W].IsReady() && SH._spells[SpellSlot.Q].GetDamage(Target) + SH._spells[SpellSlot.W].GetDamage(Target) > Target.Health)
                {
                    SH.CastQ(Target);
                    Utility.DelayAction.Add(250, () => SH.CastW());
                }
                else if (MenuHandler.getMenuBool("QKS") && Target.IsValidTarget(SH.QRange) && SH._spells[SpellSlot.Q].IsReady() && SH._spells[SpellSlot.Q].GetDamage(Target) > Target.Health)
                {
                    SH.CastQ(Target);
                }
                else if (MenuHandler.getMenuBool("WKS") && Target.IsValidTarget(SH.WRange) && SH._spells[SpellSlot.W].IsReady() && SH._spells[SpellSlot.W].GetDamage(Target) > Target.Health)
                {
                    SH.CastW();
                }
            }

            var BonusRange = Orbwalking.GetRealAutoAttackRange(Player) + (Target.BoundingRadius / 2) - 50;

            SH.animCancel(Target);

            if (MenuHandler.getMenuBool("CE") && SH._spells[SpellSlot.E].IsReady() && CH.CanE)
            {
                if (MenuHandler.getMenuBool("UseE-GC") && !MenuHandler.getMenuBool("UseE-AA"))
                {
                    if (!Target.IsValidTarget(SH._spells[SpellSlot.E].Range - BonusRange + 50) &&
                        Target.IsValidTarget(SH._spells[SpellSlot.E].Range + BonusRange))
                    {
                        SH.CastE(Target.Position);
                    }
                    else if (SH._spells[SpellSlot.Q].IsReady() &&
                             !Target.IsValidTarget(SH._spells[SpellSlot.E].Range + BonusRange) &&
                             Target.IsValidTarget(SH._spells[SpellSlot.E].Range + SH._spells[SpellSlot.Q].Range - 50))
                    {
                        SH.CastE(Target.Position);
                    }
                }
                else if(Target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    SH.CastE(Target.Position);
                }
            }

            SH.castItems(Target);

            if (MenuHandler.getMenuBool("CW") && SH._spells[SpellSlot.W].IsReady() && CH.CanW && Environment.TickCount - CH.LastE >= 100 && Target.IsValidTarget(SH._spells[SpellSlot.W].Range))
            {
                SH.CastW();
            }

            if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.LastE >= 100 && MenuHandler.getMenuBool("CQ"))
            {
                if (Target.IsValidTarget(SH.QRange) && CH.CanQ)
                {
                    SH.CastQ(Target);
                    return;
                }
                if (!Target.IsValidTarget(BonusRange + 50) && Target.IsValidTarget(SH.QRange) && CH.CanQ && MenuHandler.getMenuBool("UseQ-GC"))
                {
                    SH.CastQ(Target);
                }
            }
        }

        public static bool burstFinished;
        public static bool startJump1;
        public static bool startJump2;

        public static void burstCombo()
        {
            SH.Orbwalk(Target);

            if (!Target.IsValidTarget())
                return;
            var BonusRange = Orbwalking.GetRealAutoAttackRange(Player) + (Target.BoundingRadius / 2) - 50;

                if (SH.SummonerDictionary[SH.summonerSpell.Flash].IsReady() && SH._spells[SpellSlot.E].IsReady() && SH._spells[SpellSlot.R].IsReady() && !CH.RState && !Target.IsValidTarget(SH._spells[SpellSlot.E].Range) &&
                    Target.IsValidTarget(SH._spells[SpellSlot.E].Range + 400) && MenuHandler.getMenuBool("BFl") || startJump1)
                {
                    startJump1 = true;
                    if (SH._spells[SpellSlot.E].IsReady())
                    {
                        SH.CastE(Target.Position);
                    }
                    if (Environment.TickCount - CH.LastE >= 200)
                    {
                        SH.CastR();
                    }
                    if (Environment.TickCount - CH.LastFR >= 250 && Environment.TickCount - CH.LastFR < 1000)
                    {
                        SH.castFlash(Target.Position);
                        startJump1 = false;
                    }
                    return;
                }
                if (Target.IsValidTarget(SH._spells[SpellSlot.E].Range) && SH._spells[SpellSlot.E].IsReady() && SH._spells[SpellSlot.R].IsReady() && !CH.RState || startJump2)
                {
                    startJump2 = true;
                    if (SH._spells[SpellSlot.E].IsReady())
                    {
                        SH.CastE(Target.Position);
                    }
                    if (Environment.TickCount - CH.LastE >= 250)
                    {
                        SH.CastR();
                        startJump2 = false;
                    }
                    return;
                }

                switch (CH.FullComboState)
                {
                    case 1:
                        {
                            if (!Target.IsValidTarget(SH._spells[SpellSlot.W].Range))
                                return;
                            SH.CastW();
                            SH.castItems(Target);
                        }
                        break;
                    case 2:
                        {
                            CH.FullComboState = 3;
                        }
                        break;
                    case 3:
                        {
                            SH.CastQ(Target);
                        }
                        break;
                }
            

            if (SH._spells[SpellSlot.R].IsReady() && SH._spells[SpellSlot.R].GetDamage(Target) + (SH._spells[SpellSlot.Q].IsReady() && Target.IsValidTarget(SH.QRange) ? SH._spells[SpellSlot.Q].GetDamage(Target) : 0) > Target.Health || startedR2Combo)
            {
                startedR2Combo = true;
                if (CH.RState)
                {
                    SH.CastR2(Target);
                    if (!SH._spells[SpellSlot.Q].IsReady() || !Target.IsValidTarget(SH.QRange))
                    {
                        startedR2Combo = false;
                    }
                }
                if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.lastR2 >= 100)
                {
                    SH.CastQ(Target);
                    startedR2Combo = false;
                }
                return;
            }

            SH.animCancel(Target);

            if (SH._spells[SpellSlot.E].IsReady() && CH.CanE)
            {
                if (!Target.IsValidTarget(SH._spells[SpellSlot.E].Range - BonusRange + 50) && Target.IsValidTarget(SH._spells[SpellSlot.E].Range + BonusRange))
                {
                    SH.CastE(Target.Position);
                }
                else if (SH._spells[SpellSlot.Q].IsReady() && !Target.IsValidTarget(SH._spells[SpellSlot.E].Range + BonusRange) &&
                         Target.IsValidTarget(SH._spells[SpellSlot.E].Range + SH._spells[SpellSlot.Q].Range - 50))
                {
                    SH.CastE(Target.Position);
                }
            }


            if (SH._spells[SpellSlot.W].IsReady() && CH.CanW && Environment.TickCount - CH.LastE >= 250 && Target.IsValidTarget(SH._spells[SpellSlot.W].Range))
            {
                SH.CastW();
            }

            SH.castItems(Target);

            if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.LastE >= 250)
            {
                if (Target.IsValidTarget(SH.QRange) && CH.CanQ)
                {
                    SH.CastQ(Target);
                    return;
                }
                if (!Target.IsValidTarget(BonusRange + 50) && Target.IsValidTarget(SH.QRange))
                {
                    SH.CastQ(Target);
                }
            }
        }

        public static void flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (SH._spells[SpellSlot.E].IsReady() && Environment.TickCount - CH.LastQ >= 250 && MenuHandler.getMenuBool("EFlee"))
            {
                SH.CastE(Game.CursorPos);
            }

            if (SH._spells[SpellSlot.Q].IsReady() && Environment.TickCount - CH.LastE >= 250 && MenuHandler.getMenuBool("QFlee"))
            {
                SH.CastQ();
            }
        }
    }
}
