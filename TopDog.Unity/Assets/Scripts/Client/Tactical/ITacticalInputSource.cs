using TopDog.Sim.Realtime;

namespace TopDog.Client.Tactical;

public interface ITacticalInputSource
{
    bool TryPoll(out PossessionInputSample sample);
}
