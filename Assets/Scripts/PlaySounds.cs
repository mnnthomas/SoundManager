using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Byjus.Gamepod;
using System;

public class PlaySounds : MonoBehaviour
{
    [SerializeField] private SoundManager m_SoundManager = default;
    [SerializeField] private Image m_PauseButton = default;
    [SerializeField] private AudioClip m_SFX = default;
    [SerializeField] private AudioClip m_Music = default;
    [SerializeField] private AudioClip m_Low = default;
    [SerializeField] private AudioClip m_Med = default;
    [SerializeField] private AudioClip m_High = default;

    private bool mIsPaused = false;

    private void Start()
    {
        m_SoundManager.OnSndEventStart += OnSoundStart;
        m_SoundManager.OnSndEventEnd += OnSoundEnd;
    }

    public void OnPause()
    {
        mIsPaused = !mIsPaused;

        if (mIsPaused)
            m_PauseButton.color = Color.red;
        else
            m_PauseButton.color = Color.white;

        m_SoundManager.PauseAll(mIsPaused);
    }

    public void OnStopAll()
    {
        m_SoundManager.StopAll();
    }

    private void OnSoundStart(AudioClip obj)
    {
        Debug.LogWarning("OnSoundStart :" + obj.name);
    }

    private void OnSoundEnd(AudioClip obj)
    {
        Debug.LogWarning("OnSoundEnd :" + obj.name);
    }

    public void OnPlaySFX()
    {
        m_SoundManager.Play(m_SFX);
    }

    public void OnPlayMusic()
    {
        m_SoundManager.Play(m_Music, true);
    }

    public void OnPlayPrioritySound(int value)
    {
        SoundPriority sndPriority = (SoundPriority)value;
        if (value == 0)
            m_SoundManager.Play(m_Low);
        else if (value == 1)
            m_SoundManager.Play(m_Med, SoundPriority.MED);
        else if (value == 2)
            m_SoundManager.Play(m_High, SoundPriority.HIGH);
    }

    private void OnDestroy()
    {
        m_SoundManager.OnSndEventStart -= OnSoundStart;
        m_SoundManager.OnSndEventEnd -= OnSoundEnd;
    }
}
