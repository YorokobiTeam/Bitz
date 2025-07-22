using Supabase.Gotrue;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "Scriptable Objects/UserData")]
public class UserData : ScriptableObject
{
    public BitzPlayerInfo playerInfo;
    public string email;
}
