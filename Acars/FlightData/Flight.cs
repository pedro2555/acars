﻿using Acars.Events;
using FSUIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;

namespace Acars.FlightData
{
    public class Flight
    {
        public FlightPlan GetFlightPlan()
        {
            LoadedFlightPlan = FlightDatabase.GetFlightPlan();

            return LoadedFlightPlan;
        }


        public Flight(FlightPhases initialPhase = FlightPhases.PREFLIGHT)
        {
            #region Register Events
            activeEvents = new FlightEvent[] {
                new FlightEvent("2A", 5, "Bank Angle Exceeded"                       , 30, 30, (t) => { return (t.Bank > 30); }),
                new FlightEvent("3A", 5, "Landing lights on above 10000 ft"     , 5 , 5 , (t) => { return (t.LandingLights && (t.Altitude > 10500)); }),
                new FlightEvent("3B", 5, "Landing lights off durring approach"  , 5 , 5 , (t) => { return (!t.LandingLights && (t.RadioAltitude < 2750 || t.Altitude < 2500) && phase == FlightPhases.APPROACH); }),
                new FlightEvent("3C", 5, "Speed above 250 IAS bellow 10000 ft"  , 10, 10, (t) => { return ((t.IndicatedAirSpeed > 255) && (t.Altitude < 9500)); }),
                new FlightEvent("3D", 5, "High Speed On Taxi Out"                      , 5 , 5, (t) => { return ((t.GroundSpeed > 30 && t.OnGround) && phase == FlightPhases.TAXIOUT); }),
                new FlightEvent("3E", 5, "High Speed On Taxi In"                      , 5 , 5, (t) => { return ((t.GroundSpeed > 30 && t.OnGround) && phase == FlightPhases.TAXIIN); }),
                new FlightEvent("4A", 5, "Landing light on above 250 IAS"       , 5 , 5 , (t) => { return (t.LandingLights && t.IndicatedAirSpeed > 255); }),
                new FlightEvent("4B", 5, "Landing lights Off On TakeOff"  , 5 , 5 , (t) => { return (!t.LandingLights && phase == FlightPhases.TAKEOFF); }),
                new FlightEvent("4C", 5, "Pitch High Below 1500ft on Departure(Radio)"             , 10, 10, (t) => { return ((t.RadioAltitude < 1500) && t.Pitch > 20); }),
                new FlightEvent("4D", 5, "Gear down above 250 IAS"             , 10, 30, (t) => { return (t.Gear && t.IndicatedAirSpeed > 255); }),
                new FlightEvent("6A", 1, "Maximum TakeOff Weight Excceded"             , 30, 30, (t) => { return (phase == FlightPhases.TAKEOFF && (t.GrossWeight > LoadedFlightPlan.Aircraft.MTW)); }),
                new FlightEvent("6B", 1, "Maximum Landing Weight Excceded"             , 30, 30, (t) => { return (phase == FlightPhases.LANDING && (t.GrossWeight > LoadedFlightPlan.Aircraft.MLW)); }),
                new FlightEvent("6C", 1, "Maximum Service Ceiling Excceded"             , 30, 30, (t) => { return (!t.OnGround && (t.Altitude > LoadedFlightPlan.Aircraft.Celling)); }),
                //new FlightEvent("6D", 1, "Maximum Speed Flap Excceded"             , 30, 30, (t) => {
                //    var flapSetting = LoadedFlightPlan.Aircraft.FlapSettings.OrderBy(x => x.Key).Where(x => x.Key < t.Flaps).FirstOrDefault().Value;
                //    return (!t.OnGround && (t.IndicatedAirSpeed > flapSetting.IASLimit));
                //}),

                new FlightEvent("6D", 1, "Maximum Speed Flap Excceded"                       , 30, 30, (t) => { return LoadedFlightPlan.Aircraft.FlapSettings.ContainsKey(t.Flaps) && (LoadedFlightPlan.Aircraft.FlapSettings[t.Flaps].IASLimit < t.IndicatedAirSpeed); }),
                new FlightEvent("7B", 5, "Pitch too high"                       , 30, 30, (t) => { return (t.Pitch > 30); })
            };
            #endregion Register Events

            phase = initialPhase;
            FlightRunning = false;

            TelemetryLog = new List<Telemetry>();

            ActualArrivalTimeId = -1;
            ActualDepartureTimeId = -1;

            LoadedFlightPlan = null;

            FinalScore = 100;

            Events = new List<EventOccurrence>();
        }

        #region variables
        // instance
        private FlightPhases phase;

        private FlightEvent[] activeEvents;
        private int ActualDepartureTimeId;
        private int ActualArrivalTimeId;
        private int lastUpdateId;

        // statics
        static private Offset<short> engine1 = new Offset<short>(0x0894);
        static private Offset<short> parkingBrake = new Offset<short>(0x0BC8, false);
        static private Offset<int> airspeed = new Offset<int>(0x02BC);
        static private Offset<short> onGround = new Offset<short>(0x0366, false);
        static private Offset<short> verticalSpeed = new Offset<short>(0x0842);
        static private Offset<short> throttle = new Offset<short>(0x088C);
        static private Offset<Double> altitude = new Offset<Double>(0x6020);
        #endregion variables

        #region Properties

        /// <summary>
        /// Returns true if flight is to running on the database
        /// </summary>
        public bool FlightRunning
        { get; private set; }
        /// <summary>
        /// Flight Identifier on the database side
        /// </summary>
        public int FlightID
        {
            get;
            internal set;
        }

        /// <summary>
        /// pipers table ID for this flight
        /// 
        /// Retrieved on FlightStart()
        /// </summary>
        public long PirepID
        { get; private set; }

        public Telemetry ActualDepartureTime
        {
            get { return (ActualDepartureTimeId > -1) ? TelemetryLog[ActualDepartureTimeId] : null; }
        }

        public Telemetry ActualArrivalTime
        {
            get { return (ActualArrivalTimeId > -1) ? TelemetryLog[ActualArrivalTimeId] : null; }
        }

        public TimeSpan ActualTimeEnRoute
        {
            get
            {
                if (ActualDepartureTime == null || ActualArrivalTime == null)
                    return TimeSpan.MinValue;
                return ActualArrivalTime.Timestamp - ActualDepartureTime.Timestamp;
            }
        }

        public Telemetry LastUpdate
        {
            get
            {
                if (lastUpdateId == default(int))
                    return null;
                return TelemetryLog[lastUpdateId];
            }
        }

        public List<Telemetry> TelemetryLog
        {
            get;
            private set;
        }

        public FlightPlan LoadedFlightPlan
        {
            get; private set;
        }

        public List<EventOccurrence> Events
        {
            get;
            private set;
        }

        public int FinalScore
        { get; private set; }

        public int EfficiencyPoints
        { get; private set; }

        public Telemetry LastTelemetry
        {
            get
            {
                if (TelemetryLog.Count == 0)
                    return Telemetry.GetCurrent();
                else
                    return TelemetryLog[TelemetryLog.Count - 1];
            }
        }
        #endregion Properties


        /// <summary>
        /// Saves a snapshot of the current telemetry readings
        /// </summary>
        /// <returns></returns>
        public Telemetry ProcessTelemetry(Telemetry t)
        {
            // collect all telemetry data we need
            TelemetryLog.Add(t);

            return t;
        }

        /// <summary>
        /// Handle flight phases
        /// </summary>
        public Telemetry HandleFlightPhases(Telemetry currentTelemetry = null)
        {
            if(currentTelemetry == null)
                currentTelemetry = Telemetry.GetCurrent();

            // handle switching phase
            switch (phase)
            {
                case FlightPhases.PREFLIGHT:
                    if (currentTelemetry.Engine1 && !currentTelemetry.ParkingBrake)
                        phase = FlightPhases.PUSHBACK;
                    break;
                case FlightPhases.PUSHBACK:
                    if (currentTelemetry.Engine1 && !currentTelemetry.ParkingBrake && currentTelemetry.IndicatedAirSpeed >= 10)
                        phase = FlightPhases.TAXIOUT;
                    break;
                case FlightPhases.TAXIOUT:
                    if (currentTelemetry.Engine1 && currentTelemetry.IndicatedAirSpeed >= 30)
                    {
                        ActualDepartureTimeId = TelemetryLog.Count; // works because we will be inserting the current telemetry data to the TelemetryLog
                        phase = FlightPhases.TAKEOFF;
                    }
                    break;
                case FlightPhases.TAKEOFF:
                    if (!currentTelemetry.OnGround)
                        phase = FlightPhases.CLIMBING;
                    break;
                case FlightPhases.CLIMBING:
                    if (currentTelemetry.VerticalSpeed <= 100 && currentTelemetry.VerticalSpeed >= -100 && !currentTelemetry.OnGround)
                        phase = FlightPhases.CRUISE;
                    else if (currentTelemetry.VerticalSpeed <= -100 && !currentTelemetry.OnGround && LastTelemetry.Location.GetDistanceTo(LoadedFlightPlan.ArrivalAirfield.Position) > 18000)
                        phase = FlightPhases.DESCENDING;
                    break;
                case FlightPhases.CRUISE:
                    if (currentTelemetry.VerticalSpeed <= -100 && !currentTelemetry.OnGround && LastTelemetry.Location.GetDistanceTo(LoadedFlightPlan.ArrivalAirfield.Position) > 18000)
                        phase = FlightPhases.DESCENDING;
                    else if (currentTelemetry.VerticalSpeed >= 100 && !currentTelemetry.OnGround)
                        phase = FlightPhases.CLIMBING;
                    else if (!currentTelemetry.OnGround && currentTelemetry.IndicatedAirSpeed <= 200 && LastTelemetry.Location.GetDistanceTo(LoadedFlightPlan.ArrivalAirfield.Position) < 18000)
                        phase = FlightPhases.APPROACH;
                    break;
                case FlightPhases.DESCENDING:
                    if (!currentTelemetry.OnGround && currentTelemetry.IndicatedAirSpeed <= 200 && LastTelemetry.Location.GetDistanceTo(LoadedFlightPlan.ArrivalAirfield.Position) < 18000)
                        phase = FlightPhases.APPROACH;
                    else if (currentTelemetry.VerticalSpeed >= 100 && !currentTelemetry.OnGround)
                        phase = FlightPhases.CLIMBING;
                    else if (currentTelemetry.VerticalSpeed <= 100 && currentTelemetry.VerticalSpeed >= -100 && !currentTelemetry.OnGround)
                        phase = FlightPhases.CRUISE;
                    break;
                case FlightPhases.APPROACH:
                    if (currentTelemetry.OnGround)
                    {
                        ActualArrivalTimeId = TelemetryLog.Count; // again, works because we will be inserting the current telemetry data to the TelemetryLog
                        phase = FlightPhases.LANDING;
                    }
                    else if (currentTelemetry.VerticalSpeed >= 100)
                        phase = FlightPhases.CLIMBING;
                    break;
                case FlightPhases.LANDING:
                    if (currentTelemetry.IndicatedAirSpeed <= 40 && currentTelemetry.OnGround)
                        phase = FlightPhases.TAXIIN;
                    break;
                case FlightPhases.TAXIIN:
                    if (!currentTelemetry.Engine1 && !currentTelemetry.Engine2 && !currentTelemetry.Engine3 && !currentTelemetry.Engine4 && currentTelemetry.ParkingBrake)
                        phase = FlightPhases.PARKING;
                    break;
            }
            currentTelemetry.FlightPhase = phase;
            return currentTelemetry;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        public bool StartFlight()
        {
            try
            {
                PirepID = FlightDatabase.StartFlight(this);
                Telemetry.SetValue(FSUIPCOffsets.engine1, false);
                Telemetry.SetValue(FSUIPCOffsets.engine2, false);
                Telemetry.SetValue(FSUIPCOffsets.engine3, false);
                Telemetry.SetValue(FSUIPCOffsets.engine4, false);
                Telemetry.SetValue(FSUIPCOffsets.parkingBrake, true);

                Telemetry.SetValue(FSUIPCOffsets.environmentDateTimeHour, DateTime.UtcNow.Hour);
                Telemetry.SetValue(FSUIPCOffsets.environmentDateTimeMinute, DateTime.UtcNow.Minute);                
                Telemetry.SetValue(FSUIPCOffsets.environmentDateTimeDayOfYear , DateTime.UtcNow.DayOfYear);
                Telemetry.SetValue(FSUIPCOffsets.environmentDateTimeYear, DateTime.UtcNow.Year);

                string Message = "Welcome to FlyAtlantic Acars";
                FSUIPCOffsets.messageWrite.Value = Message;
                FSUIPCOffsets.messageDuration.Value = 10;
                FSUIPCConnection.Process();

                phase = FlightPhases.PREFLIGHT;
                FlightRunning = true;
            }
            catch (Exception crap)
            {
                throw new Exception("Failed to start flight.", crap);
            }

            // TODO: do all stuff via telemetry to force desired values


            return FlightRunning;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void AnalyseFlightLog(bool updateScore = false)
        {

            //                new FlightEvent("5C", 1, "Hard Landing"             , 30, 30, (t) => { return (ActualArrivalTime.VerticalSpeed <= -500D); }),
            foreach (FlightEvent e in activeEvents)
            {               
                Events.AddRange(e.GetOccurrences(TelemetryLog.ToArray(), out int discount));

                FinalScore -= discount;
            }

            if(ActualArrivalTime.VerticalSpeed <= -500.0)
            {
                Events.Add(new EventOccurrence(ActualArrivalTimeId, ActualArrivalTimeId, new FlightEvent("5C", 1, "Hard Landing", 30, 30, (t) => { return true; })));

                if (updateScore)
                    FinalScore -= 30;
            }

            if (updateScore)
                UpdateScore();
        }

        private void UpdateScore()
        {
            // calculate EPs
            EfficiencyPoints = (int)Math.Round((ActualTimeEnRoute.TotalMinutes / 10) * (FinalScore * 0.01));
        }


        private bool IsUpdateRequired()
        {
            if (LastUpdate == null)
                return true;

            // check for minimum time requirement for update (15min, 10min for 'safety reasons')
            TimeSpan timeDiff = LastTelemetry.Timestamp - LastUpdate.Timestamp;
            if (timeDiff.TotalMinutes >= 10)
                return true;

            // check flight phase change (TODO: discontinue)
            if (LastTelemetry.FlightPhase != LastUpdate.FlightPhase)
                return true;

            // check if altitude changed more than 50ft
            double altDiff = LastTelemetry.Altitude - LastUpdate.Altitude;
            if (Math.Abs(altDiff) >= 50.0)
                return true;

            // check speed changed more than 5 knots
            int spdDiff = (int)LastTelemetry.GroundSpeed - (int)LastUpdate.GroundSpeed;
            if (Math.Abs(spdDiff) >= 5)
                return true;

            // Heading changed more than 5 degrees (TODO: probably using trigonometry would be a wise idea)
            double hdgDiff = LastTelemetry.Compass - LastUpdate.Compass;
            if (Math.Abs(hdgDiff) >= 5.0)
                return true;

            //
            // TODOs
            //

            // Event triggered

            return false;
        }

        public void UpdateFlight()
        {          
            if (IsUpdateRequired())
            {
                FlightDatabase.UpdateFlight(this);

                lastUpdateId = TelemetryLog.Count - 1;

                bool onVatsim = FlightDatabase.IsPilotOnVatsim(this);
                if (!onVatsim && Events.Count(t => t.Event.Code == "5A") == 0)
                    Events.Add(new EventOccurrence(lastUpdateId, lastUpdateId, new FlightEvent("5A", 900, "Offline From Vatsim", 10, 30, (t) => { return false; })));

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndFlight()
        {
            AnalyseFlightLog(true);

            // calculate flight efficiency
            EfficiencyPoints = Convert.ToInt32(Math.Round((ActualTimeEnRoute.TotalMinutes / 10) * (FinalScore * 0.01)));

            // do database stuff
            FlightDatabase.EndFlight(this);
        }
    }
}
