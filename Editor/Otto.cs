using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Helios.Otto
{
    //add me to an Editor folder
    public class Otto
    {
        /// <summary>
        /// Loops through all public fields on the target mono behaviour and searches for a relevant asset in the asset database
        /// Separate your search terms with an underscore, and being as specific as possible with your variable names helps Otto to find the right assets
        /// Example: "Optimistic Unicorn.mp4" can be found with variable names "Optimistic", "Unicorn", or best of all: "OptimisticUnicorn"
        /// </summary>
        [MenuItem("CONTEXT/MonoBehaviour/Auto Populate - All")]
        static void AutoPopulateAll(MenuCommand command)
        {
            Debug.Log("[Otto]: Auto populating variables");
            var type = command.context.GetType();
            Debug.Log($"[Otto]: Doing a kickflip over the {type.Name}");
            var fields = type.GetFields();
            var fieldsSet = 0;
            List<GameObject> sceneObjects = GetSceneObejcts();
            for (int i = 0; i < fields.Length; i++)
            {
                var assetName = ParseName(fields[i].Name);
                if (SearchAssets(command.context, assetName, fields[i]))
                {
                    fieldsSet++;
                    continue;
                }

                if (SearchScene(command.context, assetName, fields[i], sceneObjects)) fieldsSet++;
            }
            Debug.Log($"[Otto]: All done! I set {fieldsSet} fields");
        }

        [MenuItem("CONTEXT/MonoBehaviour/Auto Populate - Assets")]
        static void AutoPopulateAssets(MenuCommand command)
        {
            Debug.Log("[Otto]: Auto populating variables");
            var type = command.context.GetType();
            Debug.Log($"[Otto]: Doing a kickflip over the {type.Name}");
            var fields = type.GetFields();
            var fieldsSet = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                var assetName = ParseName(fields[i].Name);
                if (SearchAssets(command.context, assetName, fields[i])) fieldsSet++;
            }
            Debug.Log($"[Otto]: All done! I set {fieldsSet} fields");
        }

        [MenuItem("CONTEXT/MonoBehaviour/Auto Populate - Scene")]
        static void AutoPopulateScene(MenuCommand command)
        {
            Debug.Log("[Otto]: Auto populating variables");
            var type = command.context.GetType();
            Debug.Log($"[Otto]: Doing a kickflip over the {type.Name}");
            var fields = type.GetFields();
            var fieldsSet = 0;
            List<GameObject> sceneObjects = GetSceneObejcts();
            for (int i = 0; i < fields.Length; i++)
            {
                var assetName = ParseName(fields[i].Name);
                if (SearchScene(command.context, assetName, fields[i], sceneObjects)) fieldsSet++;
            }
            Debug.Log($"[Otto]: All done! I set {fieldsSet} fields");
        }

        static bool SearchAssets(UnityEngine.Object target, string search, FieldInfo field)
        {
            var asset = AssetDatabase.FindAssets(search);
            if (asset.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(asset[0]);
                var loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, field.FieldType);
                if (loadedAsset != null)
                {
                    Undo.RecordObject(target, $"Set field {field.Name}");
                    field.SetValue(target, loadedAsset);
                    return true;
                }
            }
            return false;
        }

        static bool SearchScene(UnityEngine.Object target, string search, FieldInfo field, List<GameObject> obj)
        {
            //if we can't even find the obejct type in the scene, why search for it?
            //i.e. you won't find a float or audio clip in the scene!
            if (TypeNotSceneCompatible(field.FieldType)&&!field.FieldType.Name.Contains("Transform"))
            {
                return false;
            }

            object targetObject = obj.Find(x =>
            {
                var s = search.Split(' ');
                return s.All(x.name.Contains);
            });

            if (!field.FieldType.Name.Contains("GameObject"))
            {
                if (field.FieldType.Name.Contains("UnityEngine.Transform")) targetObject = ((GameObject)targetObject).transform;
                else targetObject = ((GameObject)targetObject)?.GetComponent(field.FieldType);
            }

            if (targetObject != null)
            {
                Undo.RecordObject(target, $"Set field {field.Name}");
                field.SetValue(target, targetObject);
                return true;
            }

            return false;
        }

        static List<GameObject> GetSceneObejcts()
        {
            List<GameObject> obj = new List<GameObject>();
            SceneManager.GetActiveScene().GetRootGameObjects(obj);
            var rootObj = new List<GameObject>(obj);
            foreach (GameObject o in rootObj)
            {
                GetChildren(ref obj, o.transform);
            }
            return obj;
        }

        static void GetChildren(ref List<GameObject> l, Transform t)
        {
            if (!l.Contains(t.gameObject)) l.Add(t.gameObject);

            for (int childIndex = 0; childIndex < t.childCount; childIndex++)
            {
                var child = t.GetChild(childIndex);

                if (!l.Contains(child.gameObject))
                {
                    l.Add(child.gameObject);
                }

                if (child.childCount > 0)
                {
                    var r = new List<GameObject>();
                    GetChildren(ref r, child);
                    foreach (GameObject g in r) if (!l.Contains(g)) l.Add(g);
                }
            }
        }

        static string ParseName(string name)
        {
            var parsedName = String.Join(" ", Regex.Split(name, @"(?<!^)(?=[A-Z])"));
            return parsedName.Replace('_', ' ');
        }

        static bool TypeNotSceneCompatible(Type type)
        {
            return (!type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsSubclassOf(typeof(Behaviour)) && type.Name != "GameObject");
        }
    }
}