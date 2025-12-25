using Il2Cpp;
using MelonLoader;
using SR2MP.Packets.Utils;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public class NetworkPlayerSound : MonoBehaviour
{
    public PlayerFXType fxType;

    private bool cachedIsPlaying = false;
    private SECTR_AudioCue cachedAudioCue;
    private SECTR_PointSource audioSource;

    public bool IsPlaying => audioSource.IsPlaying && !audioSource.instance.Paused;
    public SECTR_AudioCue AudioCue => audioSource.Cue;
    
    private void Start()
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
    
    void SendPacket()
    {
        // Defaults to PlayerFXType.None
        if (!fxManager.TryGetFXType(audioSource.Cue, out fxType))
        {
            return;
        }
        
        var packet = new PlayerFXPacket()
        {
            Type = (byte)PacketType.PlayerFX,
            FX = fxType,
            Player = LocalID
        };
        Main.SendToAllOrServer(packet);
    }
}