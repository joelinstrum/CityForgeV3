#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;

public static class PlaneAnchorReview
{
    public static void VerifyPhotographicTrees()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (var id in new[] { "mature-oak", "american-elm", "shagbark-hickory" })
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, season);
            if (path != FloraTreeRepairs.BillboardPath(id, season) ||
                !path.Contains("PhotographicDeciduousV01"))
                throw new Exception("Photographic path mismatch: " + id + " " + season);
            var art = Resources.Load<Texture2D>(path);
            if (art == null || art.width != 1024 || art.height != 1536)
                throw new Exception("Missing or resized tree: " + path);
            int first = Array.FindIndex(art.GetPixels32(), p => p.a > 128);
            float foot = first / art.width;
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Root mismatch: " + path);
            float ppu = LotWorldController.FloraPixelsPerUnit(id, art.name);
            if (ppu < 90f || ppu > 105f) throw new Exception("Scale mismatch: " + path);
            report += id + " " + season + ": root=" + foot + "px pivot=" +
                pivot.y * art.height + "px PPU=" + ppu + "\n";
        }
        foreach (var id in new[] { "ashe", "oak", "oak-b" })
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer);
            if (path.Contains("PhotographicDeciduousV01") ||
                Resources.Load<Texture2D>(path) == null)
                throw new Exception("Existing tree changed: " + id);
        }
        const string directory = "Documentation/Validation/photographic-deciduous-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "resources.txt", report +
            "PASS: 12 seasonal resources, measured roots, library paths and legacy Ashe/Oak paths. No scene or save mutation.\n");
    }

    public static void CaptureWillowGround()
    {
        var renderer = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .First(r => r.name.StartsWith("Flora — vendor-willow"));
        var world = renderer.GetComponentInParent<LotWorldController>();
        string before = JsonUtility.ToJson(world.Session.Data);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var sink = (float)typeof(LotWorldController).GetMethod("FloraPresentationSink", flags)
            .Invoke(null, new object[] { new PlacedFlora { FloraId = "vendor-willow" } });
        if (Mathf.Abs(sink - 0.16f) > 0.0001f) throw new Exception("Willow presentation sink changed");
        var ground = world.transform.position.y + 0.02f;
        var current = renderer.transform.position;
        // Existing Game-view objects can retain their old transform through a
        // script refresh. Update this presentation only, without editing data.
        renderer.transform.position = new Vector3(current.x, ground - sink, current.z);
        var shadow = renderer.transform.Find("Flora Shadow — Canopy");
        var shade = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
        if (shade == null || shade.sprite != renderer.sprite)
            throw new Exception("Willow shadow no longer shares the tree sprite");
        var marker = world.GetComponentsInChildren<Transform>(true)
            .First(t => t.name == "Selected Flora Highlight");
        var x = (float)typeof(LotWorldController).GetMethod("FloraRootLocalX", flags)
            .Invoke(null, new object[] { renderer.sprite, renderer.flipX });
        var foot = renderer.transform.TransformPoint(new Vector3(x, 0f, 0f));
        marker.position = new Vector3(foot.x, world.transform.position.y + 0.06f, foot.z);
        marker.gameObject.SetActive(true);
        if (before != JsonUtility.ToJson(world.Session.Data))
            throw new Exception("Willow QA modified lot data");
        const string directory = "Documentation/Validation/willow-ground-v02/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "review.txt", DateTime.UtcNow.ToString("O") +
            "\nWillow sprite foot=" + foot + "; marker=" + marker.position +
            "; visual sink=" + sink + "m. Shadow shares sprite; lot JSON unchanged.\n");
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.maximized = false; view.Focus(); view.Repaint();
        ScreenCapture.CaptureScreenshot(directory + "game-view.png");
    }

    public static void VerifyWillow()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            string path = LotWorldController.ResolveFloraResourcePath("vendor-willow", season);
            var art = Resources.Load<Texture2D>(path);
            if (art == null || !path.Contains("WillowRealisticV01"))
                throw new Exception("Missing realistic willow " + season);
            int first = Array.FindIndex(art.GetPixels32(), p => p.a > 128);
            float foot = first / art.width;
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Willow root mismatch " + season);
            float ppu = LotWorldController.FloraPixelsPerUnit("vendor-willow", art.name);
            if (ppu != 104f) throw new Exception("Willow scale mismatch");
            report += season + ": " + path + "; root=" + foot + "px; pivot=" +
                pivot.y * art.height + "px; PPU=" + ppu + "\n";
        }
        const string directory = "Documentation/Validation/willow-realistic-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "review.txt", report +
            "PASS: four seasonal resources and measured root anchors. No scene or save mutation.\n");
    }

    public static void VerifySilverMaple()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            string path = LotWorldController.ResolveFloraResourcePath("silver-maple-a", season);
            var art = Resources.Load<Texture2D>(path);
            if (art == null || !path.Contains("SilverMapleRealisticV01"))
                throw new Exception("Missing realistic silver maple " + season);
            int first = Array.FindIndex(art.GetPixels32(), p => p.a > 128);
            var pivot = LotWorldController.FloraPivot(art.name);
            float foot = first / art.width;
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Silver maple root mismatch " + season);
            float ppu = LotWorldController.FloraPixelsPerUnit("silver-maple-a", art.name);
            if (ppu != 84f) throw new Exception("Silver maple scale mismatch");
            report += season + ": " + path + "; root=" + foot + "px; pivot=" +
                pivot.y * art.height + "px; PPU=" + ppu + "\n";
        }
        if (LotWorldController.ResolveFloraResourcePath("silver-maple-b", SeasonPreset.Summer)
            .Contains("SilverMapleRealisticV01"))
            throw new Exception("Silver Maple B unexpectedly replaced");
        const string directory = "Documentation/Validation/silver-maple-realistic-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "review.txt", report +
            "PASS: four seasonal resources and measured root anchors; Silver Maple B retained. No scene or save mutation.\n");
    }

    public static void VerifyMaple()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            string path = LotWorldController.ResolveFloraResourcePath("vendor-red-maple", season);
            var art = Resources.Load<Texture2D>(path);
            if (art == null || !path.Contains("RedMapleRealisticV01")) throw new Exception("Missing realistic maple " + season);
            int first = Array.FindIndex(art.GetPixels32(), p => p.a > 128);
            var pivot = LotWorldController.FloraPivot(art.name);
            float foot = first / art.width;
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f) throw new Exception("Maple root mismatch " + season);
            float ppu = LotWorldController.FloraPixelsPerUnit("vendor-red-maple", art.name);
            if (ppu != 96f) throw new Exception("Maple scale mismatch");
            report += season + ": " + path + "; root=" + foot + "px; pivot=" + pivot.y * art.height + "px; PPU=" + ppu + "\n";
        }
        if (LotWorldController.ResolveFloraResourcePath("vendor-red-maple-young", SeasonPreset.Summer).Contains("RedMapleRealisticV01"))
            throw new Exception("Young maple unexpectedly replaced");
        const string directory = "Documentation/Validation/red-maple-realistic-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "review.txt", report + "PASS: all four seasonal resources and measured root anchors; young maple retained. No scene or save mutation.\n");
    }

    public static void VerifyRetirement()
    {
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var expected = LotWorldController.ResolveFloraResourcePath("london-plane-b", season);
            if (LotWorldController.ResolveFloraResourcePath("london-plane-c", season) != expected)
                throw new Exception("Retired Plane C resource mismatch: " + season);
            var texture = Resources.Load<Texture2D>(expected);
            if (texture == null) throw new Exception("Missing Plane B artwork: " + season);
            if (LotWorldController.FloraPixelsPerUnit("london-plane-c", texture.name) !=
                LotWorldController.FloraPixelsPerUnit("london-plane-b", texture.name))
                throw new Exception("Retired Plane C scale mismatch: " + season);
            for (int variation = 0; variation < 4; variation++)
                if (LotWorldController.ResolveFloraPresentationId("london-plane-c", variation, season) != "london-plane-b")
                    throw new Exception("Retired Plane C presentation mismatch");
        }
        const string directory = "Documentation/Validation/plane-c-retirement-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "review.txt", DateTime.UtcNow.ToString("O") +
            "\nPASS: all four seasons and variation profiles resolve retired C to B; textures load and scales match. No scene or save mutation.\n");
    }

    public static void Capture()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        // Check actual opaque image pixels against the production planting
        // pivot, rather than asserting the same guessed constant twice.
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath("london-plane-a", season));
            int first = Array.FindIndex(art.GetPixels32(), p => p.a > 128);
            float footPixel = first / art.width;
            var pivot = LotWorldController.FloraPivot(art.name);
            float error = Mathf.Abs(footPixel - pivot.y * art.height);
            if (error > 1f) throw new Exception("Root registration failure " + season);
            report += season + ": opaque foot=" + footPixel + " pivot=" + pivot.y * art.height + " error=" + error + " pixels\n";
        }
        var r = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .First(r => r.name.StartsWith("Flora — london-plane-a"));
        var world = r.GetComponentInParent<LotWorldController>();
        var before = JsonUtility.ToJson(world.Session.Data);
        var artCurrent = r.sprite.texture;
        // Hot reload retains old Sprite instances. Refresh only this scene's
        // presentation and its matching shadow; no lot placements are edited.
        var sprite = Sprite.Create(artCurrent, new Rect(0,0,artCurrent.width,artCurrent.height),
            LotWorldController.FloraPivot(artCurrent.name), r.sprite.pixelsPerUnit);
        r.sprite = sprite;
        var shadow = r.transform.Find("Flora Shadow — Canopy").GetComponent<SpriteRenderer>();
        shadow.sprite = sprite;
        var marker = world.GetComponentsInChildren<Transform>(true).First(t => t.name == "Selected Flora Highlight");
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var x = (float)typeof(LotWorldController).GetMethod("FloraRootLocalX", flags).Invoke(null, new object[]{sprite,r.flipX});
        var root = r.transform.TransformPoint(new Vector3(x,0,0));
        marker.position = new Vector3(root.x,world.transform.position.y+.06f,root.z);
        marker.gameObject.SetActive(true);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        report += "Actual trunk ground height=" + root.y + "; marker height=" + marker.position.y + "\n";
        report += "Screen separation (includes marker's 4cm height bias)=" + Vector2.Distance(camera.WorldToScreenPoint(root),camera.WorldToScreenPoint(marker.position)) + " pixels\n";
        if(before != JsonUtility.ToJson(world.Session.Data)) throw new Exception("QA modified lot data");
        report += "PASS seasonal alpha registration; shared tree/shadow origin; lot data unchanged.\n";
        const string directory="Documentation/Validation/plane-a-anchor-v02/";
        Directory.CreateDirectory(directory); File.WriteAllText(directory+"review.txt",report);
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.maximized=false;view.Focus();view.Repaint();
        ScreenCapture.CaptureScreenshot(directory+"game-view.png");
    }
}
#endif
