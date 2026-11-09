# Part menus fail after painting and boarding

4 November 2026: maddavo reports that after painting and the Kerbal reboarding, right-click no longer opens the vessel's part action windows (PAWs).

## Evidence and cause

The current game's log contains repeated `NullReferenceException` stacks in `UIPartActionGroup.AddItemToContent`, `KSPCommunityFixes.PAWItemsOrder`, `UIPartActionWindow.CreatePartList/UpdateWindow` and `UIPartActionController.UpdateActiveWindows`, beginning around 23:12:11. This is a UI rebuilding failure, rather than evidence that the right mouse button itself stopped working.

Graffiti Artist's old `LateUpdate` looked up the target PAW and called `Destroy(paw.gameObject)`. Inspection of the installed KSP assembly shows that a PAW's `OnDestroy` removes event subscriptions and its part link, but does not remove it from the controller's active/hidden window collections. The stock controller instead fires `onPartActionUIDismiss`, hides the window and moves it into the hidden/reusable list during normal dismissal. Direct destruction bypasses that bookkeeping and cleanup notifications. This is a concrete lifecycle defect consistent with the observed repeated rebuild errors; native confirmation of the correction remains pending.

## Correction in 0.1.4

- If the paint target has an unpinned PAW, invoke stock `UIPartActionController.Deselect(false)` rather than destroying any stock UI object. The stock operation dismisses eligible unpinned menus and handles events/recycling. Pinned menus remain intact.
- Do not interact with PAWs if the active EVA/tool changed. Alt bypasses this dismissal.
- Close/release the paint editor on the active Kerbal's boarding event, vessel change, component disable and teardown. Track the named editor lock so it is also released if a draft is absent.
- Do not force controller activation or change other mods' input locks. No save or marking metadata edits.

A restart for installation discards the old damaged session UI objects; the fix prevents this direct-destruction path from recurring. It does not hot-repair an already running game's menu caches.

## Validation and next game check

Seven production-adapter host assertions cover null/absent/pinned menus, one native-dismiss request, retained recyclable objects, idempotence and reopening. Candidate IL and installed API checks establish that the new path calls native Deselect and contains no direct Destroy call, and that boarding/vessel-change/disable/destroy callbacks close the editor and release its named lock. The stock host implementation is a stand-in, not Unity UI validation.

Total offline checks: 556 host assertions, 511 geometry/orientation assertions (including the local Mk3 fixture) and 27 installed API/config/IL checks: **1,094**. The existing 0.1.3 control changes are included.

After restart/install, paint and Apply/Cancel, unequip the can, reopen several normal part menus, board the vessel and repeat. Also test boarding with a draft, switching vessels, pinned menus and Alt-right-click. Confirm no repeated PAW exceptions. These native results remain pending.
