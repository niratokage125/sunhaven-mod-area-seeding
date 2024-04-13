using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wish;
using static AreaSeeding.SeedsPatch;

namespace AreaSeeding
{
    [HarmonyPatch(typeof(UseItem))]
    public static class UseItemPatch
    {
        [HarmonyPatch("Use2"), HarmonyPrefix]
        public static bool Use2_Prefix(UseItem __instance)
        {
            if (__instance is Seeds &&
                Plugin.modEnabled.Value &&
                Plugin.activeKey.Value.IsPressed())
            {
                var traverse = Traverse.Create(__instance);
                var _crop = traverse.Field<Decoration>("_crop").Value as Crop;
                var _seedItem = traverse.Property<SeedData>("SeedItem").Value;
                if (_crop == null || _seedItem == null)
                {
                    return false;
                }

                var pos = traverse.Field<Vector2Int>("pos").Value;
                var width = Plugin.width.Value;
                var height = Plugin.height.Value;

                var slotIndex = Player.Instance.ItemIndex;
                if (slotIndex < 0 || Player.Instance.Inventory.Items.Count <= slotIndex)
                {
                    return false;
                }

                var id = Player.Instance.Inventory.Items[slotIndex].id;
                if (Plugin.prevAction == "Use2" &&
                    Plugin.prevPos == pos &&
                    Plugin.prevId == id)
                {
                    return false;
                }
                Plugin.prevAction = "Use2";
                Plugin.prevPos = pos;
                Plugin.prevId = id;
                Plugin.playAudio = true;

                var count = width * height;
                for (int i = 0; i < count; i++)
                {
                    if (slotIndex != Player.Instance.ItemIndex || Player.Instance.Inventory.Items[slotIndex].amount <= 0)
                    {
                        return false;
                    }
                    int x = i % width - width / 2;
                    int y = -i / width + height / 2;
                    var p = new Vector2Int(pos.x + x, pos.y + y);
                    Crop crop;
                    if (!SingletonBehaviour<GameManager>.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(p.x * 6, p.y * 6, 0), out crop))
                    {
                        continue;
                    }
                    if (!Plugin.destroyOtherCrops.Value && crop.SeedData.id != _crop.SeedData.id)
                    {
                        continue;
                    }
                    switch (Plugin.growthStageOption.Value)
                    {
                        case Plugin.GrowthStageOptions.None:
                            continue;
                        case Plugin.GrowthStageOptions.Day1Only:
                            if (0 < Traverse.Create(crop).Method("TimePassedSincePlanted").GetValue<int>())
                            {
                                continue;
                            }
                            break;
                        case Plugin.GrowthStageOptions.NotFullyGrown:
                            if (crop.CheckGrowth)
                            {
                                continue;
                            }
                            break;
                        case Plugin.GrowthStageOptions.All:
                            break;
                    }
                    crop.DropSeeds();
                    crop.DestroyCrop();
                }
                return false;
            }

            return true;
        }
    }
}
