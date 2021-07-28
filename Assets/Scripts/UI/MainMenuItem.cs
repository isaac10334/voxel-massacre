using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	private TMP_Text textToUnderline;

	private void Awake()
	{
		textToUnderline = GetComponent<TMP_Text>();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		textToUnderline.fontStyle = TMPro.FontStyles.Underline;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
        textToUnderline.fontStyle &= ~FontStyles.Underline;
	}
}