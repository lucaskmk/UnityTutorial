using UnityEngine;

// Som de clique dos botões. Fica num AudioSource que ignora a pausa do AudioListener.
public class UIAudio : MonoBehaviour
{
    static UIAudio instance;

    public AudioClip clickClip;
    AudioSource source;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        source.ignoreListenerPause = true;
    }

    public static void Click()
    {
        if (instance != null) instance.source.PlayOneShot(instance.clickClip);
    }
}
