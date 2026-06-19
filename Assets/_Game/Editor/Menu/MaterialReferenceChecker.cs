using UnityEditor;
using UnityEngine;

public static class MaterialReferenceChecker
{
    [MenuItem("Tools/Check Selected Material References In Scene")]
    private static void CheckSelectedMaterialReferencesInScene()
    {
        Material targetMaterial = Selection.activeObject as Material;

        if (targetMaterial == null)
        {
            Debug.LogError("Material을 선택한 상태에서 실행하세요.");
            return;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int rendererCount = 0;
        int slotCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i] != targetMaterial)
                {
                    continue;
                }

                rendererCount++;
                slotCount++;

                Debug.Log(
                    $"[Material Ref] {renderer.name} / Slot {i}",
                    renderer.gameObject);
            }
        }

        Debug.Log(
            $"[Material Ref Result] {targetMaterial.name} - Renderer Count: {rendererCount}, Slot Count: {slotCount}");
    }
}