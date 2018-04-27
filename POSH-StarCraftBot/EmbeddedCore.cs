using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using POSH.sys;
using POSHStarCraftBot.behaviours;
using POSH.sys.strict;
using SWIG.BWTA;
using POSH.sys.annotations;
using System.Threading;

namespace POSHStarCraftBot
{
    class EmbeddedCore : AStarCraftBehaviour
    {
        protected internal Dictionary<string, BWAPI.IStarcraftBot> clients = null;
        protected string botName;
        public static RealTimeTimer Timer { get; private set; }
        private Thread bwtaThread;

        public EmbeddedCore(AgentBase agent)
            : this(agent, null)
        {

        }

        public EmbeddedCore(AgentBase agent, Dictionary<string, object> attributes)
            : base(agent, new string[] { }, new string[] { })
        {
            // default connection values, use attributes to override
            botName = "POSHbot";
            clients = new Dictionary<string, BWAPI.IStarcraftBot>();
            Timer = new RealTimeTimer(50L);
            // Create the thread object, passing in the method
            // via a ThreadStart delegate. This does not start the thread.
            bwtaThread = new Thread(new ThreadStart(this.RunBWTA));
        }

        //
        // SENSES
        //
        [ExecutableSense("Success")]
        public bool Success()
        {
            return true;
        }

        [ExecutableSense("Fail")]
        public bool Fail()
        {
            return false;
        }

        //
        // INTERNAL
        //

       void loadBot()
        {
            clients["initBot"] = (BWAPI.IStarcraftBot)new BODStarCraftBot(log);
            
            //Timer.Reset();
            System.Console.WriteLine("Bot Loaded: OK");
        }

        void ApplyAttributes()
        {
            if (attributes.Count < 1)
                return;

            try
            {
                if (attributes.ContainsKey("Core.botname"))
                    this.botName = (string)attributes["Core.botname"];
            }
            catch (Exception e)
            {
                if (_debug_)
                {
                    Console.Out.WriteLine("Exception: Could not set init file attibutes!");
                    Console.Out.WriteLine("Trace: " + e);
                }

            }

        }

        void InitBot()
        {
            loadBot(); //preload our bot so that any module load errors come up now instead of at match start.

            //TODO: remove the match speed
            //bwapi.Broodwar.setLocalSpeed(0);
            
            
            System.Console.WriteLine("Starting Match");
            IBWAPI = clients["initBot"];
        }

        /// <summary>
        /// Attempts connecting to StarCraft game using BWAPI.
        /// 
        /// If the bot is currently connected, it disconnects, waits a bit,
        /// and then reconnects.
        /// 
        /// This method should be called at least once!
        /// </summary>
        public override bool Reset()
        {
            try
            {
                Console.WriteLine("Behaviour Oriented Design Bot .50");
                if (IntPtr.Size == 8)
                {
                    System.Console.WriteLine("64bit");
                }
                else
                {
                    System.Console.WriteLine("32bit");
                }
                InitBot();
                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error: {0}", e);
                System.Console.WriteLine(e.StackTrace);
                return false;
            }

        }

        public void RunBWTA() 
        {
            // initializing additional functionality provided by BWTA
            bwta.readMap();
            bwta.analyze();
        }
    }
}
