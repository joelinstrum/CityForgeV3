# Original saltbox import experiment v07

**3D Building Library → Residential → Saltbox — Original Import**

Catalog ID `new-england-saltbox-original-v07` loads the original FBX directly from `Buildings3D/SaltboxOriginalV07/Source`. The supplied archive's FBX and all 28 color images were copied byte-for-byte. No Blender re-export, carved panes, additional material assignments, custom material overrides or lighting controllers are used. Unity generates its default embedded-material representation. The original file already contains 28 segmented Tripo parts; this experiment removes our lighting modifications, not Tripo's original segmentation.

Height normalization is 9.5m, matching the existing saltbox, with catalog pitch0/yaw90. Current lit saltbox and saved placements are retained.

Normal windowed Game-view comparison passed import/appearance inspection: both buildings upright, textured and similarly sized. **Original in foreground; lit derivative in background**. The captured frame predates the corrected front/back status text. The direct original still looks pale in Unity; this experiment does not establish pane edits as the cause of washout. No color correction was added to this control asset.

QA menu: City Forge → QA → Saltbox → Open Original Comparison. Opens an unsaved fixture; no user-owned lot files are saved. User visual preference remains pending.
