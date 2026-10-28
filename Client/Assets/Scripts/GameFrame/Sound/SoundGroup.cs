using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using GameCore;

namespace GameFrame
{
    public class SoundGroup
    {
        private readonly Transform m_Root;
        private readonly SoundType m_AudioType;
        private readonly AudioMixerGroup m_AudioMixerGroup;
        private List<SoundAgent> m_AudioAgents = new List<SoundAgent>();

        private bool m_Mute;
        private float m_Volume = 1.0f;

        public SoundType AudioType => m_AudioType;

        public AudioMixerGroup AudioMixerGroup => m_AudioMixerGroup;


        public float Volume
        {
            get => m_Volume;
            set
            {
                m_Volume = value;
                var v = Mathf.Clamp(m_Volume, 0.0001f, 1f); // 0.0001f 是为了避免 log10(0) 的情况
                // 将线性范围（0到1）转换为对数范围（-20到0）
                float volumeInDecibels = 20 * Mathf.Log10(v);
                m_AudioMixerGroup.audioMixer.SetFloat(m_AudioType.ToString(), volumeInDecibels);
            }
        }

        public bool Mute
        {
            get => m_Mute;
            set
            {
                m_Mute = value;
                if (value)
                {
                    StopAll();
                }
            }
        }

        public SoundGroup(SoundType audioType)
        {
            this.m_AudioType = audioType;
            m_Root = new GameObject(this.m_AudioType.ToString()).transform;
            m_Root.SetParent(SoundModule.Instance.Root);
            var audioMixerGroups = SoundModule.Instance.AudioMixer.FindMatchingGroups($"Master/{this.m_AudioType.ToString()}");
            m_AudioMixerGroup = audioMixerGroups.FirstOrDefault();
            if (m_AudioMixerGroup == null)
            {
                Log.Error($"Audio group {audioType.ToString()} could not be found.");
            }
        }

        public void Play(string location, SoundPlayType audioPlayType, float volume)
        {
            if (Mute)
            {
                return;
            }
            SoundAgent targetAgent = null;
            foreach (SoundAgent audioAgent in m_AudioAgents)
            {
                if (audioAgent.AudioState == SoundState.None)
                {
                    targetAgent = audioAgent;
                    break;
                }
            }

            if (targetAgent == null)
            {
                var audioSource = m_Root.gameObject.AddComponent<AudioSource>();
                targetAgent = new SoundAgent(audioSource, this);
                m_AudioAgents.Add(targetAgent);
            }

            targetAgent.Play(location, audioPlayType, volume);
        }

        public void Stop(string location, bool fadingOut = false)
        {
            foreach (var audioAgent in m_AudioAgents)
            {
                if (audioAgent.PlayingLocation == location)
                {
                    if (fadingOut)
                    {
                        audioAgent.StopFadingOut();
                    }
                    else
                    {
                        audioAgent.Stop();
                    }
                }
            }
        }

        public void StopAll(bool fadingOut = false)
        {
            foreach (var audioAgent in m_AudioAgents)
            {
                if (fadingOut)
                {
                    audioAgent.StopFadingOut();
                }
                else
                {
                    audioAgent.Stop();
                }
            }
        }

        public void Update()
        {
            foreach (var audioAgent in m_AudioAgents)
            {
                audioAgent.Update();
            }
        }

    }
}