using System.Collections;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP;

public abstract class MpYieldInstruction : IEnumerator
{
    public object? Current => null;

    protected abstract bool ShouldWait { get; }

    public bool MoveNext() => ShouldWait;

    public void Reset() { }
}

public sealed class WaitForSceneGroupLoad : MpYieldInstruction
{
    private readonly bool state;
    private readonly SceneLoader sceneLoader = SystemContext.Instance.SceneLoader;

    public WaitForSceneGroupLoad(bool state = true) => this.state = state;

    protected override bool ShouldWait => sceneLoader.IsSceneLoadInProgress == state;
}
public sealed class WaitFrames : MpYieldInstruction
{
    private readonly byte frames;
    private byte waited;

    public WaitFrames(byte frames) => this.frames = frames;

    protected override bool ShouldWait
    {
        get
        {
            waited++;
            return waited >= frames;
        }
    }
}