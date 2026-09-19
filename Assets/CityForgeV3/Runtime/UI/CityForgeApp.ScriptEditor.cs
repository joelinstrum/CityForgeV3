using System;
using System.IO;
using System.Linq;
using CityForgeV3.Behaviors;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        private void OpenBehaviorScriptEditor(string source, string editingId)
        {
            var panel = CreateDocumentModal("{ } SCRIPT", "Edit the JSON behavior script. Apply updates this lot and leaves the behavior ready to Start.");
            panel.name = "lot-script-editor";
            panel.AddToClassList("road-material-modal-panel"); panel.AddToClassList("flora-modal-panel");
            panel.style.width = Length.Percent(94); panel.style.maxWidth = 1500;
            var message = StyledLabel("", "inspector-note"); message.name = "script-validation"; panel.Add(message);
            try { message.text = _lotWorld.DescribeBehaviorScript(source); }
            catch (Exception e) { message.text = e.Message; }
            foreach (var heading in panel.Children()) heading.style.flexShrink = 0;
            var area = new ScrollView(ScrollViewMode.Vertical)
            { name = "lot-script-scroll", verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible };
            area.style.flexGrow = 1; area.style.flexBasis = 0; area.style.minHeight = 0;
            area.style.marginBottom = 12; area.style.overflow = Overflow.Hidden;
            area.contentViewport.style.overflow = Overflow.Hidden; panel.Add(area);
            var code = new TextField
            { multiline = true, value = source, name = "lot-script-source", verticalScrollerVisibility = ScrollerVisibility.Hidden };
            code.style.whiteSpace = WhiteSpace.Normal; code.style.fontSize = 24;
            code.style.flexShrink = 0; area.Add(code);
            // Give the editable document its full height. The outer view alone
            // owns wheel/scrollbar input, so the TextField cannot swallow it.
            void SizeDocument()
            {
                var columns = Mathf.Max(20, Mathf.FloorToInt((area.contentViewport.resolvedStyle.width - 40) / 15f));
                var lines = code.value.Split('\n').Sum(line => Mathf.Max(1, Mathf.CeilToInt(line.Length / (float)columns)));
                code.style.height = Mathf.Max(64, lines * 32f + 32);
            }
            code.RegisterValueChangedCallback(_ => SizeDocument());
            area.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => SizeDocument());
            code.RegisterCallback<WheelEvent>(evt =>
            { area.scrollOffset += new Vector2(0, evt.delta.y * 32); evt.StopPropagation(); }, TrickleDown.TrickleDown);
            area.verticalScroller.style.width = 22; area.verticalScroller.style.minWidth = 22;
            area.verticalScroller.style.backgroundColor = new Color(.12f, .19f, .22f);
            var thumb = area.verticalScroller.Q<VisualElement>("unity-dragger");
            if (thumb != null) thumb.style.backgroundColor = new Color(.64f, .68f, .57f);
            var input = code.Q<VisualElement>("unity-text-input");
            input.style.backgroundColor = new Color(.045f, .075f, .09f); input.style.color = new Color(.9f, .91f, .84f);
            input.style.paddingLeft = 14; input.style.paddingRight = 14; input.style.paddingTop = 12; input.style.paddingBottom = 12;
            var tools = new VisualElement(); tools.AddToClassList("inspector-actions");
            tools.Add(CfButton.Create("OBJECTS / IDs", () => OpenLotScriptObjects(() => OpenBehaviorScriptEditor(code.value, editingId)), true, "quiet"));
            tools.Add(CfButton.Create("IMPORT", () => OpenBehaviorScriptImport(text => OpenBehaviorScriptEditor(text, editingId), () => OpenBehaviorScriptEditor(code.value, editingId)), true, "quiet"));
            tools.Add(CfButton.Create("COPY SCRIPT", () => { GUIUtility.systemCopyBuffer = code.value; message.text = "Script copied."; }, true, "quiet"));
            tools.style.flexShrink = 0; panel.Add(tools);
            var footer = DocumentModalActions(); footer.style.flexShrink = 0;
            var validate = CfButton.Create("CHECK", () =>
            { try { message.text = "Valid. " + _lotWorld.ValidateBehaviorScript(code.value, editingId); } catch (Exception e) { message.text = e.Message; } }, true, "quiet");
            validate.name = "check-lot-script"; footer.Add(validate);
            var apply = CfButton.Create("APPLY", () =>
            {
                try { _lotWorld.ApplyBehaviorScript(code.value, editingId); OpenLotBehaviorsModal(); }
                catch (Exception e) { message.text = e.Message; }
            }, true, "primary"); apply.name = "apply-lot-script"; footer.Add(apply);
            if (editingId != null) footer.Add(CfButton.Create("REMOVE", () => { _lotWorld.RemoveBehavior(editingId); OpenLotBehaviorsModal(); }, true, "danger"));
            footer.Add(CfButton.Create("CANCEL", OpenLotBehaviorsModal, true, "quiet")); panel.Add(footer);
        }
        private void OpenLotScriptObjects(Action back)
        {
            var panel = CreateDocumentModal("LOT OBJECTS", "Copy a placed object's ID into the script. IDs remain stable when objects move or the lot is saved.");
            panel.name = "lot-script-objects"; panel.AddToClassList("flora-modal-panel");
            var search = new TextField { name = "lot-object-search" }; search.label = "Search"; panel.Add(search);
            var status = StyledLabel("", "inspector-note"); panel.Add(status);
            var scroll = new ScrollView(); scroll.AddToClassList("flora-modal-scroll"); panel.Add(scroll);
            var objects = _lotWorld.ScriptObjects();
            void Fill()
            {
                scroll.Clear();
                foreach (var item in objects.Where(x => string.IsNullOrWhiteSpace(search.value) ||
                    (x.Kind + " " + x.Name + " " + x.Id).IndexOf(search.value, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    scroll.Add(StyledLabel(item.Kind + " · " + item.Name, "inspector-title"));
                    var id = new TextField { value = item.Id, isReadOnly = true }; id.style.fontSize = 18; scroll.Add(id);
                    scroll.Add(CfButton.Create("COPY ID", () => { GUIUtility.systemCopyBuffer = item.Id; status.text = "Copied " + item.Name; }, true, "quiet"));
                }
                if (scroll.childCount == 0) scroll.Add(StyledLabel("No matching objects.", "inspector-note"));
            }
            search.RegisterValueChangedCallback(_ => Fill()); Fill();
            panel.Add(CfButton.Create("BACK", back, true, "primary"));
        }
        private void OpenBehaviorScriptImport(Action<string> loaded, Action cancel = null, Action<string> validate = null)
        {
            var panel = CreateDocumentModal("IMPORT SCRIPT", "Choose a .json behavior script. It opens for review before Apply.");
            panel.name = "lot-script-import"; panel.AddToClassList("road-material-modal-panel"); panel.AddToClassList("flora-modal-panel");
            var location = new TextField("File or folder") { value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), name = "script-import-path" };
            panel.Add(location); var error = StyledLabel("", "inspector-note"); panel.Add(error);
            var scroll = new ScrollView(); scroll.AddToClassList("flora-modal-scroll"); panel.Add(scroll);
            void Open(string path)
            {
                try
                {
                    path = Path.GetFullPath(path.Trim());
                    if (Directory.Exists(path))
                    {
                        location.value = path; scroll.Clear(); error.text = "";
                        var parent = Directory.GetParent(path); if (parent != null) scroll.Add(CfButton.Create("↑ PARENT FOLDER", () => Open(parent.FullName), true, "secondary"));
                        var entries = Directory.EnumerateFileSystemEntries(path).Where(p => Directory.Exists(p) || p.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).OrderBy(p => !Directory.Exists(p)).ThenBy(Path.GetFileName).Take(150).ToArray();
                        foreach (var entry in entries) scroll.Add(CfButton.Create((Directory.Exists(entry) ? "▸ " : "{ } ") + Path.GetFileName(entry), () => Open(entry), true, "secondary"));
                        if (entries.Length == 0) error.text = "No JSON scripts in this folder.";
                    }
                    else
                    {
                        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose a .json behavior script.");
                        if (new FileInfo(path).Length > LotScriptCodec.MaxCharacters * 4) throw new ArgumentException("Script file is too large.");
                        var text = File.ReadAllText(path);
                        if (validate != null) validate(text);
                        else _lotWorld.DescribeBehaviorScript(text);
                        loaded(text);
                    }
                }
                catch (Exception e) { error.text = e.Message; }
            }
            var footer = DocumentModalActions();
            var open = CfButton.Create("OPEN", () => Open(location.value), true, "primary"); open.name = "open-script-file"; footer.Add(open);
            footer.Add(CfButton.Create("CANCEL", cancel ?? OpenLotBehaviorsModal, true, "quiet")); panel.Add(footer); Open(location.value);
        }
    }
}
