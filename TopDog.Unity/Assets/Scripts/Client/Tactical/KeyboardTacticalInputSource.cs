using TopDog.Sim.Realtime;
using UnityEngine;

namespace TopDog.Client.Tactical;

public sealed class KeyboardTacticalInputSource : ITacticalInputSource
{
    public bool TryPoll(out PossessionInputSample sample)
    {
        var yaw = 0f;
        var pitch = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            yaw -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            yaw += 1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            pitch += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            pitch -= 1f;
        }
        var toggle = Input.GetKeyDown(KeyCode.Space);
        if (Mathf.Abs(yaw) < 0.01f && Mathf.Abs(pitch) < 0.01f && !toggle)
        {
            sample = default;
            return false;
        }
        sample = new PossessionInputSample
        {
            yawInput = yaw,
            pitchInput = pitch,
            toggleThrottle = toggle,
        };
        return true;
    }
}
