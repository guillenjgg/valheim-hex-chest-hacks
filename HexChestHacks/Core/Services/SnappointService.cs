using UnityEngine;

namespace HexChestHacks.Core.Services
{
    internal static class SnappointService
    {
        internal static void AddSnappoints(ZNetScene zNetScene)
        {
            foreach (var prefab in zNetScene.m_prefabs)
            {
                if (prefab == null || !Constants.IsSupportedChest(prefab.name))
                {
                    continue;
                }

                var chest = prefab.GetComponent<Piece>();

                if (chest == null)
                {
                    continue;
                }

                // Treasure chests do not have a Container component.
                if (prefab.name != Constants.TreasureChestPrefab && prefab.GetComponent<Container>() == null)
                {
                    continue;
                }

                DisableSupportDamage(prefab);

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
                AddVerticalSnappoints(chest.transform, bounds);
            }
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

        // Create two additional snap points for stacking chests vertically.
        private static void AddVerticalSnappoints(Transform root, Bounds bounds)
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

        // This allows chests to be stacked ontop of each other
        private static void DisableSupportDamage(GameObject prefab)
        {
            var wearNTear = prefab.GetComponent<WearNTear>();

            if (wearNTear != null)
            {
                wearNTear.m_noSupportWear = false;
                wearNTear.m_supports = true;
            }
        }
    }
}