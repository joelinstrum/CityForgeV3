#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CityForgeV3.World;

public static class PhotographicPalmsReview
{
    private static readonly string[] Ids =
        { "date-palm-tall", "date-palm-short", "la-fan-palm-a", "la-fan-palm-b" };
    private const string DirectoryPath = "Documentation/Validation/photographic-palms-v01/";
    private const string HeightDirectory = "Documentation/Validation/la-palm-heights-v01/";

    public static void VerifyHeightVariants()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (var source in new[] { "la-fan-palm-a", "la-fan-palm-b" })
        {
            var variant = source + "-medium";
            if (FloraFamilies.ForTree(variant) != FloraFamilies.Tropical ||
                !RegionClimateRules.AllowsTree(RegionClimate.Mediterranean, variant))
                throw new Exception("Medium LA palm not plantable: " + variant);
            foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
            {
                var sourcePath = LotWorldController.ResolveFloraResourcePath(source, season);
                var variantPath = LotWorldController.ResolveFloraResourcePath(variant, season);
                if (sourcePath != variantPath ||
                    sourcePath != FloraTreeRepairs.BillboardPath(source, season))
                    throw new Exception("Medium LA palm resource mismatch: " + variant);
                var art = Resources.Load<Texture2D>(sourcePath);
                if (art == null) throw new Exception("Missing LA palm: " + source);
                var sourcePpu = LotWorldController.FloraPixelsPerUnit(source, art.name);
                var variantPpu = LotWorldController.FloraPixelsPerUnit(variant, art.name);
                if (Mathf.Abs(sourcePpu / variantPpu - .75f) > .0001f)
                    throw new Exception("Medium LA palm height ratio: " + variant);
                report += variant + " " + season + ": shared=" + sourcePath +
                    " tallPPU=" + sourcePpu + " mediumPPU=" + variantPpu +
                    " ratio=" + sourcePpu / variantPpu + "\n";
            }
        }
        Directory.CreateDirectory(HeightDirectory);
        File.WriteAllText(HeightDirectory + "resources.txt", report +
            "PASS: two added medium planting IDs share tall artwork and are exactly 75% as tall in every season.\n");
    }

    public static void CaptureHeightVariants() => CaptureIds(
        new[] { "la-fan-palm-a", "la-fan-palm-a-medium",
                "la-fan-palm-b", "la-fan-palm-b-medium" },
        HeightDirectory, "lot-camera-palm-heights.png");

    public static void Verify()
    {
        string report = DateTime.UtcNow.ToString("O") + "\n";
        foreach (var id in Ids)
        {
            var path = LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer);
            var art = Resources.Load<Texture2D>(path);
            if (path != FloraTreeRepairs.BillboardPath(id, SeasonPreset.Summer) ||
                !path.Contains("PhotographicPalmsV01") || art == null ||
                art.width != 1024 || art.height != 1536 ||
                FloraFamilies.ForTree(id) != FloraFamilies.Tropical ||
                !RegionClimateRules.AllowsTree(RegionClimate.Mediterranean, id))
                throw new Exception("Palm resource/family: " + id);
            foreach (SeasonPreset season in Enum.GetValues(typeof(SeasonPreset)))
                if (LotWorldController.ResolveFloraResourcePath(id, season) != path)
                    throw new Exception("Palm evergreen art changed: " + id + " " + season);
            var pixels = art.GetPixels32();
            int foot = -1;
            for (int y = 0; y < art.height && foot < 0; y++)
            for (int x = 350; x < 675; x++)
                if (pixels[y * art.width + x].a > 128) { foot = y; break; }
            var pivot = LotWorldController.FloraPivot(art.name);
            if (Mathf.Abs(foot - pivot.y * art.height) > 1f)
                throw new Exception("Palm trunk contact: " + id);
            float ppu = LotWorldController.FloraPixelsPerUnit(id, art.name);
            if (ppu <= 0f) throw new Exception("Palm scale: " + id);
            report += id + ": path=" + path + " foot=" + foot +
                "px pivot=" + pivot.y * art.height + "px PPU=" + ppu + "\n";
        }
        if (!RegionClimateRules.AllowsTree(RegionClimate.Desert, "date-palm-tall") ||
            !RegionClimateRules.AllowsTree(RegionClimate.Desert, "date-palm-short"))
            throw new Exception("New date palms unavailable in desert");
        var legacy = LotWorldController.ResolveFloraResourcePath("date-palm", SeasonPreset.Summer);
        if (legacy.Contains("PhotographicPalmsV01")) throw new Exception("Legacy date palm changed");
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(DirectoryPath + "resources.txt", report +
            "PASS: four resources, evergreen routing, tropical family, climate eligibility, pivots, legacy date palm unchanged.\n");
    }

    public static void Capture() => CaptureIds(Ids, DirectoryPath, "lot-camera-four-palms.png");

    private static void CaptureIds(string[] ids, string directory, string filename)
    {
        var world = UnityEngine.Object.FindObjectsByType<LotWorldController>(FindObjectsSortMode.None)
            .First(w => w.isActiveAndEnabled);
        var camera = Camera.allCameras.First(c => c.name == "Lot Camera" && c.enabled);
        string before = JsonUtility.ToJson(world.Session.Data);
        var roots = new List<GameObject>();
        var sprites = new List<Sprite>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var material = (Material)typeof(LotWorldController)
            .GetMethod("FloraLitShadowReceiverMaterial", flags).Invoke(world, null);
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var art = Resources.Load<Texture2D>(LotWorldController.ResolveFloraResourcePath(id, SeasonPreset.Summer));
            var sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height),
                LotWorldController.FloraPivot(art.name),
                LotWorldController.FloraPixelsPerUnit(id, art.name));
            var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth * (.32f + .12f * i),
                camera.pixelHeight * .24f));
            if (!new Plane(Vector3.up, world.transform.position + Vector3.up * .02f)
                .Raycast(ray, out var distance)) throw new Exception("Palm preview missed ground");
            var root = new GameObject("Temporary photographic palm " + i);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = ray.GetPoint(distance);
            root.transform.rotation = camera.transform.rotation;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            roots.Add(root);
            sprites.Add(sprite);
        }
        Directory.CreateDirectory(directory);
        var priorTarget = camera.targetTexture;
        var priorActive = RenderTexture.active;
        var target = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24);
        var pixels = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(directory + filename, pixels.EncodeToPNG());
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
            throw new Exception("Palm preview changed lot data");
        File.WriteAllText(directory + "game-review.txt", DateTime.UtcNow.ToString("O") +
            "\nDONE. Four temporary palms rendered then removed; lot JSON unchanged. Current lot lighting, no preview shadows.\n");
    }
}
#endif
