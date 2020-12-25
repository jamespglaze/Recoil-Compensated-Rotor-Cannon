using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        string abrazorgrouptag = "Abrazor";
        int massAmount = 585;
        List<int> step = new List<int>();
        List<bool> cannonActive = new List<bool>();
        List<IMyBlockGroup> abrazors = new List<IMyBlockGroup>();
        List<IMyMotorAdvancedStator> cannonDrivers = new List<IMyMotorAdvancedStator>();
        List<IMyMotorStator> endDrivers = new List<IMyMotorStator>();
        List<IMyCargoContainer> recoilDampers = new List<IMyCargoContainer>();
        List<IMyUserControllableGun> fireIndicators = new List<IMyUserControllableGun>();

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument)
        {
            int index = 0;
            string[] temp = Me.CustomData.TrimEnd('\n').Split(',', '\n');
            GridTerminalSystem.GetBlockGroups(abrazors, abrazor => abrazor.Name.Contains(abrazorgrouptag));
            foreach (IMyBlockGroup abrazor in abrazors)
            {
                if (cannonActive.Count <= index)
                    cannonActive.Insert(index, false);
                if (step.Count <= index)
                    step.Insert(index, 0);

                abrazor.GetBlocksOfType(fireIndicators);
                foreach (IMyUserControllableGun fireIndicator in fireIndicators)
                {
                    if (fireIndicator.IsShooting)
                    {
                        cannonActive[index] = true;
                        break;
                    }
                    else
                        cannonActive[index] = false;
                }

                if (fireIndicators.Count == 0)
                {
                    abrazor.GetBlocksOfType(endDrivers, endDriver => endDriver.CustomName.Contains("Rotor End"));
                    foreach (IMyMotorStator endDriver in endDrivers)
                    {
                        for (int i = 0; i < temp.Length / 2; i++)
                        {
                            if (temp[i * 2 + 1] == endDriver.CustomName.Substring(0, 4))
                            {
                                if (temp[i * 2] == "run")
                                    cannonActive[index] = true;
                                if (temp[i * 2] == "stop")
                                    cannonActive[index] = false;
                            }
                        }
                    }
                }

                if (cannonActive[index] || step[index] == 0)
                {
                    abrazor.GetBlocksOfType(endDrivers, endDriver => endDriver.CustomName.Contains("Rotor End"));
                    abrazor.GetBlocksOfType(cannonDrivers);
                    abrazor.GetBlocksOfType(recoilDampers);
                    CycleCannon(cannonDrivers, endDrivers, recoilDampers, step[index], cannonActive[index], massAmount);
                }
                if (cannonActive[index] && step[index] == 0)
                    step[index] = 1;
                else
                    step[index] = 0;

                index = index + 1;
            }
        }

        public void CycleCannon(List<IMyMotorAdvancedStator> cannonDrivers, List<IMyMotorStator> endDrivers, List<IMyCargoContainer> recoilContainers, int step, bool cannonActive, int massAmount)
        {
            switch (step)
            {
                case 0:

                    foreach (IMyMotorAdvancedStator cannonDriver in cannonDrivers)
                        cannonDriver.SetValue<float>("Displacement", -0.4f);

                    foreach (IMyMotorStator endDriver in endDrivers)
                        endDriver.ApplyAction("Detach");

                    recoilDampers.Find(cargo => cargo.CustomName.Contains("Base")).GetInventory().TransferItemFrom(recoilDampers.Find(cargo => cargo.CustomName.Contains("End")).GetInventory(), 0, 0);
                    break;
                case 1:
                    if (cannonActive)
                    {
                        foreach (IMyMotorAdvancedStator cannonDriver in cannonDrivers)
                            cannonDriver.Displacement = 0f;
                        foreach (IMyMotorStator endDriver in endDrivers)
                            endDriver.ApplyAction("AddRotorTopPart");
                        recoilDampers.Find(cargo => cargo.CustomName.Contains("End")).GetInventory().TransferItemFrom(recoilDampers.Find(cargo => cargo.CustomName.Contains("Base")).GetInventory(), 0, 0, true, massAmount);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}