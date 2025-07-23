using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionAnimator : MonoBehaviour
{
    [SerializeField]
    Image sceneTransitioner;
    [SerializeField, Range(0.5f, 10f)]
    float sceneTransitionTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LeanTween.moveX(sceneTransitioner.rectTransform, 2387, sceneTransitionTime).setEaseOutExpo();
    }

}
