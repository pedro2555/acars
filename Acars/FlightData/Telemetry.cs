﻿using FSUIPC;
using System;

namespace Acars.FlightData
{
    public class Telemetry
    {
        #region Properties
        /// <summary>
        /// Offset for Airspeed
        /// </summary>
        public int airspeed;
        /// <summary>
        /// Offset for Compass
        /// </summary>
        public Double Compass;
        /// <summary>
        /// Offset for Lon features
        /// </summary>
        public long Longitude;
        /// <summary>
        /// Offset for Lat features
        /// </summary>
        public long Latitude;
        /// <summary>
        /// UTC time of collection
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// Flgiht phase
        /// </summary>
        public FlightPhases FlightPhase;
        /// <summary>
        /// Kts
        /// </summary>
        public int IndicatedAirSpeed;
        /// <summary>
        /// Angle
        /// </summary>
        public int Pitch;
        /// <summary>
        /// Angle
        /// </summary>
        public int Bank;
        /// <summary>
        /// Engine 1 running state
        /// </summary>
        public bool Engine1;
        /// <summary>
        /// Engine 2 running state
        /// </summary>
        public bool Engine2;
        /// <summary>
        /// Engine 3 running state
        /// </summary>
        public bool Engine3;
        /// <summary>
        /// Engine 4 running state
        /// </summary>
        public bool Engine4;
        /// <summary>
        /// Parking Brake state
        /// </summary>
        public bool ParkingBrake;
        /// <summary>
        /// Returns true if aircraft is on ground, false otherwise
        /// </summary>
        public bool OnGround;
        /// <summary>
        /// Vertical Speed in ft/min
        /// </summary>
        public double VerticalSpeed;
        /// <summary>
        /// Throttle position ?%
        /// </summary>
        public short Throttle;
        /// <summary>
        /// Altitude? MSL AGL ... ?, in ft most likely
        /// </summary>
        public double Altitude;
        /// <summary>
        /// Landing gear state, (true = down, false = up, ? = off)
        /// </summary>
        public bool Gear;
        /// <summary>
        /// Slew mode active
        /// </summary>
        public bool Slew;
        /// <summary>
        /// Pause state
        /// </summary>
        public bool Pause;
        /// <summary>
        /// Is aircraft in overspeed
        /// </summary>
        public bool OverSpeed;
        /// <summary>
        /// Is aircraft in stall condition
        /// </summary>
        public bool Stall;
        /// <summary>
        /// Battery switch position
        /// </summary>
        public bool Battery;
        /// <summary>
        /// Landing lights switch
        /// </summary>
        bool LandingLights;
        /// <summary>
        /// Squawk
        /// </summary>
        short Squawk;
        /// <summary>
        /// SimTime
        /// </summary>
        byte[] SimTime;
        /// <summary>
        /// SimRate
        /// </summary>
        int SimRate;
        /// <summary>
        /// Number f Engines
        /// </summary>
        int EnginesNumber;
        /// <summary>
        /// QNH
        /// </summary>
        short QNH;

        public double GrossWeight;

        public double ZeroFuelWeight;
        #endregion Properties

        public static Telemetry GetCurrent()
        {
            Telemetry result = new Telemetry();

            // snapshot data
            FSUIPCConnection.Process();
            result.Timestamp = DateTime.UtcNow;

            // capture values
            result.IndicatedAirSpeed = (FSUIPCOffsets.indicatedAirSpeed.Value / 128);
            result.Pitch = ((((FSUIPCOffsets.pitch.Value) / 360) / 65536) * 2) / -1;
            result.Bank = (((FSUIPCOffsets.bank.Value) / 360) / 65536) * 2;
            result.Engine1 = (FSUIPCOffsets.engine1.Value == 0) ? false : true;
            result.Engine2 = (FSUIPCOffsets.engine2.Value == 0) ? false : true;
            result.Engine3 = (FSUIPCOffsets.engine3.Value == 0) ? false : true;
            result.Engine4 = (FSUIPCOffsets.engine4.Value == 0) ? false : true;
            result.ParkingBrake = (FSUIPCOffsets.parkingBrake.Value == 0) ? false : true;
            result.OnGround = (FSUIPCOffsets.onGround.Value == 0) ? false : true;
            result.VerticalSpeed = (FSUIPCOffsets.verticalSpeed.Value * 3.28084) / -1;
            result.Throttle = FSUIPCOffsets.throttle.Value;
            result.Altitude = (FSUIPCOffsets.altitude.Value * 3.2808399);
            result.Gear = (FSUIPCOffsets.gear.Value == 0) ? false : true;
            result.Slew = (FSUIPCOffsets.slew.Value == 0) ? false : true;
            result.Pause = (FSUIPCOffsets.pause.Value == 0) ? false : true;
            result.OverSpeed = (FSUIPCOffsets.overSpeed.Value == 0) ? false : true;
            result.Stall = (FSUIPCOffsets.stall.Value == 0) ? false : true;
            result.Battery = (FSUIPCOffsets.battery.Value == 0) ? false : true;
            result.LandingLights = (FSUIPCOffsets.landingLights.Value == 0) ? false : true;
            result.GrossWeight = (FSUIPCOffsets.grossWeight.Value) * 0.45359237;
            result.ZeroFuelWeight = (FSUIPCOffsets.zeroFuelWeight.Value / 256) * 0.45359237;
            result.Squawk = FSUIPCOffsets.squawk.Value;
            result.SimTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, FSUIPCOffsets.SimTime.Value[0], FSUIPCOffsets.SimTime.Value[1], FSUIPCOffsets.SimTime.Value[2]);
            result.SimRate = FSUIPCOffsets.SimRate.Value;
            result.QNH = FSUIPCOffsets.QNH.Value / 16;
            result.EnginesNumber = FSUIPCOffsets.EnginesNumber.Value;

            return result;
        }
    }
}
