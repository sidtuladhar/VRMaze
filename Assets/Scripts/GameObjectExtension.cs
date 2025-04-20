using UnityEngine;

public static class GameObjectExtensions
{
    public static GameObject GetNamedChild(this GameObject parent, string childName)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }
        Debug.LogWarning($"Child named '{childName}' not found under {parent.name}");
        return null;
    }
}
