using HarmonyLib;

namespace HexChestHacks.Core.ReflectionAccessors
{
    internal static class ContainerAccessor
    {
        private static readonly AccessTools.FieldRef<Container, Inventory> InventoryField =
            AccessTools.FieldRefAccess<Container, Inventory>("m_inventory");

        private static readonly AccessTools.FieldRef<Container, ZNetView> NetViewField =
            AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<Container, int> WidthField =
            AccessTools.FieldRefAccess<Container, int>("m_width");

        private static readonly AccessTools.FieldRef<Container, int> HeightField =
            AccessTools.FieldRefAccess<Container, int>("m_height");

        private static readonly AccessTools.FieldRef<Container, uint> LastRevisionField =
            AccessTools.FieldRefAccess<Container, uint>("m_lastRevision");

        private static readonly AccessTools.FieldRef<Container, bool> InUseField =
            AccessTools.FieldRefAccess<Container, bool>("m_inUse");

        internal static Inventory GetInventory(Container container) => container.GetInventory();

        internal static ZNetView GetNetView(Container container) => NetViewField(container);
        
        internal static int GetWidth(Container container) => WidthField(container);

        internal static int GetHeight(Container container) => HeightField(container);

        internal static uint GetLastRevision(Container container) => LastRevisionField(container);

        internal static bool GetInUse(Container container) => InUseField(container);

        internal static void SetWidth(Container container, int width) => WidthField(container) = width;

        internal static void SetHeight(Container container, int height) => HeightField(container) = height;
    }
}