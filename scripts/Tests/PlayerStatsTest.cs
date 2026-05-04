using Godot;
using System;
using umaEraArchive.Game;

namespace umaEraArchive.Tests;

/// <summary>
/// PlayerStats 自动化测试
/// </summary>
public partial class PlayerStatsTest : Node
{
    public override void _Ready()
    {
        GD.Print("\n[Test] === Starting PlayerStats Tests ===\n");

        try
        {
            VerifyConsumeMoney();
            VerifyConsumeStamina();
            VerifyConsumeEnergy();

            GD.Print("\n[Test] === PlayerStats Tests Passed! ===\n");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[Test] !!! PlayerStats Test Failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void VerifyConsumeMoney()
    {
        GD.Print("[Test] Verifying ConsumeMoney...");

        var stats = new PlayerStats();
        stats.Money = 100;

        // Test exact amount
        if (!stats.ConsumeMoney(100)) throw new Exception("Failed to consume exact money.");
        if (stats.Money != 0) throw new Exception("Money should be 0 after consuming exact amount.");

        // Reset
        stats.Money = 100;

        // Test normal amount
        if (!stats.ConsumeMoney(40)) throw new Exception("Failed to consume normal money.");
        if (stats.Money != 60) throw new Exception($"Money should be 60, got {stats.Money}.");

        // Test failure condition
        if (stats.ConsumeMoney(100)) throw new Exception("Should fail to consume more money than available.");
        if (stats.Money != 60) throw new Exception("Money should not change on failed consumption.");

        GD.Print("[Test] ConsumeMoney OK.");
    }

    private void VerifyConsumeStamina()
    {
        GD.Print("[Test] Verifying ConsumeStamina...");

        var stats = new PlayerStats();
        stats.Stamina = 100;

        // Test exact amount
        if (!stats.ConsumeStamina(100)) throw new Exception("Failed to consume exact stamina.");
        if (stats.Stamina != 0) throw new Exception("Stamina should be 0 after consuming exact amount.");

        // Reset
        stats.Stamina = 100;

        // Test normal amount
        if (!stats.ConsumeStamina(30)) throw new Exception("Failed to consume normal stamina.");
        if (stats.Stamina != 70) throw new Exception($"Stamina should be 70, got {stats.Stamina}.");

        // Test failure condition
        if (stats.ConsumeStamina(100)) throw new Exception("Should fail to consume more stamina than available.");
        if (stats.Stamina != 70) throw new Exception("Stamina should not change on failed consumption.");

        GD.Print("[Test] ConsumeStamina OK.");
    }

    private void VerifyConsumeEnergy()
    {
        GD.Print("[Test] Verifying ConsumeEnergy...");

        var stats = new PlayerStats();
        stats.Energy = 100;

        // Test exact amount
        if (!stats.ConsumeEnergy(100)) throw new Exception("Failed to consume exact energy.");
        if (stats.Energy != 0) throw new Exception("Energy should be 0 after consuming exact amount.");

        // Reset
        stats.Energy = 100;

        // Test normal amount
        if (!stats.ConsumeEnergy(20)) throw new Exception("Failed to consume normal energy.");
        if (stats.Energy != 80) throw new Exception($"Energy should be 80, got {stats.Energy}.");

        // Test failure condition
        if (stats.ConsumeEnergy(100)) throw new Exception("Should fail to consume more energy than available.");
        if (stats.Energy != 80) throw new Exception("Energy should not change on failed consumption.");

        GD.Print("[Test] ConsumeEnergy OK.");
    }
}
