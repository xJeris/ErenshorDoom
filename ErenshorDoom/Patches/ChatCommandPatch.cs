using HarmonyLib;
using UnityEngine.UI;

namespace ErenshorDoom.Patches
{
    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    internal static class ChatCommandPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TypeText __instance)
        {
            Text typedField = __instance.typed;
            if (typedField == null || string.IsNullOrEmpty(typedField.text))
                return true;

            string text = typedField.text.ToLower().Trim();

            if (text == "/doom")
            {
                Plugin.Instance.ToggleDoom();
                return false; // skip original CheckCommands — CheckInput handles cleanup
            }

            return true;
        }
    }
}
