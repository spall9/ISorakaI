using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using color = System.Drawing.Color;
using Microsoft.Win32;
using SharpDX;

namespace ISorakaI
{
    internal class General
    {
        private static Spell.Skillshot Q, E;
        private static Spell.Targeted W;
        private static Spell.Active R;

        private static Menu Config, Support;

        private static AIHeroClient User => Player.Instance;

        public void Load()
        {
            //Spells
            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Circular, (int) 0.283f, 1100, (int) 210f);
            W = new Spell.Targeted(SpellSlot.W, 550);
            E = new Spell.Skillshot(SpellSlot.E, 925, SkillShotType.Circular, (int) 0.5f, 1750, (int) 70f);
            R = new Spell.Active(SpellSlot.R);

            //Menu
            Config = MainMenu.AddMenu("BP's Soraka", "SorakaMenu");
            Config.AddGroupLabel("[Spell Settings]:");
            Config.AddGroupLabel("• Starcall [Q]:");
            Config.Add("Soraka.Q.Combo", new CheckBox("Use Q in Combo"));
            Config.Add("Soraka.Q.Harass", new CheckBox("Use Q in Harass"));
            Config.Add("Soraka.Q.Gap", new CheckBox("Use Q on GapCloser"));
            Config.AddGroupLabel("• Astral Infusion [W]:");
            Config.Add("Soraka.W.Auto", new CheckBox("Use Auto W"));
            Config.Add("Soraka.W.Fountain", new CheckBox("Don't use W in fountain range"));
            Config.Add("Soraka.W.Ally", new Slider("Auto W if Ally Health >= [{0}]%", 50, 0, 100));
            Config.Add("Soraka.W.Me", new Slider("Don't Use W if My Health <= [{0}]% ", 30, 0, 100));
            Config.Add("Soraka.W.HealingPrio", new ComboBox("Healing Mode", new List<string>(new[] { "Most AD", "Most AP", "Least Health", "Prioritize Squishies" })));
            Config.AddGroupLabel("• Equinox [E]:");
            Config.Add("Soraka.E.Combo", new CheckBox("Use E in Combo"));
            Config.Add("Soraka.E.Gap", new CheckBox("Use E in GapCloser"));
            Config.Add("Soraka.E.Inter", new CheckBox("Use E to Interrupt"));
            Config.AddGroupLabel("• Wish [R]:");
            Config.Add("Soraka.R.Use", new CheckBox("Use Auto R"));
            Config.Add("Soraka.R.UseMe", new CheckBox("Use Auto R to myself", false));
            Config.Add("Soraka.R.Auto", new Slider("Auto R if Ally/My Health <= [{0}]%", 15, 0, 100));
            Config.AddGroupLabel("[Miscellaneous Settings]:");
            Config.AddGroupLabel("• Drawings:");
            Config.Add("Soraka.Draw.OnlyRdy", new CheckBox("Draw Spell`s range only if they are ready"));
            Config.Add("Soraka.Draw.Q", new CheckBox("Draw Q"));
            Config.Add("Soraka.Draw.W", new CheckBox("Draw W"));
            Config.Add("Soraka.Draw.E", new CheckBox("Draw E"));
            Config.AddGroupLabel("• Skin Changer");
            Config.Add("Soraka.SkinChanger.Use", new CheckBox("Enable Skin Changer"));
            Config.Add("Soraka.SkinShanger.Skin", new ComboBox("Choose the skin", 6, "Default Skin", "Dryad Soraka", "Divine Soraka", "Celestine Soraka", "Reaper Soraka", "Order of the Banana Soraka", "Program Soraka"));

            //Support = MainMenu.AddMenu("Support Test", "SupportMenu");

            //Events
            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGap;
            Interrupter.OnInterruptableSpell += OnInterruptableSpel;
            Drawing.OnDraw += OnDraw;
        }

        private void OnUpdate(EventArgs args)
        {
            //Main modes
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;

            }

            //Other modes
            if (Config["Soraka.W.Auto"].Cast<CheckBox>().CurrentValue) { WAuto();}
            if (Config["Soraka.R.Use"].Cast<CheckBox>().CurrentValue) { RAuto();}
            if (Config["Soraka.SkinChanger.Use"].Cast<CheckBox>().CurrentValue) { User.SetSkinId(Config["Soraka.SkinShanger.Skin"].Cast<ComboBox>().CurrentValue);}

            //Support
            
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target != null)
            {
                if (Config["Soraka.Q.Combo"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    var Qpred = Q.GetPrediction(target);

                    if (Qpred.HitChance >= HitChance.High)
                    {
                        Q.Cast(target);
                    }
                }
                if (Config["Soraka.E.Combo"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    var Epred = E.GetPrediction(target);

                    if (Epred.HitChance >= HitChance.Medium)
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target != null)
            {
                if (Config["Soraka.Q.Harass"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    var Qpred = Q.GetPrediction(target);
                    if (Qpred.HitChance >= HitChance.High)
                    {
                        Q.Cast(target);
                    }
                }
            }
        }

        private static void WAuto()
        {
            if (!W.IsReady()) { return; }

            if (User.HealthPercent < Config["Soraka.W.Me"].Cast<Slider>().CurrentValue) { return; }

            if (Config["Soraka.W.Fountain"].Cast<CheckBox>().CurrentValue && User.IsInFountainRange()) { return; }

            var Allys = Config["Soraka.W.Ally"].Cast<Slider>().CurrentValue;
            var Targets = ObjectManager.Get<AIHeroClient>().Where(x => x != null && x.IsValidTarget(W.Range, false) && x.IsAlly && x.HealthPercent < Allys);
            var WPrio = Config["Soraka.W.HealingPrio"].Cast<ComboBox>().SelectedIndex;
            var target = Config["Soraka.W.Fountain"].Cast<CheckBox>().CurrentValue ? Targets.FirstOrDefault(x => !x.IsInFountainRange()) : Targets.FirstOrDefault();

            switch (WPrio)
            {
                case 0:
                    Targets = Targets.OrderByDescending(x => x.TotalAttackDamage);
                    break;
                case 1:
                    Targets = Targets.OrderByDescending(x => x.TotalMagicalDamage);
                    break;
                case 2:
                    Targets = Targets.OrderBy(x => x.Health);
                    break;
                case 3:
                    Targets = Targets.OrderBy(x => x.Health).ThenBy(x => x.MaxHealth);
                    break;
            }

            if (target != null)
            {
                W.Cast(target);
            }
        }

        private static void RAuto()
        {
            if (!R.IsReady()) { return;}

            if (Config["Soraka.R.UseMe"].Cast<CheckBox>().CurrentValue == true)
            {
                if (
                    ObjectManager.Get<AIHeroClient>()
                        .Any(
                            x =>
                                x.IsAlly && x.IsValidTarget(float.MaxValue, false) &&
                                x.HealthPercent < Config["Soraka.R.Auto"].Cast<Slider>().CurrentValue))
                {
                    R.Cast();
                }
            }
            if (Config["Soraka.R.UseMe"].Cast<CheckBox>().CurrentValue == false)
            {
                if (
                    ObjectManager.Get<AIHeroClient>()
                        .Any(
                            x =>
                                !x.IsMe && x.IsAlly && x.IsValidTarget(float.MaxValue, false) &&
                                x.HealthPercent < Config["Soraka.R.Auto"].Cast<Slider>().CurrentValue))
                {
                    R.Cast();
                }
            }
        }

        private void OnGap(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloserEventArgs)
        {
            var t = gapcloserEventArgs.Sender;

            if (Config["Soraka.Q.Gap"].Cast<CheckBox>().CurrentValue && t.IsValidTarget(Q.Range) && Q.IsReady())
            {
                Q.Cast(t);
            }
            if (Config["Soraka.E.Gap"].Cast<CheckBox>().CurrentValue && t.IsValidTarget(E.Range) && E.IsReady())
            {
                E.Cast(t);
            }
        }

        private void OnInterruptableSpel(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            var t = sender;
            var s = interruptableSpellEventArgs;

            if ((Config["Soraka.E.Inter"].Cast<CheckBox>().CurrentValue == false || s.DangerLevel != DangerLevel.High) && !t.IsValidTarget(E.Range) && !E.IsReady()) { return; }

            E.Cast(t);
        }

        private void OnDraw(EventArgs args)
        {
            var readyDraw = Config["Soraka.Draw.OnlyRdy"].Cast<CheckBox>().CurrentValue;

            if (Config["Soraka.Draw.Q"].Cast<CheckBox>().CurrentValue && readyDraw
                ? Q.IsReady()
                : Config["Soraka.Draw.Q"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { BorderWidth = 2, Color = color.Aqua, Radius = Q.Range }.Draw(User.Position);
            }
            if (Config["Soraka.Draw.W"].Cast<CheckBox>().CurrentValue && readyDraw
                ? W.IsReady()
                : Config["Soraka.Draw.W"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { BorderWidth = 2, Color = color.IndianRed, Radius = W.Range }.Draw(User.Position);
            }
            if (Config["Soraka.Draw.E"].Cast<CheckBox>().CurrentValue && readyDraw
                ? E.IsReady()
                : Config["Soraka.Draw.E"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { BorderWidth = 2, Color = color.Orange, Radius = E.Range }.Draw(User.Position);
            }
        }
    }
}