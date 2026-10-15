using RogueLibsCore;
using SoRCharacters.Core.Railgunner;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    [ItemCategories(RogueCategories.Weapons, RogueCategories.Usable)]
    public class Railshot : CustomAbility, IAbilityChargeable, IAbilityRechargeable
    {
        public const string OnlineMessage = "SoRCharacters: the Railgunner only works in single-player and local co-op.";

        private static bool _onlineMessageShown;

        private bool _holdActive;
        private bool _readyShown;
        private bool _holdConsumed;
        private float _lastHeld;

        public override void SetupDetails()
        {
            base.SetupDetails();
            Item.itemType = ItemTypes.Tool;
        }

        public override void OnAdded()
        {
        }

        public override void OnPressed()
        {
            _holdActive = false;
            _readyShown = false;
            _holdConsumed = false;
            _lastHeld = 0f;

            var owner = Owner;
            if (owner == null || GameCompat.IsPreviewOrPool(owner)) return;

            if (RailshotState.IsCharging(owner))
            {
                RailshotState.End(owner);
                AimLine.Hide(owner);
                Plugin.Log.LogInfo("[Railgunner] stale charge cleared on new press");
            }

            if (!GameCompat.IsOnline) return;

            if (!_onlineMessageShown)
            {
                _onlineMessageShown = true;
                owner.Say(OnlineMessage);
            }
        }

        public void OnHeld(AbilityHeldArgs e)
        {
            var owner = Owner;
            if (owner == null) return;
            RailshotState.Touch(owner, e.HeldTime);

            // After an auto-fire the rest of this hold does nothing; a drop in HeldTime means a new hold began.
            if (_holdConsumed)
            {
                if (e.HeldTime >= _lastHeld)
                {
                    _lastHeld = e.HeldTime;
                    return;
                }
                _holdConsumed = false;
            }

            if (owner.dead || owner.KnockedOut() || GameCompat.IsPreviewOrPool(owner)) return;
            if (GameCompat.IsOnline || Count > 0) return;

            if (e.HeldTime < _lastHeld)
            {
                _holdActive = false;
                _readyShown = false;
            }
            _lastHeld = e.HeldTime;

            if (!_holdActive)
            {
                _holdActive = true;
                _readyShown = false;
                RailshotState.Begin(owner, e.HeldTime);
                Plugin.Log.LogInfo($"[Railgunner] charging started for {owner.agentRealName} (held {e.HeldTime:0.00}s already)");
            }

            if (!RailshotState.IsCharging(owner)) return;

            var charged = RailshotState.ChargedFor(owner, e.HeldTime);
            if (!_readyShown && RailshotRules.IsCharged(charged, RailgunnerModule.Settings))
            {
                _readyShown = true;
                GameController.gameController.spawnerMain.SpawnStatusText(owner, "Countdown", "READY");
                RailshotState.Arm(owner, e.HeldTime, Aim.Direction(owner));
                AimLine.Show(owner);
                RailshotState.StartReadyTone(owner);
                Plugin.Log.LogInfo($"[Railgunner] charged after {charged:0.00}s");
            }
        }

        public void OnReleased(AbilityReleasedArgs e)
        {
            _holdActive = false;
            _readyShown = false;
            _lastHeld = 0f;

            var owner = Owner;
            if (owner == null) return;

            if (_holdConsumed)
            {
                _holdConsumed = false;
                RailshotState.End(owner);
                AimLine.Hide(owner);
                Plugin.Log.LogInfo("[Railgunner] released after auto-fire -> nothing");
                return;
            }

            try
            {
                var wasCharging = RailshotState.IsCharging(owner);
                var settings = RailgunnerModule.Settings;
                var charged = RailshotState.ChargedFor(owner, e.HeldTime);
                var paused = IsPaused();
                var interfaceForced = !paused && InterfaceForcedRelease(owner);
                var outcome = RailshotRules.OnRelease(charged, !owner.dead, settings);
                var fire = wasCharging && !paused && !interfaceForced && outcome == ReleaseOutcome.Fire && Count <= 0 && !GameCompat.IsOnline;
                if (wasCharging)
                {
                    if (paused)
                    {
                        Plugin.Log.LogInfo("[Railgunner] charge cancelled (paused)");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[Railgunner] released after {charged:0.00}s charged -> {(fire ? "fire" : interfaceForced ? "cancel (interface opened)" : "cancel")}");
                    }
                }

                if (!fire) return;

                FireCharged(owner);
            }
            finally
            {
                RailshotState.End(owner);
                AimLine.Hide(owner);
            }
        }

        // Called by the module tick once he has been armed for ArmedMaxSeconds: fires exactly as a release would and
        // marks this hold as used, so letting go later neither fires again nor restarts the recharge.
        internal void AutoFire(Agent owner)
        {
            _holdConsumed = true;
            _holdActive = false;
            _readyShown = false;

            try
            {
                if (owner.dead || Count > 0 || GameCompat.IsOnline || !RailshotState.IsCharging(owner)) return;
                Plugin.Log.LogInfo($"[Railgunner] auto-fire after {RailgunnerModule.Settings.ArmedMaxSeconds:0.##}s armed");
                FireCharged(owner);
            }
            finally
            {
                RailshotState.End(owner);
                AimLine.Hide(owner);
            }
        }

        private void FireCharged(Agent owner)
        {
            RailshotState.StopReadyTone(owner);
            try
            {
                var dir = RailshotState.FireDirection(owner);
                var result = RailshotFire.Fire(owner, dir);
                RailshotFire.Guard("line 'em up", () => RailgunnerLineEmUp.RecordShot(owner, result.NpcKills));
            }
            finally
            {
                Count = RailshotRules.RechargeCount(RailgunnerModule.Settings, GameCompat.HasSupercapacitors(owner));
                owner.inventory?.buffDisplay?.specialAbilitySlot?.MakeNotUsable();
            }
        }

        // PlayerControl.Update releases a held ability when the menu opens (PlayerControl.cs:2492-2495) or when time is
        // stopped outside a hit-stop freeze (:3413-3416, GameController.cs:4504-4515 sets timeScale 0 for freeze frames).
        private static bool IsPaused()
        {
            var gc = GameController.gameController;
            return (Time.timeScale == 0f && gc.freezeFrame == -1f) || gc.mainGUI.menuGUI.onMenu;
        }

        // Mirrors the conditions under which PlayerControl.Update releases a held ability instead of continuing it
        // (PlayerControl.cs:2927-2948); the attack-press release does not match them and still fires.
        private static bool InterfaceForcedRelease(Agent owner)
        {
            if (owner.isOperating) return true;

            var gui = owner.mainGUI;
            if (gui == null) return false;
            if (gui.showingInterface) return true;
            return gui.showingTarget && !(gui.targetItem != null && gui.targetItem.invItemName == "Laptop");
        }

        public void OnRecharging(AbilityRechargingArgs e)
        {
            e.UpdateDelay = 1f;
            Item.invItemCount--;
        }

        public override CustomTooltip GetCountString()
        {
            return Count > 0 ? new CustomTooltip(Count, Color.red) : default;
        }
    }
}
