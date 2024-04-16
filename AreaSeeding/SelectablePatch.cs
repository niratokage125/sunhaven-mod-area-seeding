using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Wish;
using static AreaSeeding.SeedsPatch;

namespace AreaSeeding
{
    [HarmonyPatch(typeof(Selectable<Crop>))]
    public static class SelectablePatch
    {
        private static GameObject baseSelection;
        private static List<GameObject> selectionList;

        [HarmonyPatch("LateUpdate"), HarmonyPrefix]
        public static bool LateUpdate_Prefix(Selectable<Crop> __instance)
        {
            if (__instance is Fertilizer &&
                Plugin.modEnabled.Value &&
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

                for (int i = 0; i + 1 < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldc_R4 &&
                        code[i].OperandIs(2.0f) &&
                        code[i + 1].opcode == OpCodes.Stloc_0)
                    {
                        code[i].operand = 1000.0f;
                    }
                }

                {
                    var tmp = new List<CodeInstruction>();
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i].OperandIs(AccessTools.Method(typeof(Selectable<Crop>), "SetSelectionOnDecoration")))
                        {
                            code[i].operand = AccessTools.Method(typeof(SelectablePatch), nameof(MySetSelectionOnDecoration));
                        }
                        tmp.Add(code[i]);
                        if (code[i].OperandIs(AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit")))
                        {
                            var c = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SelectablePatch), nameof(MyAlwaysTrue)));
                            tmp.Add(c);
                        }
                    }
                    code = tmp;
                }

                return code;
            }
            _ = Transpiler(null);
        }

        public static bool MyAlwaysTrue(bool value)
        {
            return true;
        }
        public static void MySetSelectionOnDecoration(Selectable<Crop> instance, Decoration decoration)
        {
            var traverse = Traverse.Create(instance);
            var _selection = traverse.Field<GameObject>("_selection").Value;
            var posF = traverse.Field<Vector2>("pos").Value;
            var pos = new Vector2Int((int)posF.x, (int)posF.y);
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
                Crop crop;
                if(!SingletonBehaviour<GameManager>.Instance.TryGetObject<Crop>(new Vector3Int(p.x, p.y, 0), out crop))
                {
                    item.SetActive(false);
                    continue;
                }
                MySetSelectionOnDecorationBody(new MySetSelectionOnDecorationBodyArg { _selection = item }, crop);
                item.gameObject.transform.position += new Vector3(0f, 0.0001f * i, 0.0001f * i);
            }
        }
        public class MySetSelectionOnDecorationBodyArg
        {
            public GameObject _selection;
        }
        [HarmonyPatch("SetSelectionOnDecoration"), HarmonyReversePatch]
        public static void MySetSelectionOnDecorationBody(object instance, Decoration decoration)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i + 1 < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldarg_0 &&
                        code[i + 1].OperandIs(AccessTools.Field(typeof(Selectable<Crop>), "_selection")))
                    {
                        code[i + 1].operand = AccessTools.Field(typeof(MySetSelectionOnDecorationBodyArg), nameof(MySetSelectionOnDecorationBodyArg._selection));
                    }
                }

                return code;
            }
            _ = Transpiler(null);
        }
    }
}
