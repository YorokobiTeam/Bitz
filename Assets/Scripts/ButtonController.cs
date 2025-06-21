using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonController : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] Sprite normal;
    [SerializeField] Sprite pressed;
    [SerializeField] InputActionReference press;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
		sr = GetComponent<SpriteRenderer>();

        press.action.started += OnButtonDown;
        press.action.canceled += OnButtonUp;
    }

	private void OnEnable()
	{
		press.action.Enable();
	}

	private void OnDisable()
	{
		press.action.Disable();
	}

	private void OnDestroy()
	{
		press.action.started -= OnButtonDown;
		press.action.canceled -= OnButtonUp;
	}

	private void OnButtonDown(InputAction.CallbackContext context)
	{
		sr.sprite = pressed;
	}

	private void OnButtonUp(InputAction.CallbackContext context)
	{
		sr.sprite = normal;
	}
}
