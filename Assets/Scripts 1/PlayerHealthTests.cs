using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for Health.cs (Assets/Scripts 1/Health.cs) - the player's health component.
/// Health.Start() needs a valid deathUI (with a CanvasGroup) and runs on the frame after
/// the component is created, so most tests use [UnityTest] with a single `yield return null`.
/// </summary>
public class PlayerHealthTests
{
    private GameObject playerGO;
    private Health health;
    private GameObject deathUIGO;

    private IEnumerator SetUpHealth(int maxHealth = 100)
    {
        playerGO = new GameObject("PlayerUnderTest");
        health = playerGO.AddComponent<Health>();
        health.maxHealth = maxHealth;

        deathUIGO = new GameObject("DeathUI");
        deathUIGO.transform.SetParent(playerGO.transform);
        deathUIGO.AddComponent<CanvasGroup>();
        health.deathUI = deathUIGO;

        yield return null; // allow Awake()/Start() to execute
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGO != null) Object.DestroyImmediate(playerGO);
    }

    [UnityTest]
    public IEnumerator Start_InitializesCurrentHealthToMaxHealth()
    {
        yield return SetUpHealth(100);

        Assert.AreEqual(100, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator ChangeHealth_ReducesCurrentHealthByGivenAmount()
    {
        yield return SetUpHealth(100);

        health.ChangeHealth(30);

        Assert.AreEqual(70, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator ChangeHealth_DamageExceedingCurrentHealth_ClampsAtZero()
    {
        yield return SetUpHealth(50);

        health.ChangeHealth(999);

        Assert.AreEqual(0, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator ChangeHealth_MultipleHits_AccumulateCorrectly()
    {
        yield return SetUpHealth(100);

        health.ChangeHealth(10);
        health.ChangeHealth(15);
        health.ChangeHealth(5);

        Assert.AreEqual(70, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator ChangeHealth_WhenPlayerIsInvincible_NoDamageIsApplied()
    {
        yield return SetUpHealth(100);
        var dodge = playerGO.AddComponent<PlayerDodge>();
        dodge.isInvincible = true;

        health.ChangeHealth(40);

        Assert.AreEqual(100, health.currentHealth, "Damage should be ignored while invincible");
    }

    [UnityTest]
    public IEnumerator ChangeHealth_HealthReachesZero_ActivatesDeathUI()
    {
        yield return SetUpHealth(20);

        health.ChangeHealth(20);
        yield return null; // let the death fade coroutine start

        Assert.IsTrue(deathUIGO.activeSelf, "Death UI should be shown once health hits zero");
    }

    [UnityTest]
    public IEnumerator ChangeHealth_AfterDeath_FurtherDamageIsIgnored()
    {
        yield return SetUpHealth(20);

        health.ChangeHealth(20); // kills the player
        yield return null;
        health.ChangeHealth(10); // should be a no-op now

        Assert.AreEqual(0, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator Heal_IncreasesCurrentHealth()
    {
        yield return SetUpHealth(100);
        health.ChangeHealth(50); // currentHealth = 50

        health.Heal(20);

        Assert.AreEqual(70, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator Heal_AmountExceedingMax_ClampsAtMaxHealth()
    {
        yield return SetUpHealth(100);
        health.ChangeHealth(10); // currentHealth = 90

        health.Heal(50);

        Assert.AreEqual(100, health.currentHealth);
    }

    [UnityTest]
    public IEnumerator Heal_WhenPlayerIsDead_DoesNothing()
    {
        yield return SetUpHealth(10);
        health.ChangeHealth(10); // kills the player
        yield return null;

        health.Heal(50);

        Assert.AreEqual(0, health.currentHealth, "Healing a dead player should have no effect");
    }
}
