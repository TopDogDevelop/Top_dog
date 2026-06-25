using TopDog.App.Brick;
using TopDog.Foundation.Bus;

namespace TopDog.Sim.Tutorial;

public sealed class TutorialOpsBrick : IBrick
{
    private static readonly string[] Steps =
    {
        "欢迎：这是运营阶段教学。输入「帮助」查看命令",
        "试试「状态」查看军团与星图",
        "输入「继续」进入下一步",
        "阶段 2：用「装备 <团员> hull_bc_spear」配船",
        "用「前往 Mine Field」跃迁到另一星系（需已配船）",
        "教程运营骨架完成。输入「继续」结束",
    };

    public string Id() => "tutorial.ops";

    public void OnRegister(BrickContext ctx)
    {
        if (ctx.State.worldline.tutorialMode && ctx.State.tutorialStep == 0)
        {
            PushStep(ctx, 0);
        }
    }

    public void Tick(BrickContext ctx, float dtSec) { }

    public bool Advance(BrickContext ctx)
    {
        if (!ctx.State.worldline.tutorialMode || ctx.State.tutorialComplete)
        {
            return false;
        }
        var next = ctx.State.tutorialStep + 1;
        if (next >= Steps.Length)
        {
            ctx.State.tutorialComplete = true;
            ctx.Bus.Publish(GameEvent.Of("tutorial.complete", "ch01_ops"));
            PushAlert(ctx, "教程1 章运营教学完成");
            return true;
        }
        ctx.State.tutorialStep = next;
        PushStep(ctx, next);
        return true;
    }

    private static void PushStep(BrickContext ctx, int idx)
    {
        var msg = Steps[Math.Min(idx, Steps.Length - 1)];
        PushAlert(ctx, "[教程] " + msg);
        ctx.Bus.Publish(GameEvent.Of("tutorial.step", idx.ToString()));
    }

    private static void PushAlert(BrickContext ctx, string msg)
    {
        ctx.State.alertLog.Add(msg);
        if (ctx.State.alertLog.Count > 50)
        {
            ctx.State.alertLog.RemoveAt(0);
        }
    }
}
