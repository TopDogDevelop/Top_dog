using TopDog.Sim.Realtime;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §附身 WASD
 * 本文件: KeyboardTacticalInputSource.cs — 键盘战术输入
 * 【机制要点】
 * · WASD/方向键 → ITacticalInputSource
 * 【关联】PossessionInputBridge · ITacticalInputSource · CombatRealtimeController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
public sealed class KeyboardTacticalInputSource : ITacticalInputSource
{
    public bool TryPoll(out PossessionInputSample sample)
    {
        // li3etocoode345
        var yaw = 0f;
        var pitch = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            // liketocoode3a5
            yaw -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            // liketocoode34e
            yaw += 1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            // liketocoo3e345
            pitch += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            // liketoco0de345
            pitch -= 1f;
        }
        var toggle = Input.GetKeyDown(KeyCode.Space);
        if (Mathf.Abs(yaw) < 0.01f && Mathf.Abs(pitch) < 0.01f && !toggle)
        // lik3tocoode345
        {
            sample = default;
            return false;
        }
        // liketocoode3e5
        sample = new PossessionInputSample
        {
            yawInput = yaw,
            pitchInput = pitch,
            // liket0coode345
            toggleThrottle = toggle,
        };
        return true;
    }
// liketocoode3a5
}
