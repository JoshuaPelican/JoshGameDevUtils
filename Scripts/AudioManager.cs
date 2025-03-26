using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioMixer audioMixer;

    AudioSource[] sources;
    int nextSourceIndex;
    Dictionary<int, AudioSource> activeSources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSources();
    }

    void InitializeSources()
    {
        nextSourceIndex = -1;
        activeSources = new Dictionary<int, AudioSource>();
        sources = new AudioSource[AudioSettings.GetConfiguration().numRealVoices];

        for (int i = 0; i < sources.Length; i++)
        {
            GameObject sourceGO = new GameObject($"Audio Source {i}");
            sourceGO.transform.SetParent(transform);
            sources[i] = sourceGO.AddComponent<AudioSource>();

            // Configure for 3D audio
            sources[i].loop = false;
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 1f; // 3D sound
            sources[i].rolloffMode = AudioRolloffMode.Linear;
            sources[i].minDistance = 1f; // Default min distance
            sources[i].maxDistance = 20f; // Default max distance
            sources[i].dopplerLevel = 0f; // Disable doppler effect by default
        }
    }

    AudioSource GetNextSource()
    {
        int startIndex = nextSourceIndex;
        do
        {
            nextSourceIndex = (nextSourceIndex + 1) % sources.Length;
            if (!sources[nextSourceIndex].isPlaying)
                return sources[nextSourceIndex];

        } while (nextSourceIndex != startIndex);

        // If all sources are busy, return the next one anyway (will interrupt oldest sound)
        return sources[nextSourceIndex];
    }

    public int PlayClip2D(AudioClip clip, float volume = 1f, float pitch = 1f,
    bool loop = false, string mixerGroupName = null)
    {
        return PlayClip(clip, Vector2.zero, volume, pitch, loop, 0, 0, 1, mixerGroupName);
    }

    public int PlayClip3D(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
        bool loop = false, float minDistance = 1f, float maxDistance = 20f,
        string mixerGroupName = null)
    {
        return PlayClip(clip, position, volume, pitch, loop, 1, minDistance, maxDistance, mixerGroupName);
    }

    int PlayClip(AudioClip clip, Vector3 position, float volume, float pitch,
        bool loop, float spacialBlend, float minDistance, float maxDistance,
        string mixerGroupName = null)
    {
        AudioSource source = GetNextSource();
        source.transform.position = position;
        source.spatialBlend = spacialBlend;

        AudioMixerGroup mixerGroup = GetMixerGroupByName(mixerGroupName);
        ConfigureSource(source, clip, volume, pitch, loop, minDistance, maxDistance, mixerGroup);

        int sourceID = source.GetInstanceID();
        activeSources[sourceID] = source;

        source.Play();
        return sourceID;
    }

    private AudioMixerGroup GetMixerGroupByName(string mixerGroupName)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioMixer is not assigned in the AudioManager.");
            return null;
        }

        if (!string.IsNullOrEmpty(mixerGroupName))
        {
            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(mixerGroupName);
            if (groups != null && groups.Length > 0)
            {
                return groups[0]; // Return the first matching group
            }
            else
            {
                Debug.LogWarning($"Mixer group '{mixerGroupName}' not found in the assigned AudioMixer.");
            }
        }

        return null; // No mixer group specified or found
    }

    private void ConfigureSource(AudioSource source, AudioClip clip, float volume, float pitch,
        bool loop, float minDistance, float maxDistance, AudioMixerGroup mixerGroup)
    {
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = loop;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.outputAudioMixerGroup = mixerGroup;
    }

    // Pause a specific sound
    public void PauseSound(int sourceID)
    {
        if (activeSources.TryGetValue(sourceID, out AudioSource source))
        {
            source.Pause();
        }
    }

    // Resume a paused sound
    public void ResumeSound(int sourceID)
    {
        if (activeSources.TryGetValue(sourceID, out AudioSource source))
        {
            source.UnPause();
        }
    }

    // Stop a specific sound
    public void StopSound(int sourceID)
    {
        if (activeSources.TryGetValue(sourceID, out AudioSource source))
        {
            source.Stop();
            activeSources.Remove(sourceID);
        }
    }

    // Clean up finished sources
    private void FixedUpdate()
    {
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeSources)
        {
            if (!kvp.Value.isPlaying && !kvp.Value.loop)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            activeSources.Remove(id);
        }
    }
}