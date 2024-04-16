using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UnityEngine;
using Wish;
using ZeroFormatter.DynamicObjectSegments.Wish;
using Sirenix.OdinInspector;

namespace AreaSeeding
{
    [HarmonyPatch(typeof(Seeds))]
    public static class SeedsPatch
    {
        private static GameObject baseSelection;
        private static List<GameObject> selectionList;

        [HarmonyPatch("LateUpdate"), HarmonyPrefix]
        public static bool LateUpdate_Prefix(Seeds __instance)
        {
            if (Plugin.modEnabled.Value &&
                Plugin.activeKey.Value.IsPressed())
            {
                MyLateUpdate(__instance);
                return false;
            }
            else
            {
                selectionList?.ForEach(x => x.SetActive(false));
            }
            return true;
        }
        [HarmonyPatch("LateUpdate"), HarmonyReversePatch]
        public static void MyLateUpdate(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i + 2 < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Callvirt &&
                        code[i].OperandIs(AccessTools.PropertyGetter(typeof(Player), nameof(Player.ExactGraphicsPosition))) &&
                        code[i + 2].OperandIs(1.5f))
                    {
                        code[i + 2].operand = 1000.0f;
                    }
                }

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Callvirt &&
                        code[i].OperandIs(AccessTools.Method(typeof(TileManager), nameof(TileManager.IsHoedOrWatered), new[] { typeof(Vector2Int) })))
                    {
                        code[i].operand = AccessTools.Method(typeof(SeedsPatch), nameof(MyIsHoedOrWatered));
                    }
                    else if (code[i].opcode == OpCodes.Call &&
                               code[i].OperandIs(AccessTools.Method(typeof(Seeds), "SetSelectionOnTile", new[] { typeof(Vector2Int) })))
                    {
                        code[i].operand = AccessTools.Method(typeof(SeedsPatch), nameof(MySetSelectionOnTile));
                    }
                }

                return code;
            }
            _ = Transpiler(null);
        }
        public static bool MyIsHoedOrWatered(object instance, Vector2Int pos)
        {
            return true;
        }
        public static void MySetSelectionOnTile(object instance, Vector2Int pos)
        {
            Seeds seeds = (Seeds)instance;
            var traverse = Traverse.Create(seeds);
            var _selection = traverse.Field<GameObject>("_selection").Value;
            if (_selection == null)
            {
                return;
            }
            var width = Plugin.width.Value;
            var height = Plugin.height.Value;

            if (baseSelection != _selection || selectionList?.Count != width * height)
            {
                baseSelection = _selection;
                selectionList?.ForEach(x => UnityEngine.Object.Destroy(x));
                selectionList?.Clear();
                selectionList = new List<GameObject>();
                for (int i = 0; i < width * height; i++)
                {
                    var item = UnityEngine.Object.Instantiate<GameObject>(_selection);
                    item.SetActive(false);
                    selectionList.Add(item);
                }
            }
            _selection.SetActive(false);
            for (int i = 0; i < selectionList.Count; i++)
            {
                int x = i % width - width / 2;
                int y = -i / width + height / 2;
                var item = selectionList[i];
                var p = new Vector2Int(pos.x + x, pos.y + y);
                MySetSelectionOnTileBody(new MySetSelectionOnTileBodyArg { _selection = item, _transform = seeds.transform }, p);
                item.gameObject.transform.position += new Vector3(0f, 0.0001f * i, 0.0001f * i);
            }
        }
        public class MySetSelectionOnTileBodyArg
        {
            public GameObject _selection;
            public Transform _transform;
            public Transform transform { get { return _transform; } }
        }
        [HarmonyPatch("SetSelectionOnTile", new[] { typeof(Vector2Int) }), HarmonyReversePatch]
        public static void MySetSelectionOnTileBody(object instance, Vector2Int pos)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i + 1 < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldarg_0)
                    {
                        if (code[i + 1].OperandIs(AccessTools.Field(typeof(Seeds), "_selection")))
                        {
                            code[i + 1].operand = AccessTools.Field(typeof(MySetSelectionOnTileBodyArg), nameof(MySetSelectionOnTileBodyArg._selection));
                        }
                        else if (code[i + 1].OperandIs(AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))))
                        {
                            code[i + 1].operand = AccessTools.PropertyGetter(typeof(MySetSelectionOnTileBodyArg), nameof(MySetSelectionOnTileBodyArg.transform));
                        }
                    }
                }

                return code;
            }
            _ = Transpiler(null);
        }

        [HarmonyPatch("OnDisable"), HarmonyPrefix]
        public static bool OnDisable_Prefix(Seeds __instance)
        {
            if (GameManager.ApplicationQuitting || GameManager.SceneTransitioning)
            {
                return true;
            }
            var _selection = Traverse.Create(__instance).Field<GameObject>("_selection").Value;
            if (baseSelection == _selection)
            {
                baseSelection = null;
                selectionList?.ForEach(x => UnityEngine.Object.Destroy(x));
                selectionList?.Clear();
            }
            return true;
        }

        [HarmonyPatch("Use1"), HarmonyPrefix]
        public static bool Use1_Prefix(Seeds __instance)
        {
            if (Plugin.modEnabled.Value &&
                Plugin.activeKey.Value.IsPressed())
            {
                var traverse = Traverse.Create(__instance);
                var _crop = traverse.Field<Decoration>("_crop").Value;
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
                if (Plugin.prevAction == "Use1" &&
                    Plugin.prevPos == pos &&
                    Plugin.prevId == id)
                {
                    return false;
                }
                Plugin.prevAction = "Use1";
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
                    var prev = new Vector2Int(p.x + 1, p.y + 1);
                    SeedsPatch.MyUse1(new MyUse1Arg { _seedItem = _seedItem, pos = p, prevPos = prev, _crop = _crop, original = __instance });
                    
                    //SeedsPatch.MyUse1(__instance);
                }
                return false;
            }

            return true;
        }
        public class MyUse1Arg
        {
            public SeedData _seedItem;
            public SeedData SeedItem { get { return _seedItem; } }
            public Vector2Int pos;
            public Vector2Int prevPos;
            public Decoration _crop;
            public Seeds original;
            public Transform transform {  get { return original.transform; } }
            public void HoldAnimation()
            {
                original.HoldAnimation();
            }
        }
        [HarmonyPatch("Use1"), HarmonyReversePatch]
        public static void MyUse1(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].OperandIs(AccessTools.PropertyGetter(typeof(Seeds), "SeedItem")))
                    {
                        code[i].operand = AccessTools.PropertyGetter(typeof(MyUse1Arg), nameof(MyUse1Arg.SeedItem));
                    }
                    else if (code[i].OperandIs(AccessTools.Field(typeof(Seeds), "pos")))
                    {
                        code[i].operand = AccessTools.Field(typeof(MyUse1Arg), nameof(MyUse1Arg.pos));
                    }
                    else if (code[i].OperandIs(AccessTools.Field(typeof(Seeds), "prevPos")))
                    {
                        code[i].operand = AccessTools.Field(typeof(MyUse1Arg), nameof(MyUse1Arg.prevPos));
                    }
                    else if (code[i].OperandIs(AccessTools.Field(typeof(Seeds), "_crop")))
                    {
                        code[i].operand = AccessTools.Field(typeof(MyUse1Arg), nameof(MyUse1Arg._crop));
                    }
                    else if (code[i].OperandIs(AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))))
                    {
                        code[i].operand = AccessTools.PropertyGetter(typeof(MyUse1Arg), nameof(MyUse1Arg.transform));
                    }
                    else if (code[i].OperandIs(AccessTools.Method(typeof(Seeds), nameof(Seeds.HoldAnimation))))
                    {
                        code[i].operand = AccessTools.Method(typeof(MyUse1Arg), nameof(MyUse1Arg.HoldAnimation));
                    }
                    else if (code[i].OperandIs(AccessTools.Method(typeof(AudioManager), nameof(AudioManager.PlayAudio), new[] { typeof(AudioClip), typeof(float), typeof(float)})))
                    {
                        code[i].operand = AccessTools.Method(typeof(SeedsPatch), nameof(MyPlayAudio));
                    }
                }

                return code;
            }
            _ = Transpiler(null);
        }
        public static void MyPlayAudio(object instance, AudioClip clip, float volume, float delay)
        {
            if (!Plugin.playAudio)
            {
                return;
            }
            Plugin.playAudio = false;
            AudioManager.Instance.PlayAudio(clip,volume,delay);
        }
    }
}
