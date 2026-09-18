#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;

public static class PhotographicTreesReview
{
    public static void VerifyMediumConifers()
    {
        var ids = new[] { "medium-balsam-fir", "medium-fraser-fir", "medium-blue-spruce" };
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (var id in ids)
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, season);
            var art = Resources.Load<Texture2D>(path);
            bool winter = season == SeasonPreset.Winter;
            if (path != FloraTreeRepairs.BillboardPath(id, season) ||
                !path.Contains("MediumConifersV01") ||
                !path.EndsWith(winter ? "-snowy" : "-snowfree") ||
                art == null || art.width != 1024 || art.height != 1536 ||
                FloraFamilies.ForTree(id) != FloraFamilies.Mountain)
                throw new Exception("Medium conifer resource: " + id + " " + season);
            var pixels = art.GetPixels32();
            int foot = -1;
            for (int y = 0; y < art.height && foot < 0; y++)
            for (int x = 350; x < 675; x++)
                if (pixels[y * art.width + x].a > 128) { foot = y; break; }
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Medium conifer root: " + id + " " + season);
            float expectedPpu = id == "medium-blue-spruce" ? 120f : 128f;
            if (LotWorldController.FloraPixelsPerUnit(id, art.name) != expectedPpu)
                throw new Exception("Medium conifer scale: " + id);
            report += id + " " + season + ": " + path + " foot=" + foot +
                "px pivot=" + pivot.y * art.height + "px PPU=" + expectedPpu + "\n";
        }
        const string directory = "Documentation/Validation/medium-conifers-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "resources.txt", report +
            "PASS: three species, six textures, four-season paths, mountain category and ground pivots.\n");
    }

    public static void CaptureMediumConifers()
    {
        var world = UnityEngine.Object.FindObjectsByType<LotWorldController>(FindObjectsSortMode.None)
            .First(w => w.isActiveAndEnabled);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        string before = JsonUtility.ToJson(world.Session.Data);
        var ids = new[] { "medium-balsam-fir", "medium-fraser-fir", "medium-blue-spruce" };
        var roots = new List<GameObject>();
        var renderers = new List<SpriteRenderer>();
        var sprites = new List<Sprite>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var material = (Material)typeof(LotWorldController)
            .GetMethod("FloraLitShadowReceiverMaterial", flags).Invoke(world, null);
        for (int i = 0; i < ids.Length; i++)
        {
            var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth * (.25f + .25f * i),
                camera.pixelHeight * .27f));
            if (!new Plane(Vector3.up, world.transform.position + Vector3.up * .02f)
                .Raycast(ray, out var distance)) throw new Exception("Conifer preview missed ground");
            var root = new GameObject("Temporary medium conifer " + i);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = ray.GetPoint(distance);
            root.transform.rotation = camera.transform.rotation;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            roots.Add(root);
            renderers.Add(renderer);
        }
        const string directory = "Documentation/Validation/medium-conifers-v01/";
        Directory.CreateDirectory(directory);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var target = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24);
        var pixels = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (var season in new[] { SeasonPreset.Summer, SeasonPreset.Winter })
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(ids[i], season));
                    var sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height),
                        LotWorldController.FloraPivot(art.name),
                        LotWorldController.FloraPixelsPerUnit(ids[i], art.name));
                    renderers[i].sprite = sprite;
                    sprites.Add(sprite);
                }
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(directory + "lot-camera-" + season.ToString().ToLowerInvariant() + ".png",
                    pixels.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
        }
        if (before != JsonUtility.ToJson(world.Session.Data))
            throw new Exception("Conifer preview changed lot data");
        File.WriteAllText(directory + "game-review.txt", DateTime.UtcNow.ToString("O") +
            "\nDONE. Three temporary conifers rendered snow-free and snowy, then removed; lot JSON unchanged. Current lot lighting, no preview shadows.\n");
    }

    public static void VerifyCypressMossPair()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (var id in new[] { "bald-cypress-moss", "bald-cypress-moss-b" })
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, season);
            var art = Resources.Load<Texture2D>(path);
            if (path != FloraTreeRepairs.BillboardPath(id, season) ||
                art == null || art.width != 1024 || art.height != 1536)
                throw new Exception("Cypress pair resource: " + id + " " + season);
            var pixels = art.GetPixels32();
            int foot = -1;
            for (int y = 0; y < art.height && foot < 0; y++)
            for (int x = 350; x < 675; x++)
                if (pixels[y * art.width + x].a > 128) { foot = y; break; }
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f ||
                LotWorldController.FloraPixelsPerUnit(id, art.name) != 82f)
                throw new Exception("Cypress pair anchor: " + id + " " + season);
            report += id + " " + season + ": root=" + foot + "px pivot=" +
                pivot.y * art.height + "px PPU=82\n";
        }
        const string directory = "Documentation/Validation/bald-cypress-moss-v02/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "resources.txt", report +
            "PASS: both planting IDs and eight resources align with measured trunk roots.\n");
    }

    public static void CaptureCypressMossPair()
    {
        var world = UnityEngine.Object.FindObjectsByType<LotWorldController>(FindObjectsSortMode.None)
            .First(w => w.isActiveAndEnabled);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        string before = JsonUtility.ToJson(world.Session.Data);
        var ids = new[] { "bald-cypress-moss", "bald-cypress-moss-b" };
        var roots = new List<GameObject>();
        var renderers = new List<SpriteRenderer>();
        var sprites = new List<Sprite>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var material = (Material)typeof(LotWorldController)
            .GetMethod("FloraLitShadowReceiverMaterial", flags).Invoke(world, null);
        for (int i = 0; i < ids.Length; i++)
        {
            var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth * (.36f + .28f * i),
                camera.pixelHeight * .24f));
            if (!new Plane(Vector3.up, world.transform.position + Vector3.up * .02f)
                .Raycast(ray, out var distance)) throw new Exception("Cypress pair missed ground");
            var root = new GameObject("Temporary cypress pair " + i);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = ray.GetPoint(distance);
            root.transform.rotation = camera.transform.rotation;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            roots.Add(root);
            renderers.Add(renderer);
        }
        const string directory = "Documentation/Validation/bald-cypress-moss-v02/";
        Directory.CreateDirectory(directory);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var target = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24);
        var pixels = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(ids[i], season));
                    var sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height),
                        LotWorldController.FloraPivot(art.name),
                        LotWorldController.FloraPixelsPerUnit(ids[i], art.name));
                    renderers[i].sprite = sprite;
                    sprites.Add(sprite);
                }
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(directory + "lot-camera-" + season.ToString().ToLowerInvariant() + ".png",
                    pixels.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
        }
        if (before != JsonUtility.ToJson(world.Session.Data))
            throw new Exception("Cypress pair preview changed lot data");
        File.WriteAllText(directory + "game-review.txt", DateTime.UtcNow.ToString("O") +
            "\nDONE. Two temporary cypresses rendered in four seasons, then removed; lot JSON unchanged. Current lot lighting, no preview shadows.\n");
    }

    public static void VerifyCypressMoss()
    {
        const string id = "bald-cypress-moss";
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, season);
            if (path != FloraTreeRepairs.BillboardPath(id, season) ||
                !path.Contains("BaldCypressMossV01")) throw new Exception("Cypress path: " + season);
            var art = Resources.Load<Texture2D>(path);
            if (art == null || art.width != 1024 || art.height != 1536)
                throw new Exception("Cypress resource: " + season);
            var pixels = art.GetPixels32();
            int foot = -1;
            for (int y = 0; y < art.height && foot < 0; y++)
            for (int x = 350; x < 675; x++)
                if (pixels[y * art.width + x].a > 128) { foot = y; break; }
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Cypress root: " + season + " " + foot);
            if (LotWorldController.FloraPixelsPerUnit(id, art.name) != 82f)
                throw new Exception("Cypress scale: " + season);
            report += season + ": " + path + "; root=" + foot + "px; pivot=" +
                pivot.y * art.height + "px; PPU=82\n";
        }
        const string directory = "Documentation/Validation/bald-cypress-moss-v01/";
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory + "resources.txt", report +
            "PASS: four seasonal resources and measured trunk contact; no save mutation.\n");
    }

    public static void CaptureCypressMoss()
    {
        const string id = "bald-cypress-moss";
        var world = UnityEngine.Object.FindObjectsByType<LotWorldController>(FindObjectsSortMode.None)
            .First(w => w.isActiveAndEnabled);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        string before = JsonUtility.ToJson(world.Session.Data);
        var path = LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer);
        var art = Resources.Load<Texture2D>(path);
        var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth * .5f,
            camera.pixelHeight * .26f));
        if (!new Plane(Vector3.up, world.transform.position + Vector3.up * .02f)
            .Raycast(ray, out var distance)) throw new Exception("Cypress preview missed ground");
        var root = new GameObject("Temporary cypress moss preview");
        root.hideFlags = HideFlags.DontSave;
        root.transform.position = ray.GetPoint(distance);
        root.transform.rotation = camera.transform.rotation;
        var renderer = root.AddComponent<SpriteRenderer>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        renderer.sharedMaterial = (Material)typeof(LotWorldController)
            .GetMethod("FloraLitShadowReceiverMaterial", flags).Invoke(world, null);
        var sprites = new List<Sprite>();
        const string directory = "Documentation/Validation/bald-cypress-moss-v01/";
        Directory.CreateDirectory(directory);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var target = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24);
        var pixels = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
            {
                art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(id, season));
                var sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height),
                    LotWorldController.FloraPivot(art.name),
                    LotWorldController.FloraPixelsPerUnit(id, art.name));
                renderer.sprite = sprite;
                sprites.Add(sprite);
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(directory + "lot-camera-" + season.ToString().ToLowerInvariant() + ".png",
                    pixels.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
        }
        if (before != JsonUtility.ToJson(world.Session.Data))
            throw new Exception("Cypress preview changed lot data");
        File.WriteAllText(directory + "game-review.txt", DateTime.UtcNow.ToString("O") +
            "\nDONE. Temporary cypress rendered in four seasons then removed; lot JSON unchanged. Current lot lighting, no preview shadow.\n");
    }

    public static void Capture()
    {
        foreach (var id in new[] { "mature-oak", "american-elm", "shagbark-hickory" })
        {
            var stale = GameObject.Find("Temporary photo tree review — " + id);
            if (stale != null) UnityEngine.Object.DestroyImmediate(stale);
        }
        var world = UnityEngine.Object.FindObjectsByType<LotWorldController>(FindObjectsSortMode.None)
            .First(w => w.isActiveAndEnabled);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        var before = JsonUtility.ToJson(world.Session.Data);
        var roots = new List<GameObject>();
        var renderers = new List<SpriteRenderer>();
        var sprites = new List<Sprite>();
        var ids = new[] { "mature-oak", "american-elm", "shagbark-hickory" };
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var material = (Material)typeof(LotWorldController)
            .GetMethod("FloraLitShadowReceiverMaterial", flags).Invoke(world, null);
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer));
            var sprite = Sprite.Create(art, new Rect(0f, 0f, art.width, art.height),
                LotWorldController.FloraPivot(art.name), LotWorldController.FloraPixelsPerUnit(id, art.name));
            var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth * (0.26f + 0.24f * i),
                camera.pixelHeight * 0.20f));
            if (!new Plane(Vector3.up, world.transform.position + Vector3.up * 0.02f)
                .Raycast(ray, out var distance)) throw new Exception("Lot preview ray missed ground");
            var root = new GameObject("Temporary photo tree review — " + id);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = ray.GetPoint(distance);
            root.transform.rotation = camera.transform.rotation;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            roots.Add(root);
            renderers.Add(renderer);
            sprites.Add(sprite);
        }
        const string directory = "Documentation/Validation/photographic-deciduous-v01/";
        Directory.CreateDirectory(directory);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var target = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24);
        var pixels = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(ids[i], season));
                    var sprite = Sprite.Create(art, new Rect(0f, 0f, art.width, art.height),
                        LotWorldController.FloraPivot(art.name),
                        LotWorldController.FloraPixelsPerUnit(ids[i], art.name));
                    renderers[i].sprite = sprite;
                    sprites.Add(sprite);
                }
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(directory + "lot-camera-" + season.ToString().ToLowerInvariant() + ".png",
                    pixels.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            foreach (var root in roots) if (root != null) UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites) if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
        }
        if (before != JsonUtility.ToJson(world.Session.Data))
            throw new Exception("Photographic tree preview changed lot data");
        File.WriteAllText(directory + "game-review.txt", DateTime.UtcNow.ToString("O") +
            "\nDONE. Three temporary tree presentations rendered in all four seasons with the active lot camera then removed; lot JSON unchanged. These texture previews use the current lot lighting.\n");
    }
}
#endif
