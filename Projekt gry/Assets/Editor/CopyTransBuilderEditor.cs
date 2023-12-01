using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(copyTransform))]
public class CopyTransBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        copyTransform myScript = (copyTransform)target;
        if (GUILayout.Button("Copy Transform"))
        {
            myScript.transformThis();
        }
    }
}
