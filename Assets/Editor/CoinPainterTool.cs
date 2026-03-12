#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CoinPainterTool : EditorWindow
{
    private enum PaintMode
    {
        Paint,
        Erase
    }

    private PaintMode _mode = PaintMode.Paint;

    private GameObject _coinPrefab;
    private float _gridSize = 1f;
    private float _gridZ = 0f;
    private Transform _parent;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private bool _isPainting;
    private bool _isActive;
    private Vector3 _hoveredCell;
    private bool _hoveredCellValid;

    private readonly Dictionary<Vector3, GameObject> _placedCoins = new();

    private static readonly Color ColorGrid = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColorHoverOk = new(1f, 0.9f, 0f, 0.85f);
    private static readonly Color ColorHoverOccupied = new(1f, 0.3f, 0.3f, 0.85f);
    private static readonly Color ColorErase = new(1f, 0.2f, 0.2f, 0.85f);
    private static readonly Color ColorPlaced = new(0.4f, 1f, 0.4f, 0.5f);

    [MenuItem("Tools/Coin Painter")]
    public static void Open()
    {
        var w = GetWindow<CoinPainterTool>("Coin Painter");
        w.minSize = new Vector2(260f, 320f);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Coin Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _coinPrefab = (GameObject)
            EditorGUILayout.ObjectField("Coin Prefab", _coinPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        _gridSize = Mathf.Max(0.25f, EditorGUILayout.FloatField("Cell Size", _gridSize));
        _gridZ = EditorGUILayout.FloatField("Z Position", _gridZ);

        EditorGUILayout.Space(4);
        _parent = (Transform)
            EditorGUILayout.ObjectField("Parent (opcional)", _parent, typeof(Transform), true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Modo", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _mode == PaintMode.Paint ? Color.yellow : Color.white;
        if (GUILayout.Button("✏  Pintar"))
            _mode = PaintMode.Paint;
        GUI.backgroundColor = _mode == PaintMode.Erase ? new Color(1f, 0.4f, 0.4f) : Color.white;
        if (GUILayout.Button("✕  Borrar"))
            _mode = PaintMode.Erase;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // ── Toggle activación ─────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        bool wantActive = GUILayout.Toggle(
            _isActive,
            _isActive ? "⬛  Tool ACTIVA  (click en SceneView)" : "▷  Activar Tool",
            "Button",
            GUILayout.Height(32)
        );

        if (wantActive != _isActive)
            SetActive(wantActive);

        if (_isActive)
        {
            EditorGUILayout.HelpBox(
                "LMB: pintar/borrar  |  Shift+LMB: modo opuesto temporal  |  Esc: desactivar",
                MessageType.Info
            );
        }

        // ── Acciones ──────────────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = _placedCoins.Count > 0;
        if (GUILayout.Button("Undo todo"))
            UndoAll();
        if (GUILayout.Button("Limpiar registro"))
            _placedCoins.Clear();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            $"Monedas en registro: {_placedCoins.Count}",
            EditorStyles.miniLabel
        );

        if (_coinPrefab == null)
            EditorGUILayout.HelpBox("Asigná un Coin Prefab.", MessageType.Warning);
    }

    // ─── Activar / desactivar escucha en SceneView ────────────────────────────
    private void SetActive(bool value)
    {
        _isActive = value;
        if (_isActive)
        {
            SceneView.duringSceneGui += OnSceneGUI;
            // Quitar foco del editor para que el SceneView reciba input
            SceneView.lastActiveSceneView?.Focus();
        }
        else
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _isPainting = false;
        }
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_isActive)
            return;

        Event e = Event.current;

        // Esc desactiva la tool
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            SetActive(false);
            e.Use();
            return;
        }

        // Calcular celda bajo el cursor
        _hoveredCellValid = TryGetCellFromMouse(e.mousePosition, sceneView, out _hoveredCell);

        // Modo temporal invertido con Shift
        PaintMode activeMode =
            (e.shift && _mode == PaintMode.Paint)
                ? PaintMode.Erase
                : (e.shift && _mode == PaintMode.Erase)
                    ? PaintMode.Paint
                    : _mode;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            _isPainting = true;
            if (_hoveredCellValid)
                ProcessCell(_hoveredCell, activeMode);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && _isPainting)
        {
            if (_hoveredCellValid)
                ProcessCell(_hoveredCell, activeMode);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            _isPainting = false;
            e.Use();
        }

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        DrawGrid(sceneView);
        DrawPlacedCoins();
        if (_hoveredCellValid)
            DrawHoverCell(_hoveredCell, activeMode);

        sceneView.Repaint();
    }

    private bool TryGetCellFromMouse(Vector2 mousePos, SceneView view, out Vector3 cell)
    {
        cell = Vector3.zero;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

        float t = (_gridZ - ray.origin.z) / ray.direction.z;
        if (float.IsInfinity(t) || float.IsNaN(t) || t < 0f)
            return false;

        Vector3 worldPos = ray.origin + ray.direction * t;

        cell = new Vector3(
            Mathf.Round(worldPos.x / _gridSize) * _gridSize,
            Mathf.Round(worldPos.y / _gridSize) * _gridSize,
            _gridZ
        );
        return true;
    }

    private void ProcessCell(Vector3 cell, PaintMode mode)
    {
        if (mode == PaintMode.Paint)
            PlaceCoin(cell);
        else
            EraseCoin(cell);
    }

    private void PlaceCoin(Vector3 cell)
    {
        if (_coinPrefab == null)
            return;
        if (_placedCoins.ContainsKey(cell))
            return;

        Undo.SetCurrentGroupName("Paint Coin");

        var coin = (GameObject)PrefabUtility.InstantiatePrefab(_coinPrefab);
        coin.transform.position = cell;
        if (_parent != null)
            coin.transform.SetParent(_parent, worldPositionStays: true);

        Undo.RegisterCreatedObjectUndo(coin, "Paint Coin");

        _placedCoins[cell] = coin;
    }

    private void EraseCoin(Vector3 cell)
    {
        if (!_placedCoins.TryGetValue(cell, out GameObject coin))
            return;

        if (coin != null)
        {
            Undo.SetCurrentGroupName("Erase Coin");
            Undo.DestroyObjectImmediate(coin);
        }

        _placedCoins.Remove(cell);
    }

    private void UndoAll()
    {
        foreach (var coin in _placedCoins.Values)
            if (coin != null)
                Undo.DestroyObjectImmediate(coin);
        _placedCoins.Clear();
    }

    private void DrawGrid(SceneView view)
    {
        Vector3 cam = view.camera.transform.position;
        float range = _gridSize * 12f;

        float startX = Mathf.Floor((cam.x - range) / _gridSize) * _gridSize;
        float startY = Mathf.Floor((cam.y - range) / _gridSize) * _gridSize;
        float endX = startX + range * 2f;
        float endY = startY + range * 2f;

        Handles.color = ColorGrid;
        for (float x = startX; x <= endX; x += _gridSize)
            Handles.DrawLine(new Vector3(x, startY, _gridZ), new Vector3(x, endY, _gridZ));
        for (float y = startY; y <= endY; y += _gridSize)
            Handles.DrawLine(new Vector3(startX, y, _gridZ), new Vector3(endX, y, _gridZ));
    }

    private void DrawPlacedCoins()
    {
        float r = _gridSize * 0.35f;
        Handles.color = ColorPlaced;

        foreach (var cell in _placedCoins.Keys)
            Handles.DrawSolidDisc(cell, Vector3.forward, r);
    }

    private void DrawHoverCell(Vector3 cell, PaintMode mode)
    {
        bool occupied = _placedCoins.ContainsKey(cell);
        float half = _gridSize * 0.5f;

        if (mode == PaintMode.Erase)
            Handles.color = ColorErase;
        else
            Handles.color = occupied ? ColorHoverOccupied : ColorHoverOk;

        var corners = new Vector3[]
        {
            cell + new Vector3(-half, -half, 0),
            cell + new Vector3(half, -half, 0),
            cell + new Vector3(half, half, 0),
            cell + new Vector3(-half, half, 0),
            cell + new Vector3(-half, -half, 0),
        };
        Handles.DrawPolyLine(corners);

        Handles.DrawWireDisc(cell, Vector3.forward, _gridSize * 0.3f);

        Handles.Label(
            cell + Vector3.up * (half + 0.05f),
            $"({cell.x:0.##}, {cell.y:0.##})",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } }
        );
    }

    private void OnDestroy() => SetActive(false);
}
#endif
