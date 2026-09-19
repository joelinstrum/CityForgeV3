using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed class LotObjectReference
    {
        public string Id, Kind, Name;
        public Vector3 Position;
        public bool HasPosition;
        internal object Source;
        internal FieldInfo IdField;
    }
    public static class LotObjectRegistry
    {
        // Walk the persisted collections, including components and circulation graphs.
        // IDs belong to placed objects, never to the shared asset/prefab.
        public static List<LotObjectReference> Read(LotSaveData data)
        {
            var result = new List<LotObjectReference>();
            if (data == null) return result;
            void Visit(object value, string kind)
            {
                if (value == null) return;
                var type = value.GetType();
                var id = type.GetField("InstanceId") ?? type.GetField("Id");
                if (id?.FieldType == typeof(string))
                {
                    var label = new[] { "AssetId", "BuildingId", "PropId", "FloraId", "EffectId", "TextureId", "ConnectorId", "WaterId", "ComponentId", "DefinitionId", "PackageId" }
                        .Select(n => type.GetField(n)?.GetValue(value) as string).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                    var item = new LotObjectReference { Id = (string)id.GetValue(value), Kind = kind, Name = label ?? kind, Source = value, IdField = id };
                    switch (value)
                    {
                        case PlacedProp p: item.Position = new Vector3(p.PositionX, 0, p.PositionZ); item.HasPosition = true; break;
                        case PlacedFlora p: item.Position = new Vector3(p.PositionX, -p.SinkDepthMeters, p.PositionZ); item.HasPosition = true; break;
                        case PlacedBuilding3D p: item.Position = new Vector3(p.X, 0, p.Z); item.HasPosition = true; break;
                        case PlacedBuilding p: item.Position = new Vector3(p.CellX, 0, p.CellZ); item.HasPosition = true; break;
                        case PlacedEffect p: item.Position = new Vector3(p.PositionX, p.PositionY, p.PositionZ); item.HasPosition = !p.HasHostAttachment; break;
                        case PlacedDecal p: item.Position = new Vector3(p.PositionX, 0, p.PositionZ); item.HasPosition = true; break;
                        case CirculationNode p: item.Position = new Vector3(p.PositionMeters.x, p.ElevationMeters, p.PositionMeters.y); item.HasPosition = true; break;
                        case PlacedRoadPiece p: item.Position = new Vector3(p.GridX * 10 + 5, 0, p.GridZ * 10 + 5); item.HasPosition = true; break;
                        case PlacedLotConnector p:
                            var access = LotWorldController.ConnectorAccess(p,
                                data.LotWidthCells * 10,
                                data.LotDepthCells * 10);
                            item.Position = new Vector3(access.Inside.x, 0,
                                access.Inside.y);
                            item.HasPosition = true;
                            break;
                    }
                    result.Add(item);
                }
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var child = field.GetValue(value);
                    if (child is IList list) foreach (var element in list)
                    {
                        if (element != null && !element.GetType().IsValueType && element is not string) Visit(element, field.Name);
                    }
                    else if (child is CirculationNetwork) Visit(child, field.Name);
                }
            }
            Visit(data, "Lot"); return result;
        }
        public static void EnsureIds(LotSaveData data)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in Read(data))
            {
                if (!string.IsNullOrWhiteSpace(item.Id) && seen.Add(item.Id)) continue;
                var id = Guid.NewGuid().ToString("N"); item.IdField.SetValue(item.Source, id); seen.Add(id);
            }
        }
        public static Vector3 ResolvePoint(LotSaveData data, CityForgeV3.Behaviors.LotScriptPoint point)
        {
            if (string.IsNullOrWhiteSpace(point.objectId)) return point.offset;
            var item = Read(data).Find(x => x.Id == point.objectId);
            if (item == null) throw new ArgumentException("Object ID not found: " + point.objectId);
            if (!item.HasPosition) throw new ArgumentException("This object cannot be used as a location: " + point.objectId);
            // Script offsets attached to a placed prop rotate with that prop.
            // This keeps a dock point on a barge when district placement turns
            // the hull to follow the river.
            if (item.Source is PlacedProp prop)
                return item.Position + Quaternion.Euler(0,
                    prop.RotationQuarterTurns * 90f, 0) * point.offset;
            return item.Position + point.offset;
        }
    }
}
