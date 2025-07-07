using UnityEngine;

public class EffectTimeOut : MonoBehaviour
{

    private float timeOut = 0.5f;
    void Update()
    {
        Destroy(gameObject, timeOut);
	}
}
