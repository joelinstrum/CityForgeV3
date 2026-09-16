# Brickworks persistence in the isolated Review editor

Cause: the Review-only `district9` helper loaded Testy from ReviewUserData/CityForge/Regions but set `_districtUndoQaSaveRoot` to ReviewScratch. District edits were saved to the temporary folder while normal region loads used the other folder. The Brickworks remained in the scratch JSON.

Fix: `district9` now loads using RegionSaveStore.Load with its normal default and clears the testing save override. Synthetic QA fixtures still explicitly use ReviewScratch. No production serialization change was necessary.

Recovery: backed up both JSON saves under the Review project's RecoveryBackups/brickworks-save-path-20260916, then restored the missing Brickworks by stable ID into the current Testy region. Existing region content was preserved. The Review's normal save root remains isolated from the main Unity project.

Validation: Unity compilation passed. The recovered Brickworks is serialized and visibly selectable; normal file reload, leaving and reentering District 9, and a complete Play-mode stop/restart each passed. See attached logs. The Review editor is back at Testy / District 9.
