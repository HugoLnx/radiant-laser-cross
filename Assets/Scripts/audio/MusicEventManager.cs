﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace rlc
{

    public class MusicEventManager : MonoBehaviour
    {

        public const float BPM = 135;

        public static MusicEventManager Instance;
        [SerializeField]
        public GameObject audioSourcePrefab;

        [SerializeField]
        private int minLayers = 2;
        [SerializeField]
        private int maxLayers = 4;
        [SerializeField]
        public int runningLayers = 2;


        [SerializeField]
        private float variationFrequencyInSeconds = 20f;
        private float nextVariationTime = 0f;
        [SerializeField]
        private float variationFadeTimeInSeconds = 0.25f;

        [SerializeField]
        private float transitionFadeTimeInSeconds = 0.5f;

        [SerializeField]
        private MusicTrack currentTrack;
        private List<AudioSource> currentSources = new List<AudioSource>();
        private int currentListLenght = 0;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (currentTrack != null)
            {
                StartTrack();
            }

        }

        void Update()
        {
            if (Time.time > nextVariationTime && currentTrack != null)
            {
                Variation();
            }
        }

        public void Transition(MusicTrack newTrack)
        {
            if (currentTrack != null)
            {
                StartCoroutine(TransitionTimer(newTrack));
            }
            else
            {
                currentTrack = newTrack;
                StartTrack();
            }
        }

        private void Variation()
        {
            nextVariationTime = Time.time + variationFrequencyInSeconds + SecondsToNextBeat() - variationFadeTimeInSeconds;
            int bringIn = FindUnusedNumber();
            int bringOut = FindUsedNumber();

            if (runningLayers >= currentTrack.musicStems.Length)
            {
                runningLayers = currentListLenght;
            }

            if (TracksRunning() < runningLayers)
            {
                StartCoroutine(FadeIn(currentSources[bringIn], variationFadeTimeInSeconds));
                currentSources[bringIn].gameObject.name = "MusicTrack (On)";
            }
            else if (TracksRunning() > runningLayers)
            {
                StartCoroutine(FadeOut(currentSources[bringOut], variationFadeTimeInSeconds));
                currentSources[bringOut].gameObject.name = "MusicTrack (Off)";
            }
            else
            {
                StartCoroutine(FadeIn(currentSources[bringIn], variationFadeTimeInSeconds));
                currentSources[bringIn].gameObject.name = "MusicTrack (On)";
                StartCoroutine(FadeOut(currentSources[bringOut], variationFadeTimeInSeconds));
                currentSources[bringOut].gameObject.name = "MusicTrack (Off)";
            }

        }

        private void StartTrack()
        {
            currentSources.Clear();
            currentListLenght = currentTrack.musicStems.Length - 1;
            for (int i = 0; i <= currentListLenght; i++)
            {
                AudioSource freshMusicSource = Instantiate(audioSourcePrefab).GetComponent<AudioSource>();
                freshMusicSource.gameObject.transform.parent = gameObject.transform;
                freshMusicSource.clip = currentTrack.musicStems[i];
                freshMusicSource.volume = 0f;
                freshMusicSource.Play();

                currentSources.Add(freshMusicSource);
                currentSources[i].gameObject.name = "MusicTrack (Off)";
            }

            for (int i = 0; i < runningLayers; i++)
            {
                int bringIn = FindUnusedNumber();
                currentSources[bringIn].volume = 1f;
                currentSources[bringIn].gameObject.name = "MusicTrack (On)";
            }

            nextVariationTime = Time.time + variationFrequencyInSeconds + SecondsToNextBeat() - variationFadeTimeInSeconds;

        }

        private int FindUnusedNumber()
        {
            int unusedNumber = Random.Range(0, currentListLenght);

            bool check = true;
            int exitCondition = currentListLenght;
            while (check)
            {
                check = false;
                for (int i = 0; i <= currentListLenght; i++)
                {
                    if (currentSources[i].volume > 0f)
                    {
                        unusedNumber++;
                        exitCondition--;
                        check = true;
                    }
                }

                if (unusedNumber > currentListLenght)
                {
                    unusedNumber = 0;
                    check = true;
                }

                if (exitCondition < 0)
                {
                    check = false;
                }
            }

            return unusedNumber;
        }

        private int FindUsedNumber()
        {
            int usedNumber = Random.Range(0, currentListLenght);

            bool check = true;
            int exitCondition = currentListLenght;
            while (check)
            {
                check = false;
                for (int i = 0; i <= currentListLenght; i++)
                {
                    if (currentSources[i].volume < 1f)
                    {
                        usedNumber++;
                        exitCondition--;
                        check = true;
                    }
                }

                if (usedNumber > currentListLenght)
                {
                    usedNumber = 0;
                    check = true;
                }

                if (exitCondition < 0)
                {
                    check = false;
                }
            }

            return usedNumber;
        }

        private float SecondsToNextBeat()
        {
            return currentSources[0].time % (60 / BPM);
        }

        private int TracksRunning()
        {
            int running = 0;
            for (int i = 0; i <= currentListLenght; i++)
            {
                if (currentSources[i].volume == 1f)
                {
                    running++;
                }
            }


            return running;
        }

        public int IncreaseLayers()
        {
            runningLayers++;
            if (runningLayers > maxLayers)
            {
                runningLayers = maxLayers;
            }

            return runningLayers;
        }

        public int DecreaseLayers()
        {
            runningLayers--;
            if (runningLayers < minLayers)
            {
                runningLayers = minLayers;
            }

            return runningLayers;
        }

        public int SetLayers(int numberOfLayers)
        {
            runningLayers = numberOfLayers;
            if (runningLayers > maxLayers)
            {
                runningLayers = maxLayers;
            }
            if (runningLayers < minLayers)
            {
                runningLayers = minLayers;
            }

            return runningLayers;
        }

        public void SetSecondsToTransition(float seconds)
        {
            variationFrequencyInSeconds = seconds;
        }


        IEnumerator FadeOutAndStop(AudioSource source, float fadeTime)
        {
            float startTime = Time.time;
            float currentTime = 0f;
            float startVolume = source.volume;

            while (startTime + fadeTime > Time.time)
            {
                currentTime = Time.time - startTime;

                source.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeTime);
                yield return null;
            }

            source.Stop();
            Destroy(source.gameObject);

        }

        IEnumerator FadeOut(AudioSource source, float fadeTime)
        {
            float startTime = Time.time;
            float currentTime = 0f;
            float startVolume = source.volume;

            while (startTime + fadeTime > Time.time)
            {
                currentTime = Time.time - startTime;

                source.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeTime);
                yield return null;
            }

            source.volume = 0f;

        }

        IEnumerator FadeIn(AudioSource source, float fadeTime)
        {
            float startTime = Time.time;
            float currentTime = 0f;
            float startVolume = source.volume;

            while (startTime + fadeTime > Time.time)
            {
                currentTime = Time.time - startTime;

                source.volume = Mathf.Lerp(startVolume, 1f, currentTime / fadeTime);
                yield return null;
            }

            source.volume = 1f;

        }

        IEnumerator TransitionTimer(MusicTrack newTrack)
        {
            float goTime = Time.time + SecondsToNextBeat();

            while (Time.time < goTime)
            {
                yield return null;
            }

            for (int i = 0; i <= currentListLenght; i++)
            {
                StartCoroutine(FadeOutAndStop(currentSources[i], transitionFadeTimeInSeconds));
                currentSources[i].gameObject.name = "MusicTrack (Old)";
            }

            currentTrack = newTrack;
            StartTrack();

        }


    }

}