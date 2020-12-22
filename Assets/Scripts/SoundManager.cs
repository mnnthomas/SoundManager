using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Byjus.Gamepod
{
    /// <summary>
    /// Defines the various priorty of sound with their percent volumes
    /// The volume updates when a sound of higher priority is played
    /// </summary>
    public enum SoundPriority
    {
        LOW = 25,
        MED = 50,
        HIGH = 100
    }

    public class SoundChannel
    {
        public string _ChannelName;
        public GameObject _ChannelObject;
        public AudioSource _AudioSource;
        public SoundPriority _Priority;
        public Coroutine _SoundEnd;
        public bool _IsPaused;

        public SoundChannel(string channelName, Transform parent)
        {
            _ChannelObject = new GameObject(channelName);
            _ChannelObject.transform.SetParent(parent);
            _ChannelObject.name = channelName;
            _ChannelName = channelName;
            _AudioSource = _ChannelObject.AddComponent<AudioSource>();
            _AudioSource.loop = false;
            _Priority = SoundPriority.LOW;
            _SoundEnd = null;
            _IsPaused = false;
        }
    }

    public class SoundManager : MonoBehaviour, ISoundManager
    {
        [SerializeField] private float m_DefaultVolume = 1f; // Default volume of newly created AudioSource 
        [SerializeField] private float m_PlayInterval = 0.2f; // Just to make sure no sound is played the second time instantly.

        private List<SoundChannel> mChannels = new List<SoundChannel>(); // List of all created SoundChannels
        private Dictionary<AudioClip, float> mPlayedHistory = new Dictionary<AudioClip, float>(); // Dictionary of played history to track time of when the sound was played
        private SoundPriority mCurrentHighestPriority = SoundPriority.LOW;

        public System.Action<AudioClip> OnSndEventStart;
        public System.Action<AudioClip> OnSndEventEnd;


        public void Play(AudioClip clip)
        {
            Play(clip, false, SoundPriority.LOW, -1);
        }

        public void Play(AudioClip clip, SoundPriority priority)
        {
            Play(clip, false, priority, -1);
        }

        public void Play(AudioClip clip, float volume)
        {
            Play(clip, false, SoundPriority.LOW, volume);
        }

        /// <summary>
        /// A play method that checks for free channel and plays sounds
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        /// <param name="loopSound">Defines whether the channel should be looped</param>
        /// <param name="priority">Defines the priority of the sound to be played</param>
        /// <param name="volume">Audio source volume</param>
        public void Play(AudioClip clip, bool loopSound = false, SoundPriority priority = SoundPriority.LOW, float volume = -1)
        {
            float curVolume = volume;
            if (curVolume == -1)
                curVolume = m_DefaultVolume;


            float lastPlayedTime = 0;
            if (mPlayedHistory.ContainsKey(clip))
                lastPlayedTime = mPlayedHistory[clip];

            if (lastPlayedTime == 0 || Time.time - lastPlayedTime >= m_PlayInterval)
            {
                SoundChannel channel = GetFreeChannel();
                channel._AudioSource.loop = loopSound;
                channel._Priority = priority;

                if (channel != null)
                {
                    channel._AudioSource.clip = clip;
                    channel._AudioSource.Play();
                    if (channel._Priority > mCurrentHighestPriority)
                    {
                        mCurrentHighestPriority = channel._Priority;
                        UpdatePriorityBasedVolume();
                        //Debug.Log("No higher priority sound playing");
                    }
                    else if (channel._Priority < mCurrentHighestPriority)
                    {
                        UpdatePriorityBasedVolume();
                        //Debug.Log(" higher priority sound already playing");
                    }
                    else
                        channel._AudioSource.volume = m_DefaultVolume;


                    OnSndEventStart?.Invoke(clip);
                    channel._SoundEnd = StartCoroutine(OnSoundEnd(channel));

                    if (lastPlayedTime == 0)
                        mPlayedHistory.Add(clip, Time.time);
                    else
                        mPlayedHistory[clip] = Time.time;
                }
            }
        }

        private void UpdatePriorityBasedVolume()
        {
            List<SoundChannel> playingChannels = GetAllPlayingChannels();
            List<SoundChannel> highPriority = playingChannels.FindAll(x => x._Priority == SoundPriority.HIGH);
            List<SoundChannel> medPriority = playingChannels.FindAll(x => x._Priority == SoundPriority.MED);
            List<SoundChannel> lowPriority = playingChannels.FindAll(x => x._Priority == SoundPriority.LOW);

            if(highPriority != null && highPriority.Count > 0)
            {
                foreach (SoundChannel ch in medPriority)
                    ch._AudioSource.volume = m_DefaultVolume * (int)Enum.GetValues(typeof(SoundPriority)).GetValue(1)/100;

                foreach (SoundChannel ch in lowPriority)
                    ch._AudioSource.volume = m_DefaultVolume * (int)Enum.GetValues(typeof(SoundPriority)).GetValue(0)/100;

                mCurrentHighestPriority = SoundPriority.HIGH;
            }
            else if(medPriority != null && medPriority.Count > 0)
            {
                foreach (SoundChannel ch in lowPriority)
                    ch._AudioSource.volume = m_DefaultVolume * (int)Enum.GetValues(typeof(SoundPriority)).GetValue(0)/100;

                mCurrentHighestPriority = SoundPriority.MED;
            }
            else
            {
                foreach (SoundChannel ch in lowPriority)
                    ch._AudioSource.volume = m_DefaultVolume;

                mCurrentHighestPriority = SoundPriority.LOW;
            }
        }

        /// <summary>
        /// Stops sound by audioClip type
        /// </summary>
        /// <param name="audioClip">Audio clip that needs to be stopped</param>
        public void Stop(AudioClip audioClip)
        {
            List<SoundChannel> ch = GetSoundChannelsByClip(audioClip);
            if(ch != null && ch.Count > 0)
            {
                for(int i = 0; i < ch.Count; i++)
                {
                    ch[i]._AudioSource.Stop();

                    //Stop the existing coroutine for SoundEnd event and trigger it in the current frame
                    if (ch[i]._SoundEnd != null)
                        StopCoroutine(ch[i]._SoundEnd);
                    OnSndEventEnd?.Invoke(audioClip);

                    ch[i]._AudioSource.clip = null;
                }
            }
        }

        /// <summary>
        /// Stops all sounds playing through SoundManager
        /// </summary>
        public void StopAll()
        {
            List<SoundChannel> ch = GetAllPlayingChannels();
            if (ch != null && ch.Count > 0)
            {
                for (int i = 0; i < ch.Count; i++)
                {
                    ch[i]._AudioSource.Stop();

                    //Stop the existing coroutine for SoundEnd event and trigger it in the current frame
                    if (ch[i]._SoundEnd != null)
                        StopCoroutine(ch[i]._SoundEnd);
                    OnSndEventEnd?.Invoke(ch[i]._AudioSource.clip);

                    ch[i]._AudioSource.clip = null;
                }
            }
        }

        /// <summary>
        /// Triggers the OnSndEventEnd after the sound playing stops. Waits if the sound is paused in between
        /// </summary>
        /// <param name="channel">Channel type</param>
        /// <returns></returns>
        IEnumerator OnSoundEnd(SoundChannel channel)
        {
            float startTime = Time.time;

            while (channel._IsPaused || channel._AudioSource.isPlaying || Time.time - startTime < channel._AudioSource.clip.length)
                yield return new WaitForEndOfFrame();

            OnSndEventEnd?.Invoke(channel._AudioSource.clip);
            ResetChannel(channel); // Resetting a used channel on SoundEnd before being re-used
            UpdatePriorityBasedVolume();
        }

        private void ResetChannel(SoundChannel ch)
        {
            ch._Priority = SoundPriority.LOW;
            ch._AudioSource.volume = m_DefaultVolume;
            ch._AudioSource.clip = null;
            ch._AudioSource.loop = false;
            ch._IsPaused = false;
            ch._SoundEnd = null;
        }

        /// <summary>
        /// Pauses all sound playing through SoundManager
        /// </summary>
        /// <param name="pause"></param>
        public void PauseAll(bool pause)
        {
            foreach (SoundChannel ch in mChannels)
            {
                if (pause)
                    PauseChannel(ch, true);
                else
                    PauseChannel(ch, false);
            }
        }

        private void PauseChannel(SoundChannel ch, bool value)
        {
            ch._IsPaused = value;

            if(value)
                ch._AudioSource.Pause();
            else
                ch._AudioSource.UnPause();
        }

        private List<SoundChannel> GetAllPlayingChannels()
        {
            List<SoundChannel> sndChannels = new List<SoundChannel>();
            if (mChannels.Count > 0)
                sndChannels = mChannels.FindAll(x => x._AudioSource.clip != null && x._AudioSource.isPlaying);

            return sndChannels;
        }

        private List<SoundChannel> GetSoundChannelsByClip(AudioClip audioClip)
        {
            List<SoundChannel> ch = new List<SoundChannel>();
            ch = GetAllPlayingChannels().FindAll(x => x._AudioSource.clip == audioClip);
            return ch;
        }

        private SoundChannel GetFreeChannel()
        {
            SoundChannel channel = default;

            if (mChannels.Count > 0)
            {
                channel = mChannels.Find(x => !x._AudioSource.isPlaying);
                if (channel == null)
                    channel = CreateNewChannel();
            }
            else if (mChannels.Count == 0)
                channel = CreateNewChannel();

            return channel;
        }

        private SoundChannel CreateNewChannel()
        {
            SoundChannel channel = default;
            channel = new SoundChannel("CHANNEL_" + (mChannels.Count + 1), transform);
            mChannels.Add(channel);
    
            return channel;
        }
    }

    public interface ISoundManager
    {
        void Play(AudioClip audioClip, bool canLoop, SoundPriority priority, float volume);
        void StopAll();
        void Stop(AudioClip clip);
        void PauseAll(bool value);
    }
}