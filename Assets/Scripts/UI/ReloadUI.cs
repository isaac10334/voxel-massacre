using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReloadUI : MonoBehaviour
{
    [SerializeField] private TMP_Text currentClip;
    [SerializeField] private TMP_Text allAmmo;
    private void Awake()
    {
        currentClip.gameObject.SetActive(false);
        allAmmo.gameObject.SetActive(false);
    }
    public void EnableReloadUI(InventoryItem item)
    {
        currentClip.gameObject.SetActive(true);
        allAmmo.gameObject.SetActive(true);

        currentClip.text = item.currentClipAmmo.ToString();
        allAmmo.text = item.restOfAmmo.ToString();
    }
    public void DisableReloadUI()
    {
        currentClip.gameObject.SetActive(false);
        allAmmo.gameObject.SetActive(false);
    }
}
