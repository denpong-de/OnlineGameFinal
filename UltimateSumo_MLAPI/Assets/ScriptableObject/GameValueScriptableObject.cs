using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameValueScriptableObject", order = 1)]
public class GameValueScriptableObject : ScriptableObject
{
    public bool switchSideTrigger;
    public bool changeNameTrigger;
}
