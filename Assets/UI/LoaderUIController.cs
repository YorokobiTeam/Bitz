using UnityEngine;
using UnityEngine.UIElements;

public class LoaderController : MonoBehaviour
{
    public VisualElement ui;
    public VisualElement loaderIcon;

    public void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    public void OnEnable()
    {
        loaderIcon = ui.Q<VisualElement>("LoadIcon");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        loaderIcon.RegisterCallback<TransitionEndEvent>(evt =>
        {
            loaderIcon.RemoveFromClassList("animate-spin");
            loaderIcon.schedule.Execute(() => loaderIcon.AddToClassList("animate-spin")).StartingIn(10);
        });
        loaderIcon.schedule.Execute(() => loaderIcon.AddToClassList("animate-spin")).StartingIn(100);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
