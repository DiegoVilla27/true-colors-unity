#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TrueColors.Customization;

namespace TrueColors.EditorTools
{
    /// <summary>
    /// Ventana e Inspector de desarrollo exclusiva de Unity Editor.
    /// Permite al desarrollador seleccionar y probar las diferentes skins de naves
    /// tanto en tiempo de edición (escena) como en Play Mode, sin afectar en absoluto
    /// las builds de producción (código 100% encapsulado en carpeta Editor).
    /// </summary>
    public class ShipSkinDevSelector : EditorWindow
    {
        private int _selectedDropdownIndex = 0;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Dev/Selector de Naves (Skins) %&s")]
        [MenuItem("Window/TrueColors/Ship Skin Selector (Dev)")]
        public static void OpenWindow()
        {
            var window = GetWindow<ShipSkinDevSelector>("Ship Selector (Dev)");
            window.minSize = new Vector2(340, 480);
            window.Show();
        }

        void OnEnable()
        {
            SyncWithCurrentEquippedShip();
        }

        void OnFocus()
        {
            SyncWithCurrentEquippedShip();
        }

        private void SyncWithCurrentEquippedShip()
        {
            string currentId = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
            var catalog = LoadCatalog();
            if (catalog != null && catalog.ships != null)
            {
                for (int i = 0; i < catalog.ships.Count; i++)
                {
                    if (catalog.ships[i] != null && catalog.ships[i].id == currentId)
                    {
                        _selectedDropdownIndex = i;
                        break;
                    }
                }
            }
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🛠️ SELECTOR DE SKINS (DEV ONLY)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Herramienta exclusiva de desarrollo para probar todas las skins de naves sin alterar producción.\n" +
                "Funciona tanto en Edit Mode (actualiza la escena activa) como en Play Mode.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            var catalog = LoadCatalog();
            if (catalog == null || catalog.ships == null || catalog.ships.Count == 0)
            {
                EditorGUILayout.HelpBox("No se encontró ShipCatalog en Assets/Resources/ShipCatalog.asset.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            // Preparar opciones del dropdown
            string[] shipNames = new string[catalog.ships.Count];
            for (int i = 0; i < catalog.ships.Count; i++)
            {
                var s = catalog.ships[i];
                shipNames[i] = $"{i + 1}. {s.displayName} ({(s.isDefaultUnlocked ? "Default" : "Pack")})";
            }

            // 1. SELECTOR / DROPDOWN PRINCIPAL
            EditorGUILayout.LabelField("Seleccionar Nave para Pruebas:", EditorStyles.boldLabel);
            int newIndex = EditorGUILayout.Popup(_selectedDropdownIndex, shipNames, GUILayout.Height(28));
            if (newIndex != _selectedDropdownIndex)
            {
                _selectedDropdownIndex = newIndex;
            }

            EditorGUILayout.Space(10);

            // 2. DETALLES Y PREVIEW DE LA NAVE SELECCIONADA
            if (_selectedDropdownIndex >= 0 && _selectedDropdownIndex < catalog.ships.Count)
            {
                var ship = catalog.ships[_selectedDropdownIndex];
                if (ship != null)
                {
                    string currentEquippedId = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
                    bool isEquipped = (ship.id == currentEquippedId);

                    // Caja de Información
                    EditorGUILayout.BeginVertical("box");
                    
                    // Estado actual
                    if (isEquipped)
                    {
                        GUI.color = new Color(0.3f, 1f, 0.4f, 1f);
                        EditorGUILayout.LabelField("★ NAVE ACTUALMENTE EQUIPADA", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                        EditorGUILayout.LabelField("○ Nave en Reserva (No equipada)", EditorStyles.miniBoldLabel);
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.LabelField($"ID: {ship.id}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Nombre: {ship.displayName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Descripción:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(ship.description, EditorStyles.wordWrappedLabel);

                    EditorGUILayout.Space(8);

                    // Preview del Sprite
                    if (ship.sprite != null && ship.sprite.texture != null)
                    {
                        EditorGUILayout.LabelField("Vista Previa:", EditorStyles.miniBoldLabel);
                        Rect previewRect = GUILayoutUtility.GetRect(160, 160, GUILayout.ExpandWidth(false));
                        previewRect.x = (position.width - 160) * 0.5f;

                        // Fondo oscuro para la preview
                        EditorGUI.DrawRect(new Rect(previewRect.x - 4, previewRect.y - 4, previewRect.width + 8, previewRect.height + 8), new Color(0.12f, 0.14f, 0.18f, 1f));
                        GUI.DrawTexture(previewRect, ship.sprite.texture, ScaleMode.ScaleToFit, true);
                        EditorGUILayout.Space(10);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(12);

                    // 3. BOTÓN DE EQUIPAR
                    GUI.backgroundColor = isEquipped ? new Color(0.3f, 0.8f, 0.4f, 1f) : new Color(0.2f, 0.7f, 1f, 1f);
                    string equipBtnText = isEquipped ? "✓ NAVE YA EQUIPADA" : $"🚀 EQUIPAR {ship.displayName.ToUpper()}";

                    if (GUILayout.Button(equipBtnText, GUILayout.Height(44)))
                    {
                        ApplySkin(ship.id);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.Space(20);

            // 4. UTILIDADES DE MONEDA Y COMPRA PARA DEV
            EditorGUILayout.LabelField("Herramientas de Soporte (Dev)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Añadir 10,000 Rocas (+ Saldo)"))
            {
                int current = PlayerPrefs.GetInt(CurrencyManager.ROCKS_KEY, 0);
                PlayerPrefs.SetInt(CurrencyManager.ROCKS_KEY, current + 10000);
                PlayerPrefs.Save();
                if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddRocks(10000);
                Debug.Log("<color=#00FFFF>[DEV]</color> 10,000 Rocas añadidas.");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Desbloquear Paquete (Dev)"))
            {
                PlayerPrefs.SetInt(ShipCustomizationManager.PACK_PURCHASED_KEY, 1);
                PlayerPrefs.Save();
                if (Application.isPlaying && ShipCustomizationManager.Instance != null)
                {
                    ShipCustomizationManager.Instance.DebugUnlockPack();
                }
                Debug.Log("<color=#00FFFF>[DEV]</color> Paquete de 4 naves marcado como comprado.");
            }

            if (GUILayout.Button("Restablecer Paquete (Dev)"))
            {
                PlayerPrefs.SetInt(ShipCustomizationManager.PACK_PURCHASED_KEY, 0);
                PlayerPrefs.Save();
                if (Application.isPlaying && ShipCustomizationManager.Instance != null)
                {
                    ShipCustomizationManager.Instance.DebugResetPack();
                }
                Debug.Log("<color=#00FFFF>[DEV]</color> Paquete restablecido a bloqueado.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        public static void ApplySkin(string shipId)
        {
            var catalog = LoadCatalog();
            if (catalog == null) return;

            var ship = catalog.GetShipById(shipId);
            if (ship == null) return;

            // 1. Guardar en PlayerPrefs
            PlayerPrefs.SetString(ShipCustomizationManager.SELECTED_SHIP_KEY, shipId);
            PlayerPrefs.Save();

            // 2. Si está en Play Mode, llamar a ShipCustomizationManager
            if (Application.isPlaying && ShipCustomizationManager.Instance != null)
            {
                ShipCustomizationManager.Instance.SelectShip(shipId);
            }

            // 3. Aplicar directamente a todas las naves en la escena activa
            ShipSkinApplier.ApplyToAllInScene(ship.sprite);
            UpdateActiveSceneShips(ship.sprite);

            Debug.Log($"<color=#55FF55><b>[DEV]</b></color> Skin cambiada exitosamente a: <b>{ship.displayName}</b> ({shipId})");
        }

        private static void UpdateActiveSceneShips(Sprite sprite)
        {
            if (sprite == null) return;

            bool sceneModified = false;

            // Actualizar elementos con ShipSkinApplier
            var appliers = Object.FindObjectsByType<ShipSkinApplier>(FindObjectsSortMode.None);
            foreach (var applier in appliers)
            {
                var img = applier.GetComponent<Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, "Dev Change Ship Skin");
                    img.sprite = sprite;
                    EditorUtility.SetDirty(img);
                    sceneModified = true;
                }

                var sr = applier.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Undo.RecordObject(sr, "Dev Change Ship Skin");
                    sr.sprite = sprite;
                    EditorUtility.SetDirty(sr);
                    sceneModified = true;
                }
            }

            // Buscar por nombre de naves conocidas si no tienen el componente
            string[] targetShipNames = { "MenuShip_Red", "MenuShip_Blue", "Ship_Left", "Ship_Right" };
            foreach (var name in targetShipNames)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    var img = obj.GetComponent<Image>();
                    if (img != null)
                    {
                        Undo.RecordObject(img, "Dev Change Ship Skin");
                        img.sprite = sprite;
                        EditorUtility.SetDirty(img);
                        sceneModified = true;
                    }

                    var sr = obj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Undo.RecordObject(sr, "Dev Change Ship Skin");
                        sr.sprite = sprite;
                        EditorUtility.SetDirty(sr);
                        sceneModified = true;
                    }
                }
            }

            if (sceneModified && !Application.isPlaying)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static ShipCatalogSO LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalogSO>("Assets/Resources/ShipCatalog.asset");
            if (catalog == null)
            {
                catalog = Resources.Load<ShipCatalogSO>("ShipCatalog");
            }
            return catalog;
        }

        #region Atajos del Menú Superior de Unity
        [MenuItem("Tools/Dev/Equipar Nave/1. Classic (Default)")]
        public static void EquipClassic() => ApplySkin("ship_classic");

        [MenuItem("Tools/Dev/Equipar Nave/2. Valkyrie (Interceptor)")]
        public static void EquipValkyrie() => ApplySkin("ship_valkyrie");

        [MenuItem("Tools/Dev/Equipar Nave/3. Titan (Heavy Juggernaut)")]
        public static void EquipTitan() => ApplySkin("ship_titan");

        [MenuItem("Tools/Dev/Equipar Nave/4. Phantom (Stealth Arrowhead)")]
        public static void EquipPhantom() => ApplySkin("ship_phantom");

        [MenuItem("Tools/Dev/Equipar Nave/5. Solaris (Solar Cruiser)")]
        public static void EquipSolaris() => ApplySkin("ship_solaris");
        #endregion
    }

    /// <summary>
    /// Custom Inspector para ShipCustomizationManager que añade el selector de skins
    /// directamente en la ventana Inspector cuando se selecciona el componente.
    /// </summary>
    [CustomEditor(typeof(ShipCustomizationManager))]
    public class ShipCustomizationManagerEditor : Editor
    {
        private int _selectedDevIndex = 0;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🛠️ PRUEBAS DE SKINS (DEV ONLY)", EditorStyles.boldLabel);

            var manager = (ShipCustomizationManager)target;
            var catalog = manager.Catalog;

            if (catalog != null && catalog.ships != null && catalog.ships.Count > 0)
            {
                string currentId = PlayerPrefs.GetString(ShipCustomizationManager.SELECTED_SHIP_KEY, "ship_classic");
                string[] options = new string[catalog.ships.Count];

                for (int i = 0; i < catalog.ships.Count; i++)
                {
                    var s = catalog.ships[i];
                    string marker = (s.id == currentId) ? "★ " : "   ";
                    options[i] = $"{marker}{i + 1}. {s.displayName}";
                    if (s.id == currentId) _selectedDevIndex = i;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Seleccionar Skin a Probar:", EditorStyles.miniBoldLabel);
                _selectedDevIndex = EditorGUILayout.Popup(_selectedDevIndex, options);

                EditorGUILayout.Space(6);
                if (GUILayout.Button("🚀 Equipar Skin Seleccionada", GUILayout.Height(30)))
                {
                    if (_selectedDevIndex >= 0 && _selectedDevIndex < catalog.ships.Count)
                    {
                        ShipSkinDevSelector.ApplySkin(catalog.ships[_selectedDevIndex].id);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
