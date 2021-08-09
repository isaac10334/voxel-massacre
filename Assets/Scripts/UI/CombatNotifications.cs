using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class CombatNotifications : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationsText;
    [SerializeField] private CanvasGroup notificationsCanvasGroup;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] killCongratulationsVoicovers;

    public async void HeadshotKill()
    {
        audioSource.PlayOneShot(killCongratulationsVoicovers[Random.Range(0, killCongratulationsVoicovers.Length)]);
        notificationsText.text = "Headshot";
        await notificationsCanvasGroup.DOFade(1f, 0.1f).AsyncWaitForCompletion();
        await notificationsCanvasGroup.DOFade(0f, 0.5f).AsyncWaitForCompletion();
    }
}
    