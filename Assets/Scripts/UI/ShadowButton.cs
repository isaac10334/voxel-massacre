using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShadowButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	#pragma warning disable 0649
	
	[SerializeField] private AudioClip clickSound;
	[SerializeField] private AudioSource audioSource;

    // The one with shadow. Or not, this script is pretty reusable.
	[SerializeField] private Sprite nonPressedSprite;
	[SerializeField] private Sprite pressedSprite;
	
	#pragma  warning restore 0649
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.sprite = nonPressedSprite;
    }

    public void OnPointerOver()
    {
        _image.color = new Color32(150, 150, 150, 255);
    }
    public void OnPointerExit()
    {
        _image.color = Color.white;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _image.sprite = pressedSprite;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
	    if (audioSource && clickSound)
	    {
		    audioSource.PlayOneShot(clickSound);
	    }
        _image.sprite = nonPressedSprite;
    }
	
}
