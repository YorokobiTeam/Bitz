using UnityEngine;

public class AlbumArtAnimator : MonoBehaviour
{
    public void OnEnable()
    {
        LeanTween.rotateAround(GetComponent<RectTransform>(), Vector3.forward, -360, 5f).setEaseLinear().setLoopClamp();
    }
}
