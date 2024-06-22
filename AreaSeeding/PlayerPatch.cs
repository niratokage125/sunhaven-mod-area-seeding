using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wish;

namespace AreaSeeding
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        [HarmonyPatch("Update"), HarmonyPostfix]
        public static void Update_Postfix()
        {
            if (!Plugin.modEnabled.Value)
            {
                return;
            }
            if (Plugin.increaseWidthKey.Value.IsDown())
            {
                Plugin.width.Value = Math.Min(Plugin.width.Value + 1, Plugin.MaxLength);
            }
            if (Plugin.increaseHeightKey.Value.IsDown())
            {
                Plugin.height.Value = Math.Min(Plugin.height.Value + 1, Plugin.MaxLength);
            }
            if (Plugin.decreaseWidthKey.Value.IsDown())
            {
                Plugin.width.Value = Math.Max(Plugin.width.Value - 1, 1);
            }
            if (Plugin.decreaseHeightKey.Value.IsDown())
            {
                Plugin.height.Value = Math.Max(Plugin.height.Value - 1, 1);
            }
            if (Plugin.rotateKey.Value.IsDown())
            {
                var tmp = Plugin.width.Value;
                Plugin.width.Value = Plugin.height.Value;
                Plugin.height.Value = tmp;
            }
        }
    }
}
