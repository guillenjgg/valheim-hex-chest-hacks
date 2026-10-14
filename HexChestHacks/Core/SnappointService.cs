using System.Collections.Generic;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class SnappointService
    {
        private const string BarrelPrefab = "piece_chest_barrel";
        private const string TreasureChestPrefab = "piece_chest_treasure";

        private static readonly HashSet<string> SupportedPrefabs = new HashSet<string>
        {
            "piece_chest_wood",
            "piece_chest",
            "piece_chest_blackmetal",
            "piece_chest_grausten",
            "piece_chest_private",
            "piece_chest_barrel",
            "piece_chest_warderobe",
            "piece_chest_treasure"
        };

        internal static bool IsSupportedChest(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            var name = prefabName.Replace("(Clone)", "").Trim();

            return SupportedPrefabs.Contains(name);
        }

        internal static void AddSnappoints(ZNetScene zNetScene)
        {
            var added = 0;

            foreach (var prefab in zNetScene.m_prefabs)
            {
                if (prefab == null || !IsSupportedChest(prefab.name))
                {
                    continue;
                }

                var chest = prefab.GetComponent<Piece>();

                if (chest == null)
                {
                    Plugin.Log.LogWarning($"No Piece component found: {prefab.name}");
                    continue;
                }

                // Treasure chests do not have a Container component.
                if (prefab.name != TreasureChestPrefab && prefab.GetComponent<Container>() == null)
                {
                    Plugin.Log.LogWarning($"No Container component found: {prefab.name}");
                    continue;
                }

                Plugin.Log.LogInfo($"Supported storage prefab: {chest.name}");

                if (HasSnappoints(chest.transform))
                {
                    continue;
                }

                if (!TryGetMeshBounds(chest.transform, out var bounds))
                {
                    Plugin.Log.LogWarning($"No visible meshes found: {chest.name}");
                    continue;
                }

                AddChestSnappoints(chest.transform, bounds);

                // Barrels can also snap vertically to stack on top of each other.
                if (prefab.name == BarrelPrefab)
                {
                    AddBarrelSnappoints(chest.transform, bounds);
                }

                Plugin.Log.LogInfo($"Added snap points to {chest.name}.");
                added++;
            }

            Plugin.Log.LogInfo($"Configured {added} storage prefabs.");
        }

        // Avoid adding duplicate snap points to pieces that already have them.
        private static bool HasSnappoints(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.CompareTag("snappoint"))
                {
                    return true;
                }
            }

            return false;
        }

        // Create four snap points at the corners of the chest's.
        private static void AddChestSnappoints(Transform root, Bounds bounds)
        {
            CreateSnappoint(root, "top_left", new Vector3(bounds.min.x, 0f, bounds.max.z));
            CreateSnappoint(root, "top_right", new Vector3(bounds.max.x, 0f, bounds.max.z));
            CreateSnappoint(root, "bottom_left", new Vector3(bounds.min.x, 0f, bounds.min.z));
            CreateSnappoint(root, "bottom_right", new Vector3(bounds.max.x, 0f, bounds.min.z));
        }

        // Create two additional snap points for stacking barrels vertically.
        private static void AddBarrelSnappoints(Transform root, Bounds bounds)
        {
            CreateSnappoint(root, "top_center", new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            CreateSnappoint(root, "bottom_center", new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        }

        // I had to reference the Unity API documenation to figure out how to calculate the bounds
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.html
        private static bool TryGetMeshBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            var found = false;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                // Ignore inactive meshes, such as an open chest lid.
                if (!IsMeshActive(meshFilter.transform, root))
                {
                    continue;
                }

                // Only include meshes that are actually rendered.
                var renderer = meshFilter.GetComponent<MeshRenderer>();

                if (renderer == null || !renderer.enabled || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Bounds meshBounds = meshFilter.sharedMesh.bounds;

                // A 3D box has eight corners.
                // We must account for mesh rotation, position, and scale.
                var meshCorners = new Vector3[]
                {
                    new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.min.z),
                    new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
                    new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
                    new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
                    new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
                    new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
                    new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
                    new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.max.z)
                };

                Matrix4x4 meshToChest = root.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;

                foreach (var meshCorner in meshCorners)
                {
                    var chestLocalPosition = meshToChest.MultiplyPoint3x4(meshCorner);

                    // Start with the first corner, then expand to include the others.
                    if (!found)
                    {
                        bounds = new Bounds(chestLocalPosition, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(chestLocalPosition);
                    }
                }
            }

            return found;
        }

        // Check whether a mesh and its parents are enabled.
        // Ignore the prefab root because Valheim may keep prefabs inactive.
        private static bool IsMeshActive(Transform transform, Transform root)
        {
            while (transform != root)
            {
                if (!transform.gameObject.activeSelf)
                {
                    return false;
                }

                transform = transform.parent;

                if (transform == null)
                {
                    return false;
                }
            }

            return true;
        }

        // Valheim discovers snap points by their "snappoint" tag.
        private static void CreateSnappoint(Transform root, string name, Vector3 position)
        {
            var snappoint = new GameObject($"$hud_snappoint_{name}");

            snappoint.transform.SetParent(root, false);
            snappoint.transform.localPosition = position;

            snappoint.tag = "snappoint";
            snappoint.layer = LayerMask.NameToLayer("piece");
        }
    }
}