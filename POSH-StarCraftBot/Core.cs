﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPIC;
using SWIG.BWAPI;
using POSH_sharp.sys;
using POSH_StarCraftBot.behaviours;
using POSH_sharp.sys.strict;
using SWIG.BWTA;
using POSH_sharp.sys.annotations;

namespace POSH_StarCraftBot
{
    class Core : AStarCraftBehaviour
    {
        protected internal Dictionary<string,BWAPI.IStarcraftBot> clients = null;
        protected string botName;
        public static RealTimeTimer Timer { get; private set; }
        
        
        public Core(AgentBase agent) : this(agent,null)
        {

        }

        public Core(AgentBase agent, Dictionary<string, object> attributes)
            : base(agent, new string[]{}, new string[]{"Fail", "Success"})
        {
            // default connection values, use attributes to override
            botName = "POSHbot";
            clients = new Dictionary<string, BWAPI.IStarcraftBot>();
            Timer = new RealTimeTimer(50L);
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
        
        void reconnect()
        {

            while (!bwapiclient.BWAPIClient.connect())
            {
                System.Threading.Thread.Sleep(1000);
            }
        
        }

        void loadBot()
        {
            clients.Add("initBot",(BWAPI.IStarcraftBot)new BODStarCraftBot());
            Timer.Reset();
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

        void LoadandRunBot()
        {

            loadBot(); //preload our bot so that any module load errors come up now instead of at match start.

            bwapi.BWAPI_init();
            System.Console.WriteLine("Connecting...");
            reconnect();
            while (true)
            {
                //wait for game to start
                System.Console.WriteLine("waiting to enter match\n");
                while (!bwapi.Broodwar.isInGame())
                {
                    bwapiclient.BWAPIClient.update();
                    if (!bwapiclient.BWAPIClient.isConnected())
                    {
                        System.Console.WriteLine("Reconnecting...\n");
                        reconnect();
                    }
                } //wait for game
                loadBot(); //reload the bot at the start of each match (for easy drop in dll replacement)
                System.Console.WriteLine("Starting Match");
                IBWAPI = clients["initBot"];
                while (bwapi.Broodwar.isInGame())
                {
                    //for(std::list<Event>::iterator e=Broodwar->getEvents().begin();e!=Broodwar->getEvents().end();e++)
                    foreach (Event e in bwapi.Broodwar.getEvents())
                    {
                        EventType_Enum et = e.getType();
                        switch (et)
                        {
                            case EventType_Enum.MatchStart:
                                // initializing additional functionality provided by BWTA
                                bwta.readMap();
                                // takes a long time to run
                                bwta.analyze();
                                
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onStart();
                                break;
                            case EventType_Enum.MatchEnd:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onEnd(e.isWinner());
                                break;
                            case EventType_Enum.MatchFrame:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onFrame();
                                break;
                            case EventType_Enum.MenuFrame:
                                break;
                            case EventType_Enum.SendText:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onSendText(e.getText());
                                break;
                            case EventType_Enum.ReceiveText:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onReceiveText(e.getPlayer(), e.getText());
                                break;
                            case EventType_Enum.PlayerLeft:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onPlayerLeft(e.getPlayer());
                                break;
                            case EventType_Enum.NukeDetect:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onNukeDetect(e.getPosition());
                                break;
                            case EventType_Enum.UnitDiscover:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitDiscover(e.getUnit());
                                break;
                            case EventType_Enum.UnitEvade:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitEvade(e.getUnit());
                                break;
                            case EventType_Enum.UnitShow:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitShow(e.getUnit());
                                break;
                            case EventType_Enum.UnitHide:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitHide(e.getUnit());
                                break;
                            case EventType_Enum.UnitCreate:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitCreate(e.getUnit());
                                break;
                            case EventType_Enum.UnitDestroy:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                client.onUnitDestroy(e.getUnit());
                                break;
                            case EventType_Enum.UnitMorph:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitMorph(e.getUnit());
                                break;
                            case EventType_Enum.UnitRenegade:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onUnitRenegade(e.getUnit());
                                break;
                            case EventType_Enum.SaveGame:
                                foreach (BWAPI.IStarcraftBot client in clients.Values)
                                    client.onSaveGame(e.getText());
                                break;
                            default:
                                break;
                        }
                    }
                    bwapiclient.BWAPIClient.update();
                    if (!bwapiclient.BWAPIClient.isConnected())
                    {
                        System.Console.WriteLine("Reconnecting...\n");
                        reconnect();
                    }
                }
                System.Console.WriteLine("Game ended\n");


            } //main while loop
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
            try {
                System.Console.WriteLine("Behaviour Oriented Design Bot .50");
                if (IntPtr.Size == 8)
                {
                    System.Console.WriteLine("64bit");
                } else {
                    System.Console.WriteLine("32bit");
                }
                
                bwapi.BWAPI_init();
                
                System.Console.WriteLine("Connecting...");
                LoadandRunBot();
                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error: {0}",e);
                System.Console.WriteLine(e.StackTrace);
                return false;
            }

        }

        public bool RegisterListener(BWAPI.IStarcraftBot client,string name)
        {
            if (name.Length < 1 || client == null || clients.ContainsKey(name))
                return false;

            clients.Add(name, client);
            return true;
        }
    }
}
