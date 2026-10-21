using UnityEngine;

namespace GameEngine
{
    public class SoundAgent
    {
        private const float FadeoutDuration = 0.2f;

        private SoundGroup m_AudioGroup;
        private AudioSource m_AudioSource;

        private float m_Volume;
        private float m_FadeoutTimer;
        private string m_CacheLocation;

        private SoundPlayType m_AudioPlayType;
        private SoundState m_AudioState = SoundState.None;

        public string PlayingLocation => m_CacheLocation;

        public SoundState AudioState => m_AudioState;

        public SoundAgent(AudioSource audioSource, SoundGroup audioGroup)
        {
            this.m_AudioSource = audioSource;
            this.m_AudioGroup = audioGroup;
            this.m_AudioSource.playOnAwake = false;
            this.m_AudioSource.outputAudioMixerGroup = audioGroup.AudioMixerGroup;
        }

        public void Play(string location, SoundPlayType audioPlayType, float volume)
        {
            m_AudioState = SoundState.Loading;
            m_CacheLocation = location;
            this.m_Volume = volume;
            this.m_AudioPlayType = audioPlayType;

            ResourceModule.Instance.LoadAssetAsync<AudioClip>(location, (audioClip) =>
            {
                m_AudioSource.volume = this.m_Volume;
                m_AudioSource.clip = audioClip;
                m_AudioSource.loop = audioPlayType == SoundPlayType.Loop;
                m_AudioSource.Play();
                m_AudioState = SoundState.Playing;
            }).Forget();
        }

        public void StopFadingOut()
        {
            if (m_AudioState != SoundState.Playing)
            {
                return;
            }
            m_FadeoutTimer = FadeoutDuration;
            m_AudioState = SoundState.FadingOut;
        }

        public void Stop()
        {
            m_AudioSource.Stop();
            m_AudioSource.clip = null;
            m_AudioState = SoundState.None;
            ResourceModule.Instance.UnloadAssetReference(m_CacheLocation);
        }

        public void Update()
        {
            if (m_AudioState == SoundState.Playing)
            {
                if (m_AudioPlayType == SoundPlayType.Once)
                {
                    if (!m_AudioSource.isPlaying)
                    {
                        Stop();
                    }
                }
                else if (m_AudioPlayType == SoundPlayType.Loop)
                {
                    //循环播放不做处理
                }
            }
            else if (m_AudioState == SoundState.FadingOut)
            {
                if (!m_AudioSource.isPlaying)
                {
                    Stop();
                }
                else if (m_FadeoutTimer > 0)
                {
                    m_FadeoutTimer -= Time.deltaTime;
                    m_AudioSource.volume = m_Volume * (m_FadeoutTimer / FadeoutDuration);
                }
                else
                {
                    Stop();
                }
            }
        }
    }
}