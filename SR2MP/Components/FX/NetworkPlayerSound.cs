using MelonLoader;
using SR2MP.Packets.FX;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkPlayerSound : MonoBehaviour
{
    public PlayerFXType fxType;

    private bool cachedIsPlaying;
    private SECTR_AudioCue cachedAudioCue;
    private SECTR_PointSource audioSource;

    public bool IsPlaying => audioSource.IsPlaying && !audioSource.instance.Paused;
    public SECTR_AudioCue AudioCue => audioSource.Cue;

    private void Awake()
    {
        audioSource = GetComponent<SECTR_PointSource>();
    }

    private void Update()
    {
        var hasChanged =  IsPlaying != cachedIsPlaying || AudioCue != cachedAudioCue;

        cachedIsPlaying = IsPlaying;
        cachedAudioCue = AudioCue;

        if (!hasChanged)
            return;

        SendPacket();
    }

    private void SendPacket()
    {
        // Defaults to PlayerFXType.None
        if (!fxManager.TryGetFXType(audioSource.Cue, out fxType))
        {
            return;
        }

        var packet = new PlayerFXPacket
        {
            FX = fxType,
            Player = LocalID
        };
        Main.SendToAllOrServer(packet);
    }
}