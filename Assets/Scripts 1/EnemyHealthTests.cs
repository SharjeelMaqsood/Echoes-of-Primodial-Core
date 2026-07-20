using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for EnemyHealth.cs (Assets/Scripts 1/Enemy Ai/EnemyHealth.cs).
/// currentHealth is a private field with no public accessor, so tests read it
/// via reflection purely for assertions (production code is not modified).
///
/// EnemyHealth.DeathRoutine() calls GetComponent<SwordEnemyAi>().enabled = false,
/// so every enemy under test needs a SwordEnemyAi component. It is disabled
/// immediately so its own Update()/NavMesh logic never runs during the test.
/// </summary>
public class EnemyHealthTests
{
    private GameObject enemyGO;
    private EnemyHealth enemyHealth;

    private IEnumerator SetUpEnemy(int maxHealth = 100)
    {
        enemyGO = new GameObject("EnemyUnderTest");
        enemyHealth = enemyGO.AddComponent<EnemyHealth>();
        enemyHealth.maxHealth = maxHealth;

        var ai = enemyGO.AddComponent<SwordEnemyAi>();
        ai.enabled = false;

        yield return null; // allow Start() to run and initialize currentHealth
    }

    [TearDown]
    public void TearDown()
    {
        if (enemyGO != null) Object.DestroyImmediate(enemyGO);
    }

    private int GetCurrentHealth(EnemyHealth target)
    {
        FieldInfo field = typeof(EnemyHealth).GetField(
            "currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)field.GetValue(target);
    }

    [UnityTest]
    public IEnumerator Start_InitializesCurrentHealthToMaxHealth()
    {
        yield return SetUpEnemy(100);

        Assert.AreEqual(100, GetCurrentHealth(enemyHealth));
    }

    [UnityTest]
    public IEnumerator TakeDamage_ReducesCurrentHealth()
    {
        yield return SetUpEnemy(100);

        enemyHealth.TakeDamage(25);

        Assert.AreEqual(75, GetCurrentHealth(enemyHealth));
    }

    [UnityTest]
    public IEnumerator TakeDamage_PartialDamage_EnemyRemainsAlive()
    {
        yield return SetUpEnemy(100);

        enemyHealth.TakeDamage(40);

        Assert.IsFalse(enemyHealth.isDead);
    }

    [UnityTest]
    public IEnumerator TakeDamage_MultipleHits_AccumulateCorrectly()
    {
        yield return SetUpEnemy(100);

        enemyHealth.TakeDamage(10);
        enemyHealth.TakeDamage(15);
        enemyHealth.TakeDamage(5);

        Assert.AreEqual(70, GetCurrentHealth(enemyHealth));
    }

    [UnityTest]
    public IEnumerator TakeDamage_ExactlyLethal_SetsIsDeadTrue()
    {
        yield return SetUpEnemy(50);

        enemyHealth.TakeDamage(50);
        yield return null; // DeathRoutine coroutine starts on this frame

        Assert.IsTrue(enemyHealth.isDead);
    }

    [UnityTest]
    public IEnumerator TakeDamage_ExceedsRemainingHealth_StillTriggersDeath()
    {
        yield return SetUpEnemy(30);

        enemyHealth.TakeDamage(999);
        yield return null;

        Assert.IsTrue(enemyHealth.isDead);
    }

    [UnityTest]
    public IEnumerator TakeDamage_WhenAlreadyDead_IgnoresFurtherDamage()
    {
        yield return SetUpEnemy(20);
        enemyHealth.TakeDamage(20); // kills the enemy
        yield return null;
        int healthAtDeath = GetCurrentHealth(enemyHealth);

        enemyHealth.TakeDamage(50); // should be a no-op

        Assert.AreEqual(healthAtDeath, GetCurrentHealth(enemyHealth));
    }

    [UnityTest]
    public IEnumerator DeathRoutine_DestroysGameObjectAfterDelay()
    {
        yield return SetUpEnemy(10);

        enemyHealth.TakeDamage(10);

        // DeathRoutine waits ~2.2s before calling Destroy(gameObject).
        yield return new WaitForSeconds(2.5f);

        Assert.IsTrue(enemyGO == null, "Enemy GameObject should be destroyed after the death delay");
        enemyGO = null; // prevent TearDown from double-destroying
    }
}
