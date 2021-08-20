using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;

public class SettingsMenu : MonoBehaviour
{
    public delegate void OnSettingsUpdate(Settings settings);
    private OnSettingsUpdate onSettingsUpdate;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private GameObject activationContainer;

    private void Awake()
    {
        CloseSettingsMenu();
        InitializeSettings();

        volumeSlider.onValueChanged.AddListener((value) => UpdateFloatSetting("Volume", value));
        mouseSensitivitySlider.onValueChanged.AddListener((value) => UpdateFloatSetting("MouseSensitivity", value));
    
        onSettingsUpdate += UpdateSimpleSettings;
    }
    
    private void Start()
    {
        if(onSettingsUpdate != null)
            onSettingsUpdate(GetSettings());
    }

    public void SubscribeToSettingsUpdate(OnSettingsUpdate subscriber)
    {
        onSettingsUpdate += subscriber;
        subscriber(GetSettings());
    }

    private void InitializeSettings()
    {   
        Settings settings = GetSettings();

        mouseSensitivitySlider.value = settings.mouseSensitivity;
        volumeSlider.value = settings.volume;

        SaveSettings(settings);
    }

    private Settings GetSettings()
    {
        Settings settings = new Settings();

        if(PlayerPrefs.HasKey("MouseSensitivity"))
        {
            float mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
            settings.mouseSensitivity = mouseSensitivity;
        }

        if(PlayerPrefs.HasKey("Volume"))
        {
            float volume = PlayerPrefs.GetFloat("Volume");
            settings.volume = volume;
            // Implemented here, maybe change that
        }

        return settings;
    }

    private void SaveSettings(Settings settings)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", settings.mouseSensitivity);
        PlayerPrefs.SetFloat("Volume", settings.volume);
        PlayerPrefs.Save();
        
        if(onSettingsUpdate != null)
            onSettingsUpdate(settings);
    }

    public void OpenSettingsMenu()
    {
        activationContainer.SetActive(true);
    }

    private void UpdateFloatSetting(string key, float value)
    {
        Settings settings = GetSettings();

        // TODO: Super bad and ugly. maybe store settings as strings in the settings class and totally change the way it all works to be more like the design of PlayerPrefs
        if(key == "Volume")
        {
            settings.volume = value;
        }
        else if(key == "MouseSensitivity")
        {
            settings.mouseSensitivity = value;
        }

        SaveSettings(settings);
    }

    public void CloseSettingsMenu()
    {
        activationContainer.SetActive(false);
    }

    private void UpdateSimpleSettings(Settings settings)
    {
        AudioListener.volume = settings.volume;
    }

}
