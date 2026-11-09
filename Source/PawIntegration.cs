namespace KSPGraffitiArtist
{
    public static class PawIntegration
    {
        public static void DismissForPaint(UIPartActionController controller,Part target)
        {
            if(controller==null || target==null)return;
            UIPartActionWindow window=controller.ItemListGet(target);
            if(window==null || window.Pinned)return;
            // KSP fires onPartActionUIDismiss and moves windows into its reusable hidden list.
            // Destroying a PAW GameObject bypasses controller and mod-owned group cleanup.
            controller.Deselect(false);
        }
    }
}
