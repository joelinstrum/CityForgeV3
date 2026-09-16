# Prop to native-building selection

Removed the viewport condition that admitted native-building hits only with
None/Building selected. Placement-tool priority remains unchanged. A successful
native-building click now clears the previous prop/flora highlight and drag state
locally, without DeselectAll or a full presentation rebuild.

Updated the old source guard test (which enforced the now-unwanted restriction)
and added a barn → farmhouse → barn interaction regression test.
Unity compiled the changes in the isolated review project. Static guard and
highlight-cleanup checks and git diff --check passed. The runtime regression is
not executed yet: the review project is in the user's active Play session, so
no test fixture replaced it and no stop/reload was forced. No progress saved.

Follow-up: all six selection checks, including the live-world barn → farmhouse → barn regression, passed in the isolated batch project during district-lot construction validation. See `../district-lot-construction-site-v01/results.txt`.
