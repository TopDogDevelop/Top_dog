using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>
/// 模拟同步器：主发言人必出；同 tick 复读人数按档位掷骰——
/// 1 号 89%、2 号 10%、3 号 1%、4 号及以上 0%（主号计入人数）。
/// </summary>
public static class BanterMultiboxSync
{
  public const int TwoSpeakerChancePercent = 10;
  public const int ThreeSpeakerChancePercent = 1;
  public const int MaxSpeakerCount = 3;

  /// <summary>掷出本次发言的同 identity 账号总数（1～3）。</summary>
  public static int RollSpeakerCount(Random rng)
  {
    var roll = rng.Next(100);
    if (roll < ThreeSpeakerChancePercent)
    {
      return 3;
    }

    if (roll < ThreeSpeakerChancePercent + TwoSpeakerChancePercent)
    {
      return 2;
    }

    return 1;
  }

  /// <summary>
  /// 主发言人必出；同 identity 其它账号按 <see cref="RollSpeakerCount"/> 决定跟读数量（随机选取，不重复）。
  /// </summary>
  public static List<string> CollectSyncSpeakers(
      IReadOnlyList<MemberState> eligiblePool,
      string primaryMemberId,
      Random rng)
  {
    var speakerIds = new List<string> { primaryMemberId };
    var identity = ResolveIdentity(eligiblePool, primaryMemberId);
    if (identity == null)
    {
      return speakerIds;
    }

    var siblings = new List<string>();
    foreach (var m in eligiblePool)
    {
      if (string.IsNullOrWhiteSpace(m.memberId)
          || primaryMemberId.Equals(m.memberId, StringComparison.Ordinal))
      {
        continue;
      }

      if (identity.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
      {
        siblings.Add(m.memberId);
      }
    }

    if (siblings.Count == 0)
    {
      return speakerIds;
    }

    var desired = RollSpeakerCount(rng);
    var capped = Math.Min(desired, Math.Min(MaxSpeakerCount, 1 + siblings.Count));
    var extra = capped - 1;
    if (extra <= 0)
    {
      return speakerIds;
    }

    var pool = new List<string>(siblings);
    for (var i = 0; i < extra && pool.Count > 0; i++)
    {
      var idx = rng.Next(pool.Count);
      speakerIds.Add(pool[idx]);
      pool.RemoveAt(idx);
    }

    speakerIds.Sort(StringComparer.Ordinal);
    return speakerIds;
  }

  private static string? ResolveIdentity(IReadOnlyList<MemberState> pool, string memberId)
  {
    foreach (var m in pool)
    {
      if (memberId.Equals(m.memberId, StringComparison.Ordinal))
      {
        return IdentityCodes.Of(m);
      }
    }

    return null;
  }
}
