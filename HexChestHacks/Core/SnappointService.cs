using System.Collections.Generic;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class SnappointService
    {
        private static readonly HashSet<string> SupportedChestPrefabs = new HashSet<string>
        {
            "piece_chest_wood"
        };

        internal static List<Piece> SupportedChests { get; } = new List<Piece>();

        internal static List<Piece> GetSupportedChests(ZNetScene zNetScene)
        {
            SupportedChests.Clear();

            foreach (var prefab in zNetScene.m_prefabs)
            {
                if (prefab == null || !SupportedChestPrefabs.Contains(prefab.name))
                {
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                var container = prefab.GetComponent<Container>();

                if (piece == null || container == null)
                {
                    continue;
                }

                SupportedChests.Add(piece);
            }

            Plugin.Log.LogInfo($"Supported chest prefabs found: {SupportedChests.Count}");

            return SupportedChests;
        }

        internal static void AddSnappoints(ZNetScene zNetScene)
        {
            var chests = GetSupportedChests(zNetScene);

            foreach (var chest in chests)
            {
                var existingSnappoints = new List<Transform>();
                chest.GetSnapPoints(existingSnappoints);

                if (existingSnappoints.Count > 0)
                {
                    Plugin.Log.LogInfo($"Skipping {chest.name}: already has snap points.");
                    continue;
                }

                if (!TryGetMeshBounds(chest, out var bounds))
                {
                    Plugin.Log.LogWarning($"Skipping {chest.name}: no visible meshes found.");
                    continue;
                }

                Plugin.Log.LogInfo(
                    $"Chest: {chest.name} | Bounds Min: {bounds.min} | Bounds Max: {bounds.max}");

                CreateSnappoint(chest, "$hud_snappoint_top_left",
                    new Vector3(bounds.min.x, 0f, bounds.max.z));

                CreateSnappoint(chest, "$hud_snappoint_top_right",
                    new Vector3(bounds.max.x, 0f, bounds.max.z));

                CreateSnappoint(chest, "$hud_snappoint_bottom_left",
                    new Vector3(bounds.min.x, 0f, bounds.min.z));

                CreateSnappoint(chest, "$hud_snappoint_bottom_right",
                    new Vector3(bounds.max.x, 0f, bounds.min.z));

                Plugin.Log.LogInfo($"Added 4 snap points to {chest.name}.");
            }
        }

        private static bool TryGetMeshBounds(Piece chest, out Bounds bounds)
        {
            bounds = new Bounds();
            var found = false;

            var meshFilters = chest.GetComponentsInChildren<MeshFilter>(true);

            foreach (var meshFilter in meshFilters)
            {
                if (!meshFilter.gameObject.activeSelf)
                {
                    continue;
                }

                var renderer = meshFilter.GetComponent<MeshRenderer>();

                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var mesh = meshFilter.sharedMesh;

                if (mesh == null)
                {
                    continue;
                }

                var meshBounds = mesh.bounds;

                for (var x = 0; x < 2; x++)
                {
                    for (var y = 0; y < 2; y++)
                    {
                        for (var z = 0; z < 2; z++)
                        {
                            var corner = new Vector3(
                                x == 0 ? meshBounds.min.x : meshBounds.max.x,
                                y == 0 ? meshBounds.min.y : meshBounds.max.y,
                                z == 0 ? meshBounds.min.z : meshBounds.max.z);

                            var worldPosition = meshFilter.transform.TransformPoint(corner);
                            var localPosition = chest.transform.InverseTransformPoint(worldPosition);

                            if (!found)
                            {
                                bounds = new Bounds(localPosition, Vector3.zero);
                                found = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localPosition);
                            }
                        }
                    }
                }
            }

            return found;
        }

        private static void CreateSnappoint(Piece chest, string name, Vector3 position)
        {
            var snappoint = new GameObject(name);

            snappoint.transform.SetParent(chest.transform, false);
            snappoint.transform.localPosition = position;
            snappoint.transform.localRotation = Quaternion.identity;

            snappoint.tag = "snappoint";
            snappoint.layer = LayerMask.NameToLayer("piece");

            Plugin.Log.LogInfo($"  {name}: {position}");
        }
    }
}
