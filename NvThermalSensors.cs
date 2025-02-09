﻿using System.Collections.Generic;

namespace FanControl.NvThermalSensors
{
    internal class NvThermalSensors
    {
        internal NvThermalSensors(int gpuIndex, NvApi.NvPhysicalGpuHandle handle)
        {
            Sensors = new List<NvThermalSensor>();

            var mask = FindThermalSensorMask(handle);
            if (mask == 0)
                return;

            var gpuName = GetGpuName(handle, gpuIndex);

            if (GetSensorValue(handle, mask, 1) != 0)
                Sensors.Add(new NvThermalSensor(gpuIndex, $"{gpuName} - Hot Spot", () => GetSensorValue(handle, mask, 1)));
            if (GetSensorValue(handle, mask, 9) != 0)
            {
                Sensors.Add(new NvThermalSensor(gpuIndex, $"{gpuName} - Memory Junction", () => GetSensorValue(handle, mask, 9)));
                Sensors.Add(new NvThermalSensor(gpuIndex, $"{gpuName} - Memory Junction -5 offset", () => GetSensorValue(handle, mask, 9) - 5));
                Sensors.Add(new NvThermalSensor(gpuIndex, $"{gpuName} - Memory Junction -7 offset", () => GetSensorValue(handle, mask, 9) - 7));
                Sensors.Add(new NvThermalSensor(gpuIndex, $"{gpuName} - Memory Junction -10 offset", () => GetSensorValue(handle, mask, 9) - 10));
            }
        }

        internal List<NvThermalSensor> Sensors { get; }

        private uint FindThermalSensorMask(NvApi.NvPhysicalGpuHandle handle)
        {
            uint mask = 0;
            for (var thermalSensorsMaxBit = 0; thermalSensorsMaxBit < 32; thermalSensorsMaxBit++)
            {
                mask = 1u << thermalSensorsMaxBit;

                GetThermalSensors(handle, mask, out NvApi.NvStatus thermalSensorsStatus);
                if (thermalSensorsStatus != NvApi.NvStatus.OK)
                    break;
            }

            return --mask;
        }

        private NvApi.NvThermalSensors GetThermalSensors(NvApi.NvPhysicalGpuHandle handle, uint mask, out NvApi.NvStatus status)
        {
            var thermalSensors = new NvApi.NvThermalSensors()
            {
                Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvThermalSensors>(2),
                Mask = mask
            };

            status = NvApi.NvAPI_GPU_ThermalGetSensors(handle, ref thermalSensors);
            return status == NvApi.NvStatus.OK ? thermalSensors : default;
        }

        private string GetGpuName(NvApi.NvPhysicalGpuHandle handle, int gpuIndex)
        {
            var gpuName = new NvApi.NvShortString();
            var status = NvApi.NvAPI_GPU_GetFullName(handle, ref gpuName);

            return status == NvApi.NvStatus.OK ? gpuName.Value.Trim() : $"GPU - {gpuIndex}";
        }

        private float GetSensorValue(NvApi.NvPhysicalGpuHandle handle, uint mask, int index)
        {
            return GetThermalSensors(handle, mask, out _).Temperatures[index] / 256.0f;
        }
    }
}
