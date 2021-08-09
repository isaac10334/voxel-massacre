using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ShadowButton : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
	#pragma warning disable 0649
	
	[SerializeField] private AudioClip hoverOverSound;
	[SerializeField] private AudioClip clickSound;
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private Sprite nonPressedSprite;
	[SerializeField] private Sprite pressedSprite;
	
	#pragma  warning restore 0649
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.sprite = nonPressedSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(audioSource && hoverOverSound)
        {
            audioSource.PlayOneShot(hoverOverSound);
        }

        transform.DOPunchScale(new Vector3(0.9f, 0.9f, 0.9f), 0.1f);
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
