using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "LoaderData", menuName = "Scriptable Objects/LoaderData")]
public class LoaderData : ScriptableObject
{
    // for now just need the tasks to load
    public List<Task> loaderTasksAsync;
    public List<Action> loaderTasks;

}
