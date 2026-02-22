using System.Collections;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP;

public abstract class MpYieldInstruction : IEnumerator
{
    public object? Current => null;

    protected abstract bool ShouldWait { get; }

    public bool MoveNext() => ShouldWait;

    public virtual void Reset() { }
}

public sealed class WaitForSceneGroupLoad : MpYieldInstruction
{
    private readonly bool state;
    private SceneLoader sceneLoader = SystemContext.Instance.SceneLoader;
    
    protected override bool ShouldWait => sceneLoader.IsSceneLoadInProgress == state;

    public WaitForSceneGroupLoad(bool state = true) => this.state = state;
    
    public override void Reset() => sceneLoader = SystemContext.Instance.SceneLoader; // Attempts to fetch the newest instance
}

public sealed class WaitFrames : MpYieldInstruction
{
    private readonly byte frames;
    private byte waited;
    
    protected override bool ShouldWait
    {
        get
        {
            waited++;
            return waited >= frames;
        }
    }

    public WaitFrames(byte frames) => this.frames = frames;

    public override void Reset() => waited = 0;
}