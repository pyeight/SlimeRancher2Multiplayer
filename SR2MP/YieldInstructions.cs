using System.Collections;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP;

public abstract class MpYieldInstruction : IEnumerator
{
    public virtual object? Current => null;

    protected abstract bool ShouldWait { get; }

    public bool MoveNext() => ShouldWait;

    public virtual void Reset() { }
}

public sealed class WaitForSceneGroupLoad : MpYieldInstruction
{
    private readonly bool targetState;

    private SceneLoader sceneLoader = SystemContext.Instance.SceneLoader;

    public override object Current => sceneLoader;

    protected override bool ShouldWait => sceneLoader.IsSceneLoadInProgress == targetState;

    public WaitForSceneGroupLoad(bool waitWhileLoading = true) => targetState = waitWhileLoading;

    public override void Reset() => sceneLoader = SystemContext.Instance.SceneLoader; // Attempts to fetch the newest instance
}

public sealed class WaitFrames : MpYieldInstruction
{
    private readonly byte framesToWait;

    private byte waited;

    protected override bool ShouldWait => waited++ < framesToWait;

    public WaitFrames(byte frames) => framesToWait = frames;

    public override void Reset() => waited = 0;
}