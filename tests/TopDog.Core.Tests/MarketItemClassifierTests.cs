using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MarketItemClassifierTests
{
    private ModuleRegistry _modules = null!;
    private ShipRegistry _ships = null!;

    [SetUp]
    public void SetUp()
    {
        _modules = ModuleRegistry.LoadDefault();
        _ships = ShipRegistry.LoadDefault();
    }

    [Test]
    public void Classify_Hull_IsShip()
    {
        Assert.That(
            MarketItemClassifier.CategoryId(MarketItemClassifier.Classify("hull_bc_spear", _modules, _ships)),
            Is.EqualTo("ship"));
    }

    [Test]
    public void Classify_Resource_IsMaterial()
    {
        Assert.That(
            MarketItemClassifier.CategoryId(MarketItemClassifier.Classify("res_inorganic", _modules, _ships)),
            Is.EqualTo("material"));
    }

    [Test]
    public void Classify_AttackModule_IsAttack()
    {
        Assert.That(
            MarketItemClassifier.CategoryId(MarketItemClassifier.Classify("mod_hybrid_gun_m", _modules, _ships)),
            Is.EqualTo("attack"));
    }

    [Test]
    public void Classify_DefenseModule_IsDefense()
    {
        Assert.That(
            MarketItemClassifier.CategoryId(MarketItemClassifier.Classify("mod_shield_regen_m", _modules, _ships)),
            Is.EqualTo("defense"));
    }

    [Test]
    public void Classify_FunctionModule_IsFunction()
    {
        Assert.That(
            MarketItemClassifier.CategoryId(MarketItemClassifier.Classify("mod_propulsion_m", _modules, _ships)),
            Is.EqualTo("function"));
    }
}
