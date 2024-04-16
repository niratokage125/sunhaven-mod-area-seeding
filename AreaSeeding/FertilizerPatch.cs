using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using static AreaSeeding.SeedsPatch;
using UnityEngine;
using Wish;
using static AreaSeeding.SelectablePatch;

namespace AreaSeeding
{
    [HarmonyPatch(typeof(Fertilizer))]
    public static class FertilizerPatch
    {
        [HarmonyPatch("Use1"), HarmonyPrefix]
        public static bool Use1_Prefix(Fertilizer __instance)
        {
            if (Plugin.modEnabled.Value &&
                Plugin.activeKey.Value.IsPressed())
            {
                var traverse = Traverse.Create(__instance);
                var posF = traverse.Field<Vector2>("pos").Value;
                var pos = new Vector2Int((int)posF.x, (int)posF.y);
                var width = Plugin.width.Value;
                var height = Plugin.height.Value;

                var slotIndex = Player.Instance.ItemIndex;
                if (slotIndex < 0 || Player.Instance.Inventory.Items.Count <= slotIndex)
                {
                    return false;
                }

                var id = Player.Instance.Inventory.Items[slotIndex].id;
                if (Plugin.prevAction == "Use1F" &&
                    Plugin.prevPos == pos &&
                    Plugin.prevId == id)
                {
                    return false;
                }
                Plugin.prevAction = "Use1F";
                Plugin.prevPos = pos;
                Plugin.prevId = id;

                var count = width * height;
                for (int i = 0; i < count; i++)
                {
                    if (slotIndex != Player.Instance.ItemIndex || Player.Instance.Inventory.Items[slotIndex].amount <= 0)
                    {
                        return false;
                    }

                    int x = i % width - width / 2;
                    int y = -i / width + height / 2;
                    var p = new Vector2Int((int)pos.x + x, (int)pos.y + y);
                    Crop crop;
                    if (!SingletonBehaviour<GameManager>.Instance.TryGetObject<Crop>(new Vector3Int(p.x, p.y, 0), out crop))
                    {
                        continue;
                    }
                    if (crop.FertilizeFromPlayer(__instance.fertilizerType))
                    {
                        Player.Instance.Inventory.RemoveItemAt(Player.Instance.ItemIndex, 1);
                        __instance.HoldAnimation();
                    }
                }
                return false;
            }
            return true;
        }
    }
}
