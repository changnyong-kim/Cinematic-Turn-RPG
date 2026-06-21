using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TileGridGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject _tilePrefab;

    [SerializeField]
    private Transform _parent;

    [SerializeField]
    private int _xCount = 6;

    [SerializeField]
    private int _zCount = 6;

    [SerializeField]
    private Vector2 _tileSize = new Vector2(4f, 4f);

    [SerializeField]
    private Vector3 _startPosition;

    [SerializeField]
    private bool _centerAlign = true;

    [ContextMenu("Generate Tiles")]
    private void GenerateTiles()
    {
        if (_tilePrefab == null)
        {
            Debug.LogError("[TileGridGenerator] Tile Prefab is null.");
            return;
        }

        Transform targetParent = _parent != null ? _parent : transform;

        ClearChildren(targetParent);

        Vector3 origin = _startPosition;

        if (_centerAlign)
        {
            origin.x -= (_xCount - 1) * _tileSize.x * 0.5f;
            origin.z -= (_zCount - 1) * _tileSize.y * 0.5f;
        }

        for (int z = 0; z < _zCount; z++)
        {
            for (int x = 0; x < _xCount; x++)
            {
                Vector3 position = origin + new Vector3(
                    x * _tileSize.x,
                    0f,
                    z * _tileSize.y);

#if UNITY_EDITOR
                GameObject tile = PrefabUtility.InstantiatePrefab(_tilePrefab, targetParent) as GameObject;
#else
                GameObject tile = Instantiate(_tilePrefab, targetParent);
#endif

                if (tile == null)
                {
                    continue;
                }

                tile.transform.localPosition = position;
                tile.transform.localRotation = Quaternion.identity;
            }
        }
    }

    [ContextMenu("Clear Tiles")]
    private void ClearTiles()
    {
        Transform targetParent = _parent != null ? _parent : transform;
        ClearChildren(targetParent);
    }

    private void ClearChildren(Transform targetParent)
    {
        for (int i = targetParent.childCount - 1; i >= 0; i--)
        {
            Transform child = targetParent.GetChild(i);

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }
    }
}