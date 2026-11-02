# Catch-up kits

One folder per lab that later labs lean on. A kit is the smallest piece of working plumbing
that lets you carry on — written for no particular theme, and deliberately plain.

**What a kit is for.** You missed a session, or didn't finish it, and next week's lab needs the
thing you didn't build. Copy the kit's files into `src/Capstone.Core/CatchUp/` and keep going.

**What a kit is not.** It is not that lab's answer, and it earns you nothing for it: the contract
tests know which namespace it lives in. The lab stays *not started*, and a wiring class of your
own that hands back the kit's objects is reported by name.

**To get those points back**, write your own version for your own theme, stop using the kit, and
be verified in one of the last two sessions.

This folder is managed — `sq update` replaces it, so don't edit it in place.