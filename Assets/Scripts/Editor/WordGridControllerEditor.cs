using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WordGridController))]
public class WordGridControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WordGridController wordGridController = target as WordGridController;

        base.OnInspectorGUI();

        if (GUILayout.Button("Generate"))
        {
            wordGridController.Generate();
        }

        if (GUILayout.Button("Solve"))
        {
            wordGridController.Solve();
        }

    }
}
