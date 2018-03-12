﻿using FSUIPC;
using NLog;
using System;
using System.Linq;
using System.Threading;

namespace FlightMonitorApi
{
    public partial class FSUIPCSnapshot
    {
        /// <summary>
        /// Set when connected to FSUIPC
        /// Gets unset when we can confirm connection was lost
        /// </summary>
        private static bool connected;

        /// <summary>
        /// UTC timestamp of the snapshot, has nothing to do with the actual data
        /// returned, is just a global timestamp for when the data was received
        /// from the underlying Sim connection
        /// </summary>
        public DateTime TimeStamp
        { get; set; }

        public FSUIPCSnapshot()
            : this(false) { }

        public FSUIPCSnapshot(bool InitializeComponent = true)
        {
            TimeStamp = DateTime.UtcNow;

            if (InitializeComponent)
                this.InitializeComponent();
        }

        public void InitializeComponent()
        {
            Position = position;
            Compass = compass;
            Altitude = altitude;
            GroundSpeed = groundspeed;
            IndicatedAirspeed = indicatedairspeed;
            TrueAirpeed = trueairspeed;
            OnGround = onground;
            QNH = qnh;
        }

        /// <summary>
        /// Returns an FSUIPCSnapshot instance with the current state of the sim
        /// </summary>
        /// <returns></returns>
        public static FSUIPCSnapshot Pool()
        {
            // TODO: do not block the current thread on this
            //       application may receive a OS taskkill command.
            //       Maybe expose connected = true?
            while (!connected)
            {
                try
                {
                    FSUIPCConnection.Open();
                    connected = true;

                    FSUIPCConnection.Process();
                    FSUIPCSnapshot data = new FSUIPCSnapshot(true);

                    // FSUIPC will return data even if the user is in scenario
                    // screen or any other screen really, this just compares the
                    // returned location with a location of [0, 0]
                    // TODO: actually filter invalid locations
                    return data.Position.SequenceEqual(
                        new double[] { 0, 0 }) ? null : data;
                }
                catch (FSUIPCException crap)
                {
                    switch (crap.FSUIPCErrorCode)
                    {
                        case FSUIPCError.FSUIPC_ERR_OPEN:
                            connected = true;
                            break;
                        case FSUIPCError.FSUIPC_ERR_NOFS:
                        case FSUIPCError.FSUIPC_ERR_SENDMSG:
                            return null;
                        default:
                            LogManager.GetCurrentClassLogger()
                                .Error(crap, "Pooling data from FSUIPC");
                            throw crap;
                    }
                }
            }

            return null;
        }
    }
}
