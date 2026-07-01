using TopDog.App.Brick;
using TopDog.Content.Banter;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>团员伴聊：只读观测 + 唯一写入 companionLog。</summary>
public sealed class MemberBanterBrick : IBrick
{
    private MemberBanterService? _service;
    private float _simTimeSec;

    public string Id() => "member_banter";

    public void OnRegister(BrickContext ctx)
    {
        _service = new MemberBanterService(BanterCatalogLoader.LoadDefault(), seed: 42);
        var rt = ctx.State.banterRuntime ??= new MemberBanterRuntimeState();
        if (rt.idleNextEmitSec <= 0f)
        {
            rt.idleNextEmitSec = 12f;
        }
    }

    public void Tick(BrickContext ctx, float dtSec)
    {
        _service ??= new MemberBanterService(BanterCatalogLoader.LoadDefault());
        _simTimeSec += dtSec;
        _service.Tick(ctx.State, dtSec, _simTimeSec);
    }
}
