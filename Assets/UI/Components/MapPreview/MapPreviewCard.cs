

using UnityEditor;
using UnityEngine.UIElements;

[UxmlElement]
public partial class MapPreviewCard : VisualElement
{
    public static BindingId keyProperty = nameof(mapTitle);


    [UxmlAttribute]
    public string mapTitle;

    [UxmlAttribute]
    public string musicAuthor;

    [UxmlAttribute]
    public string mapAuthor;

    [UxmlAttribute]
    public string currentPoints;


    //public void OnEnable()
    //{
    //    var visualElementTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Components/MapPreview/MapPreviewCard.uxml");
    //    var root = visualElementTree.Instantiate();
    //    this.Add(root);
    //}

}
