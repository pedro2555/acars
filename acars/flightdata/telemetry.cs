﻿using FSUIPC;
using System;
using System.Device.Location;

namespace Acars.FlightData
{
    public class Telemetry
    {
        #region Properties
        /// <summary>
        /// Offset for Compass
        /// </summary>
        public Double Compass;
        /// <summary>
        /// Offset for Lon features
        /// </summary>
        public double Longitude;
        /// <summary>
        /// Offset for Lat features
        /// </summary>
        public double Latitude;
        /// <summary>
        /// 
        /// </summary>
        public GeoCoordinate Location;
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
        /// Parking Brake state
        /// </summary>
        public int ParkingBrakeWrite;
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
        public int Slew;
        /// <summary>
        /// Pause state
        /// </summary>
        public bool Pause;
        /// <summary>
        /// Pause Write
        /// </summary>
        public int PauseWrite;
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
        public bool LandingLights;
        /// <summary>
        /// Squawk
        /// </summary>
        public short Squawk;
        /// <summary>
        /// SimTime
        /// </summary>
        public DateTime SimTime;
        /// <summary>
        /// SimRate
        /// </summary>
        public int SimRate;
        /// <summary>
        /// Number f Engines
        /// </summary>
        public int EngineCount;
        /// <summary>
        /// QNH
        /// </summary>
        public int QNH;
        /// <summary>
        /// GroundSpeed
        /// </summary>
        public double GroundSpeed;
        /// <summary>
        /// MachSpeed
        /// </summary>
        public long MachSpeed;
        /// <summary>
        /// RadioAltitude
        /// </summary>
        public Double RadioAltitude;
        /// <summary>
        /// GW in pounds
        /// </summary>
        public double GrossWeight;
        /// <summary>
        /// ZFW in pounds
        /// </summary>
        public double ZeroFuelWeight;
        /// <summary>
        /// Flaps level
        /// </summary>
        public short Flaps;
        #endregion Properties

        /// <summary>
        /// Returns true on successfull connection, false otherwise
        /// </summary>
        /// <returns></returns>
        public static bool Connect()
        {
            try
            {
                FSUIPCConnection.Open();
            }
            catch (Exception crap)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a snapshot of the current simulator telemetry, or, if unable returns null
        /// 
        /// </summary>
        /// <returns></returns>
        public static Telemetry GetCurrent()
        {
            Telemetry result = new Telemetry();

            // snapshot data
            try
            {
                FSUIPCConnection.Process();
            }
            catch (Exception crap)
            {
                // failed to connect to the sim?
                return null;
            }

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
            result.ParkingBrakeWrite = FSUIPCOffsets.parkingBrakeWrite.Value;
            result.OnGround = (FSUIPCOffsets.onGround.Value == 0) ? false : true;
            result.VerticalSpeed = (FSUIPCOffsets.verticalSpeed.Value * 3.28084) / -1;
            result.Throttle = FSUIPCOffsets.throttle.Value;
            result.Altitude = (FSUIPCOffsets.altitude.Value * 3.2808399);
            result.Gear = FSUIPCOffsets.GetBool(FSUIPCOffsets.gear);
            result.Slew = FSUIPCOffsets.slew.Value;
            result.Pause = (FSUIPCOffsets.pause.Value == 0) ? false : true;
            result.OverSpeed = (FSUIPCOffsets.overSpeed.Value == 0) ? false : true;
            result.Stall = (FSUIPCOffsets.stall.Value == 0) ? false : true;
            result.Battery = (FSUIPCOffsets.battery.Value == 0) ? false : true;
            result.LandingLights = (FSUIPCOffsets.landingLights.Value == 0) ? false : true;
            result.GrossWeight = FSUIPCOffsets.grossWeight.Value * 0.45359237;
            result.ZeroFuelWeight = (FSUIPCOffsets.zeroFuelWeight.Value / 256) * 0.45359237;
            result.Squawk = FSUIPCOffsets.squawk.Value;
            result.SimTime = (new DateTime(BitConverter.ToInt16(FSUIPCOffsets.simTime.Value, 8), 1, 1, FSUIPCOffsets.simTime.Value[0], FSUIPCOffsets.simTime.Value[1], FSUIPCOffsets.simTime.Value[2])).Add(new TimeSpan(BitConverter.ToInt16(FSUIPCOffsets.simTime.Value, 6) - 1, 0, 0, 0));
            result.SimRate = (FSUIPCOffsets.simRate.Value / 256);
            result.QNH = FSUIPCOffsets.qnh.Value / 16;
            result.EngineCount = FSUIPCOffsets.engineCount.Value;
            result.Compass = FSUIPCOffsets.compass.Value;
            result.Latitude = FSUIPCOffsets.latitude.Value * (90.0 / (10001750.0 * 65536.0 * 65536.0));
            result.Longitude = FSUIPCOffsets.longitude.Value * (360.0 / (65536.0 * 65536.0 * 65536.0 * 65536.0));
            result.Location = new GeoCoordinate(result.Latitude, result.Longitude);
            result.GroundSpeed = (FSUIPCOffsets.groundspeed.Value / 65536)* 1.94384449;
            result.RadioAltitude = FSUIPCOffsets.RadioAltitude.Value / 65536;
            result.MachSpeed = FSUIPCOffsets.machSpeed.Value / 20480;
            result.Flaps = FSUIPCOffsets.flapsControl.Value;

            return result;
        }

        public static void SetValue(Offset<byte> offset, bool value)
        {
            offset.Value = (value) ? (byte)1 : (byte)0;

            FSUIPCConnection.Process();

        }

        public static void SetValue(Offset<byte[]> offset, int value)
        {
            offset.Value = BitConverter.GetBytes(value);

            FSUIPCConnection.Process();

        }

    }
}