using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using SakuraTools.Core;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace SakuraTools.SizeReferences
{
    public class SizeReferences : MonoBehaviour
    {
        //------------------------------------------------------------------------------------//
        /*----------------------------------- FIELDS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        private const string SELECTED_INDEX_KEY = "ScaleReferences_Index";
        private const string POSITION_KEY_PRE = "ScaleReferences_Position";
        private const string ROTATION_KEY_PRE = "ScaleReferences_Rotation";
        private const string MESH_COLOR_KEY = "ScaleReferences_MeshColor";
        private const string IS_VISIBLE_KEY = "ScaleReferences_IsVisible";

        private const string MATERIAL_HDR_COLOR_NAME = "_Emission";

        private static Vector3 _EXTRA_OFFSET = new Vector3(0f, 0.0005f, 0f);

        [SerializeField]
        private MeshRenderer _gizmo;

        [SerializeField]
        private MeshRenderer[] _orderedReferences = Array.Empty<MeshRenderer>();

        [SerializeField]
        private string[] _referenceNames = Array.Empty<string>();

        // Runtime
        private static SizeReferences _instance;

        private int _selectedIndex;
        private Material _material;
        private Transform _transform;

        //------------------------------------------------------------------------------------//
        /*--------------------------------- PROPERTIES ---------------------------------------*/
        //------------------------------------------------------------------------------------//

        public string[] ReferenceNames => _referenceNames;
        public GameObject CurrentMeshObject => _orderedReferences[SelectedIndex].gameObject;
        public string CurrentReferenceName => _referenceNames[SelectedIndex];
        public int TotalReferences => _orderedReferences.Length;

        public static SizeReferences Instance
        {
            get
            {
                // Still haven't figured out why, but with the current HideFlags, the prefab persists through scenes, but loses all components.
                // Starting to think a don't destroy on load was required, but this should take care of all sorts of unseen errors in one go.
                if (_instance != null && _instance.GetComponentInChildren<Renderer>() != null) { return _instance; }

#if UNITY_EDITOR
                // Get rid of old instances if they somehow still exist.
                foreach (SizeReferences refs in (SizeReferences[])Resources.FindObjectsOfTypeAll(typeof(SizeReferences)))
                {
                    if (EditorUtility.IsPersistent(refs) || PrefabUtility.GetPrefabInstanceHandle(refs) != null) { continue; }
                    try { refs.EditorDestroyGameObject(true); } catch { }
                }

                // Get the prefab asset from within the package.
                string targetRP =
#if ST_URP
                    "URP";
#elif ST_HDRP
                    "HDRP";
#else
                    "RP";
#endif
                string prefabPath = $"Packages/com.heisarzola.unity-size-references/Runtime/{targetRP}/References Prefab.prefab";
                var packagedPrefab = ((GameObject)AssetDatabase.LoadAssetAtPath(prefabPath,
                    typeof(GameObject)));

                if (packagedPrefab != null)
                {
                    _instance = GameObject.Instantiate(packagedPrefab.GetComponentInChildren<SizeReferences>());
                    _instance.InitialSetup();
                    return _instance;
                }

                // Fallback for local testing (non unity-packaged files).
                string[] guids = AssetDatabase.FindAssets("t:prefab");
                foreach (var id in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(id);
#if ST_URP
                    if (!assetPath.Contains("URP")) { continue; }
#elif ST_HDRP
                    if (!assetPath.Contains("HDRP")) { continue; }
#else
                    if (assetPath.Contains("URP") || assetPath.Contains("HDRP")) { continue; }
#endif
                    SizeReferences prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath)?.GetComponentInChildren<SizeReferences>();
                    if (prefab == null) { continue; }
                    _instance = GameObject.Instantiate(prefab);
                    _instance.InitialSetup();
                    return _instance;
                }
#endif

                throw new NotImplementedException("Unable to find references prefab.");
            }
        }

        private string SceneKeySuffix
        {
            get
            {
#if UNITY_EDITOR
                // This way the position of the references can be scene-specific.
                return $"_{SceneManager.GetActiveScene().path}";
#else
                return string.Empty;
#endif
            }
        }

        public Transform CachedTransform => _transform;

        public Vector3 ReferencePosition
        {
            get => PlayerPrefsHelper.GetVector3($"{POSITION_KEY_PRE}{SceneKeySuffix}", Vector3.zero);
            set
            {
                PlayerPrefsHelper.SetVector3($"{POSITION_KEY_PRE}{SceneKeySuffix}", value);
                _transform.position = value + _EXTRA_OFFSET;
            }
        }

        public Quaternion ReferenceRotation
        {
            get => PlayerPrefsHelper.GetQuaternion($"{ROTATION_KEY_PRE}{SceneKeySuffix}", Quaternion.identity);
            set
            {
                PlayerPrefsHelper.SetQuaternion($"{ROTATION_KEY_PRE}{SceneKeySuffix}", value);
                _transform.rotation = value;
            }
        }

        public bool IsVisible
        {
            get => PlayerPrefsHelper.GetBool(IS_VISIBLE_KEY, true);
            set
            {
                PlayerPrefsHelper.SetBool(IS_VISIBLE_KEY, value);
                UpdateSelection();
            }
        }

        public int SelectedIndex
        {
            get => PlayerPrefs.GetInt(SELECTED_INDEX_KEY, Instance.TotalReferences - 1);
            set
            {
                var newIndex = value;
                if (newIndex >= TotalReferences)
                {
                    newIndex = TotalReferences - 1;
                }
                PlayerPrefs.SetInt(SELECTED_INDEX_KEY, newIndex);
                UpdateSelection(newIndex);
            }
        }

        private Material Material => _material ??= GetComponentInChildren<Renderer>(true).sharedMaterial;
        public Color MeshColor
        {
            get => PlayerPrefsHelper.GetColor(MESH_COLOR_KEY, new Color(0, 0.4f, 0.5f, 1f));
            set
            {
                PlayerPrefsHelper.SetColor(MESH_COLOR_KEY, value);
                Material.SetColor(MATERIAL_HDR_COLOR_NAME, value);
#if UNITY_EDITOR
                EditorUtility.SetDirty(Material);
#endif
            }
        }

        //------------------------------------------------------------------------------------//
        /*---------------------------------- METHODS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        public void UpdateSelection(int targetIndex = -1)
        {
            int index = targetIndex > -1 ? targetIndex : SelectedIndex;
            for (int i = 0; i < TotalReferences; i++)
            {
                _orderedReferences[i].enabled = i == index && IsVisible;
            }

            _gizmo.enabled = IsVisible;
        }

        public void InitialSetup()
        {
            // Set Transform and Rotation
            _transform = transform;
            _transform.position = ReferencePosition + _EXTRA_OFFSET;
            _transform.rotation = ReferenceRotation;

            // Set Color
            Material.SetColor(MATERIAL_HDR_COLOR_NAME, MeshColor);

            // Reset Everything
            for (int i = 0; i < TotalReferences; i++)
            {
                _orderedReferences[i].enabled = false;
                _orderedReferences[i].transform.localPosition = Vector3.zero;
            }
            _gizmo.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
            SceneVisibilityManager.instance.DisablePicking(gameObject, true);
            gameObject.MarkAsUneditable();
#endif
            UpdateSelection();
        }
    }
}