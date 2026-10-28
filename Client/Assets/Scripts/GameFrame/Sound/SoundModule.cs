using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameFrame
{
    public class SoundModule : GameModule<SoundModule>
    {
        private Transform m_Root;
        private AudioMixer m_AudioMixer;
        private Dictionary<SoundType, SoundGroup> m_AudioGroupDict;

        public Transform Root => m_Root;
        public AudioMixer AudioMixer => m_AudioMixer;

        
        protected override void Awake()
        {
            base.Awake();

            m_Root = gameObject.transform;

            m_AudioMixer = Resources.Load<AudioMixer>("Sound/AudioMixer");
            Resources.UnloadAsset(m_AudioMixer);
            m_AudioGroupDict = new()
            {
                [SoundType.UISound] = new SoundGroup(SoundType.UISound),
                [SoundType.Music] = new SoundGroup(SoundType.Music),
                [SoundType.Sound] = new SoundGroup(SoundType.Sound),
                [SoundType.Voice] = new SoundGroup(SoundType.Voice),
            };
        }

        /// <summary>
        /// 播放UI音效
        /// </summary>
        /// <param name="location">音效资源名</param>
        /// <param name="volume">音量大小</param>
        /// <param name="audioPlayType">播放类型</param>
        public void PlayUISound(string location, SoundPlayType audioPlayType = SoundPlayType.Once, float volume = 1f)
        {
            m_AudioGroupDict[SoundType.UISound].Play(location, audioPlayType, volume);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="location">音效资源名</param>
        /// <param name="volume">音量大小</param>
        /// <param name="audioPlayType">播放类型</param>
        public void PlaySound(string location, SoundPlayType audioPlayType = SoundPlayType.Once, float volume = 1f)
        {
            m_AudioGroupDict[SoundType.Sound].Play(location, audioPlayType, volume);
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="location">音效资源名</param>
        public void PlayMusic(string location)
        {
            m_AudioGroupDict[SoundType.Music].StopAll(true);
            m_AudioGroupDict[SoundType.Music].Play(location, SoundPlayType.Loop, 1);
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="audioType">音效类型</param>
        /// <param name="location">音效资源名</param>
        /// <param name="fadingOut">是否淡出停止</param>
        public void StopPlay(SoundType audioType, string location, bool fadingOut = false)
        {
            m_AudioGroupDict[audioType].Stop(location, fadingOut);
        }

        /// <summary>
        /// 停止播放所有
        /// </summary>
        /// <param name="audioType">音效类型</param>
        /// <param name="fadingOut">是否淡出停止</param>
        public void StopAllPlay(SoundType audioType, bool fadingOut = false)
        {
            m_AudioGroupDict[audioType].StopAll(fadingOut);
        }

        /// <summary>
        /// 设置声音大小
        /// </summary>
        /// <param name="audioType">音效类型</param>
        /// <param name="volume">音量大小</param>
        public void SetAudioSize(SoundType audioType, float volume)
        {
            m_AudioGroupDict[audioType].Volume = volume;
        }

        /// <summary>
        /// 设置主声音大小
        /// </summary>
        /// <param name="volume">音量大小</param>
        public void SetMainAudioSize(float volume)
        {
            var v = Mathf.Clamp(volume, 0.0001f, 1f);
            float volumeInDecibels = 20 * Mathf.Log10(v);
            m_AudioMixer.SetFloat("Master", volumeInDecibels);
        }

        /// <summary>
        /// 禁用音效
        /// </summary>
        /// <param name="audioType">音效类型</param>
        /// <param name="mute">是否禁用</param>
        public void MuteAudio(SoundType audioType, bool mute)
        {
            m_AudioGroupDict[audioType].Mute = mute;
        }

        /// <summary>
        /// 禁用所有音效
        /// </summary>
        /// <param name="mute">是否禁用</param>
        public void MuteAllAudio(bool mute)
        {
            foreach (var audioGroup in m_AudioGroupDict.Values)
            {
                audioGroup.Mute = mute;
            }
        }

        internal void Update()
        {
            foreach (var audioGroup in m_AudioGroupDict.Values)
            {
                audioGroup.Update();
            }
        }

        internal void Clear()
        {
            MuteAllAudio(true);
            m_AudioMixer = null;
            m_AudioGroupDict.Clear();
            Object.Destroy(m_Root.gameObject);
        }
    }
}
