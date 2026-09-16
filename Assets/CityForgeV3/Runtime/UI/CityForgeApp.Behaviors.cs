using CityForgeV3.Behaviors;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void TickDistrictLotBehaviors()
        {
            if (_currentScreen != AppScreen.DistrictTerraform || _districtWorld == null) return;
            _districtWorld.SetLotBehaviorsPaused(_districtSimulationPaused);
        }
        private string _behaviorAnchorId = "";
        private bool _behaviorAnchorIsPickup;
        private void OpenLotBehaviorsModal()
        {
            var panel = CreateDocumentModal("LOT BEHAVIORS", "Start, pause, or re-run the scripts attached to this lot.");
            panel.name = "lot-behaviors-panel";
            panel.AddToClassList("road-material-modal-panel"); panel.AddToClassList("flora-modal-panel");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("flora-modal-scroll"); panel.Add(scroll);
            foreach (var instance in _lotWorld.LotBehaviors)
            {
                var definition = _lotWorld.BehaviorDefinition(instance);
                scroll.Add(StyledLabel(definition?.displayName ?? instance.DefinitionId, "inspector-title"));
                var status = StyledLabel("", "inspector-note"); status.name = "behavior-status-" + instance.InstanceId;
                void UpdateStatus() => status.text = _lotWorld.BehaviorStatus(instance.InstanceId);
                UpdateStatus(); status.schedule.Execute(UpdateStatus).Every(250); scroll.Add(status);
                var actions = new VisualElement(); actions.AddToClassList("inspector-actions");
                var start = CfButton.Create(instance.Enabled ? "PAUSE" : instance.HasStarted ? "RUN" : "START", () =>
                {
                    if (instance.Enabled) { _lotWorld.ToggleBehavior(instance.InstanceId); OpenLotBehaviorsModal(); }
                    else { _lotWorld.RunBehavior(instance.InstanceId); RemoveDocumentModal(); }
                }, true, "primary"); start.name = "behavior-run-" + instance.InstanceId; actions.Add(start);
                var rerun = CfButton.Create("RE-RUN", () => { _lotWorld.RunBehavior(instance.InstanceId, true); RemoveDocumentModal(); }, true, "quiet");
                rerun.name = "behavior-rerun-" + instance.InstanceId; actions.Add(rerun);
                var script = CfButton.Create("{ } SCRIPT", () => OpenBehaviorScriptEditor(_lotWorld.BehaviorScriptSource(instance.InstanceId), instance.InstanceId), true, "quiet");
                script.name = "behavior-script-" + instance.InstanceId; actions.Add(script); scroll.Add(actions);
            }
            if (_lotWorld.LotBehaviors.Count == 0) scroll.Add(StyledLabel("No scripts attached. Add one from the library or import a script.", "inspector-note"));
            var footer = DocumentModalActions();
            footer.Add(CfButton.Create("ADD BEHAVIOR", OpenBehaviorLibrary, true, "quiet"));
            var import = CfButton.Create("IMPORT SCRIPT", () => OpenBehaviorScriptImport(source => OpenBehaviorScriptEditor(source, null)), true, "quiet"); import.name = "import-lot-script"; footer.Add(import);
            footer.Add(CfButton.Create("OBJECTS / IDs", () => OpenLotScriptObjects(OpenLotBehaviorsModal), true, "quiet"));
            footer.Add(CfButton.Create("DONE", () => { RemoveDocumentModal(); ComposeLotEditor(); }, true, "primary")); panel.Add(footer);
        }
        private void OpenBehaviorLibrary()
        {
            var panel = CreateDocumentModal("BEHAVIOR LIBRARY", "Choose a starting routine, then edit its Script.");
            var error = StyledLabel("", "inspector-note"); panel.Add(error);
            foreach (var definition in LotBehaviorCatalog.All)
                panel.Add(CfButton.Create(definition.displayName, () =>
                {
                    if (_lotWorld.AddCargoLoadingBehavior(definition.id)) OpenLotBehaviorsModal();
                    else error.text = "Select an available target object before adding this behavior.";
                }, true, "secondary"));
            panel.Add(CfButton.Create("BACK", OpenLotBehaviorsModal, true, "quiet"));
        }
    }
}
