using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.FX;
using Starlight.Storage;

namespace SR2MP.Components.FX;

[InjectIntoIL]
internal sealed class NetworkPlayerSound : MonoBehaviour
{
    public PlayerFXType FXType;

    private bool cachedIsPlaying;
    private SECTR_AudioCue cachedAudioCue;
    private SECTR_PointSource audioSource;

    public bool IsPlaying => audioSource.IsPlaying && !audioSource.instance.Paused;
    public SECTR_AudioCue AudioCue => audioSource.Cue;

    [UsedImplicitly]
    public void Awake() => audioSource = GetComponent<SECTR_PointSource>();

    public void Update()
    {
        var hasChanged =  IsPlaying != cachedIsPlaying || AudioCue != cachedAudioCue;

        cachedIsPlaying = IsPlaying;
        cachedAudioCue = AudioCue;

        if (!hasChanged)
            return;

        // Defaults to PlayerFXType.None
        if (!FXManager.TryGetFXType(audioSource.Cue, out FXType))
            return;

        var packet = new PlayerFXPacket
        {
            FX = FXType,
            Player = LocalID
        };
        Main.SendToAllOrServer(packet);
    }
}