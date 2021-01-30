using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Video;

namespace Gates.Otto
{
    //add me to an Editor folder
    public class Otto
    {
        /*
        *   This is a helper tool that will attempt to fill all of the target mono behavior's public fields with references by searching through the scene and asset database
        *
        *   How to use:
        *   Name your public variables in a way that matches the asset/scene object it should reference and then execute an Auto Populate method
        *   Separate your search terms with an underscore or camel case, and be as specific as possible with your variable names to help Otto to find the right assets
        *   Example: "Optimistic Unicorn.mp4" can be found with variable names "Optimistic", "Unicorn", or best of all: "OptimisticUnicorn"
        */

        /// <summary>
        /// Iterates through all public fields on the target mono behaviour and searches for a relevant asset in the asset database and scene
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

        /// <summary>
        /// Iterates through all public fields on the target mono behaviour and searches for a relevant asset in the asset database
        /// </summary>
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

        /// <summary>
        /// Iterates through all public fields on the target mono behaviour and searches for a relevant asset in the scene
        /// </summary>
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

        /// <summary>
        /// Searches through the asset database to find an asset with a matching name
        /// </summary>
        static bool SearchAssets(UnityEngine.Object target, string search, FieldInfo field)
        {
            var asset = AssetDatabase.FindAssets(search);
            if (asset.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(asset[0]);
                var loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, field.FieldType);
                if (!loadedAsset.Equals(null))
                {
                    Undo.RecordObject(target, $"Set field {field.Name}");
                    field.SetValue(target, loadedAsset);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Searches through the scene to find an asset with a matching name
        /// </summary>
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

            if (!targetObject.Equals(null))
            {
                Undo.RecordObject(target, $"Set field {field.Name}");
                field.SetValue(target, targetObject);
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Iterates through all the objects in the scene and dispatches a recursive retrieval of their children
        /// </summary>
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

        /// <summary>
        /// Recursively returns a list of all of the children of a game object
        /// </summary>
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

        /// <summary>
        /// Breaks up the name of a variable into searchable terms
        /// </summary>
        static string ParseName(string name)
        {
            var parsedName = String.Join(" ", Regex.Split(name, @"(?<!^)(?=[A-Z])"));
            return parsedName.Replace('_', ' ');
        }

        /// <summary>
        /// Some types will never be found in the scene, so we should not search for them in the scene
        /// </summary>
        static bool TypeNotSceneCompatible(Type type)
        {
            return (!type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsSubclassOf(typeof(Behaviour)) && type.Name != "GameObject");
        }
    }
}