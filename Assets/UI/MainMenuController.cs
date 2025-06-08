using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private VisualElement menuPanelRoot;
    private VisualElement rightPanelRoot;
    private List<VisualElement> buttons = new List<VisualElement>();
    private UIDocument ui;
    private InputAction navigate;

    private int leftPanelCurrentIndex = 0;
    private int rightPanelCurrentIndex = 0;



    private bool isLeft = true;

    public void Start()
    {
        ui = GetComponent<UIDocument>();
        menuPanelRoot = ui.rootVisualElement.Q<VisualElement>("MenuPanel");
        rightPanelRoot = ui.rootVisualElement.Q<VisualElement>("PlayHistory");
        // Add menu items to the left panel list

        menuPanelRoot.Children().FirstOrDefault().Focus();

        buttons.AddRange(menuPanelRoot.Children());

        navigate = InputSystem.actions.FindAction("Navigate");
        navigate.performed += handleNavigation;
    }

    private void handleNavigation(InputAction.CallbackContext ctx)
    {
        if (navigate != null)
        {
            Vector2 vector2 = ctx.ReadValue<Vector2>();
            if (vector2.y > 0)
            {
                if (isLeft)
                {
                    leftPanelCurrentIndex--;
                    buttons[leftPanelCurrentIndex].Focus();
                }
                //else
                //{
                //    rightPanelCurrentIndex--;
                //}
                leftPanelCurrentIndex = WrapIndex(leftPanelCurrentIndex, 0, buttons.Count - 1);

            }
        }
    }

    private void RelinkClasses()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            // Dynamically remove all potential 'scale-X' classes.
            // It's generally better to clear all possible states before applying the new one.
            // If you have a predefined set of scale classes (e.g., scale-0 to scale-5),
            // you might want to remove them explicitly for clarity or use a loop.
            // For now, assuming scale-0, scale-1, scale-2 are sufficient based on previous code.
            buttons[i].RemoveFromClassList("scale-0");
            buttons[i].RemoveFromClassList("scale-1");
            buttons[i].RemoveFromClassList("scale-2");
            // Add more RemoveFromClassList calls here if you have more scale classes
            // than just 0, 1, 2, to cover the full range of potential distances.

            // Add the appropriate scale class based on the distance from the currentMenuItem
            buttons[i].AddToClassList("scale-" + Math.Abs(i - leftPanelCurrentIndex));
        }
    }

    private int WrapIndex(int indx, int min, int max)
    {
        if (indx > max)
        {
            return min;
        }
        if (indx < min)
        {
            return max;
        }
        return indx;
    }

}
