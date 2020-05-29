﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbHeatProduction : PartModule
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Heat Production", guiFormat = "N2", guiUnits = " kW", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.01f, affectSymCounterparts = UI_Scene.All)]
        public float heatProduction = 0.0f;

        [KSPField(guiActive = true, guiName = "Cabin Temperature", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double temperature = 0.0f;

        [KSPField(guiActive = true, guiName = "Cooling Thermostat", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double thermostat = 0.0f;

        [KSPField(guiActive = true, guiName = "Outside Temperature", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public string outsideTemperature = "";

        [KSPField(guiActive = true, guiName = "Skin Temperature", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double skinTemperature = 0.0f;

        [KSPField(guiActive = true, isPersistant = true, guiName = "Cabin Vent", groupDisplayName = "Environment", groupName = "bdbEnvironment"), UI_Toggle(disabledText = "Closed", enabledText = "Open")]
        public bool vented = false;

        private double saveConduction;

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            saveConduction = part.skinInternalConductionMult;
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            Fields["thermostat"].guiActive = vessel.FindPartModuleImplementing<ModuleActiveRadiator>() != null;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            double heatFlux = heatProduction;
            heatFlux += part.CrewCapacity * 0.1;
            heatFlux += part.protoModuleCrew.Count * 0.1;

            ModuleCommand mc = part.FindModuleImplementing<ModuleCommand>();
            if (mc != null)
            {
                double ec = 0.0;
                ModuleResourceHandler mcr = mc.resHandler;
                if (mcr != null && mcr.inputResources != null)
                {
                    for (int i = 0; i < mcr.inputResources.Count; i++)
                    {
                        if (mcr.inputResources[i].name == "ElectricCharge")
                        {
                            ec = mcr.inputResources[i].rate;
                        }
                    }
                }
                if (mc.hasHibernation && mc.hibernation)
                {
                    ec *= mc.hibernationMultiplier;
                }
                heatFlux += ec;
            }

            part.AddThermalFlux(heatFlux);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            temperature = KtoF(part.temperature);
            thermostat = KtoF(part.maxTemp * part.radiatorMax);

            if (vessel.staticPressurekPa > 0.0)
                outsideTemperature = KtoF(vessel.atmosphericTemperature).ToString("N1") + " F"; // KtoF(vessel.externalTemperature);
            else
                outsideTemperature = "-.- F";

            skinTemperature = KtoF(part.skinTemperature);

            if (vented)
                part.skinInternalConductionMult = 2000;
            else
                part.skinInternalConductionMult = saveConduction;
        }

        private double KtoC(double k)
        {
            return k - 273.15;
        }

        private double KtoF(double k)
        {
            return k * (9.0 / 5.0) - 459.67;
        }
    }
}