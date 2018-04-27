using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net.Core;
using System.Threading;
using System.Reflection;
using POSH.sys;
using POSH.sys.exceptions;

#if LOG_ON
using log4net;
using POSHStarCraftBot.behaviours;
using POSHStarCraftBot;
using BWAPI;
#endif

namespace POSHLauncher
{
    class EmbeddedLauncher : IBehaviourConnector
    {
        internal string engineLog;
        internal EmbeddedControl control;
        internal bool loopsRunning = false;
        internal bool verbose;
        internal AgentBase[] agents = null;
        internal Core core;

        protected Dictionary<string, string> plans;
        protected Dictionary<string, string> initFiles;

        protected bool started = false;

        public string [] actionPlans;
        public string usedPOSHConfig;
        public string[] agentConfiguration;
        public bool stopAgents = false;

        public EmbeddedLauncher()
        {
            verbose = false;
            // activate logging. we do this before initialising the world, as it might
            // use this logging facility
#if LOG_ON
            string configFile = String.Format("log4net.xml", Path.DirectorySeparatorChar);

            /**/
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(configFile);
            /**/
            log4net.Config.XmlConfigurator.ConfigureAndWatch(fileInfo);
            /**/
            log4net.LogManager.GetLogger(typeof(LogBase)).InfoFormat("tesat", configFile);
#endif
            InitPOSH();
        }

        private string[] getPlanFiles(string dir)
        {
            string planPath = dir;
            string[] plans = { };
            List<string> result = new List<string>();

            try
            {
                if (File.Exists(planPath))
                    plans = Directory.GetFiles(planPath, "*.lap", SearchOption.TopDirectoryOnly);
                int end;
                foreach (string plan in plans)
                {
                    end = plan.ToLower().Contains(".lap") ? plan.ToLower().LastIndexOf(".lap") : 0;
                    result.Add(plan.Remove(end));
                }
            }
            catch (IOException)
            {
                // TODO: @swen: some clever log or comment here!!!
            }
            return result.ToArray();
        }

        private string[] getPlans(string lib)
        {
            string planPath = control.getRootPath()+Path.DirectorySeparatorChar+lib;
            string[] plans = { };
            List<string> result = new List<string>();

            try
            {
                if (File.Exists(planPath))
                    plans = Directory.GetFiles(planPath, "*.lap", SearchOption.TopDirectoryOnly);
                int end;
                foreach (string plan in plans)
                {
                    end = plan.ToLower().Contains(".lap") ? plan.ToLower().LastIndexOf(".lap") : 0;
                    result.Add(plan.Remove(end));
                }
            }
            catch (IOException)
            {
                // TODO: @swen: some clever log or comment here!!!
            }
            return result.ToArray();
        }

        private string GetPlanFile(string lib, string plan)
        {
            string planPath = control.getRootPath() + Path.DirectorySeparatorChar + lib;
            string[] plans = { };
            string result = "";

            try
            {
                if (Directory.Exists(planPath))
                    plans = Directory.GetFiles(planPath, "*", SearchOption.AllDirectories);

                foreach (string p in plans)
                {
                    if (p.Split(Path.DirectorySeparatorChar)
                            .Last().Contains(plan))
                    {
                        result = p;
                        break;
                    }
                }
            }
            catch (IOException)
            { Console.Error.WriteLine("poshSHARP: could not find plan file at:" + planPath); }

            string planResult = new StreamReader(File.OpenRead(result)).ReadToEnd();

            return planResult;
        }

            protected virtual void InitPOSH()
            {
                //TODO: this needs to be clean up and is only a temp fix

            AssemblyControl.SetForUnityMode();
            control = AssemblyControl.GetControl() as EmbeddedControl;
            control.SetBehaviourConnector(this);




            plans = new Dictionary<string, string>();
            string[] planNames = getPlans("lib"+Path.DirectorySeparatorChar+"plans");
            for (int i = 0; i < planNames.Length; i++)
            {
                string planResult = GetPlanFile("lib" + Path.DirectorySeparatorChar + "plans", planNames[i]);
                if (planResult != null)
                    plans.Add(planNames[i], planResult);
            }
            control.SetActionPlans(plans);

            initFiles = new Dictionary<string,string>();
            string initString = control.GetAgentInitFileString(POSHStarCraftBot.Properties.Settings.Default.initFile);
            if (initString != null)
                initFiles.Add(POSHStarCraftBot.Properties.Settings.Default.initFile.Split('-')[0], initString);
            control.SetInitFiles(initFiles);
          
            engineLog = "init";
            

        }

        protected bool StopPOSH()
        {
            foreach (AgentBase ag in agents)
                ag.StopLoop();
            Console.WriteLine("STOPPING POSH Planner");
            started = false;
            return false;
        }

        protected bool PausePOSH()
        {
            Console.WriteLine("STOPPING POSH Planner");
            
            foreach (AgentBase ag in agents)
                ag.PauseLoop();
            return false;
        }

        /// <summary>
        /// Checks if at least one POSH agent is still running
        /// </summary>
        /// <param name="checkStopped">If true the method checks if the agents are entirely stopped if false it will check if the agents are only paused.</param>
        /// <returns></returns>
        protected virtual bool AgentRunning(bool checkStopped)
        {
            foreach (AgentBase agent in agents)
                if (checkStopped)
                {
                    if (agent.LoopStatus().First)
                        return true;
                }
                else
                {
                    if (agent.LoopStatus().Second)
                        return true;
                }

            return false;
        }

        protected virtual bool RunPOSH()
        {
            
            if (!started)
            {
                Console.WriteLine("init POSH planner");

                List<Tuple<string, object>> agentInit = control.InitAgents(true, "", usedPOSHConfig);
                agents = control.CreateAgents(true, usedPOSHConfig, agentInit, new Tuple<World, bool>(null, false));
                control.StartAgents(true, agents);
                Console.WriteLine("running POSH");

            }
            control.Running(true, agents, false);
            started = true;
            return started;
        }

        

        
        public string GetBehaviourLibrary()
        {
            return usedPOSHConfig;
        }

        public Behaviour[] GetBehaviours(AgentBase agent)
        {
            List<Behaviour> result = new List<Behaviour>();
            IEnumerable<Type> behaviours = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(t => t.GetTypes()) //TODO: take care to update the reference to the namespace below
                       .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AStarCraftBehaviour)) && t.Namespace == "POSHStarCraftBot.behaviours");
           
            foreach (Type behave in behaviours)
            {
                ConstructorInfo poshConstructor = behave.GetConstructor(new Type[]{typeof(AgentBase)});
                AStarCraftBehaviour poshBehaviour = poshConstructor.Invoke(new AgentBase[]{agent}) as AStarCraftBehaviour;
                if (poshBehaviour != null)
                    result.Add(poshBehaviour);
                if (poshBehaviour.GetType() == typeof(Core))
                    core = poshBehaviour as Core;
            }
            return result.ToArray();
        }

        public virtual string GetPlanFileStream(string planName)
        {
            return plans[planName];
        }

        public virtual string GetInitFileStream(string libraryName)
        {
            return initFiles[libraryName];
        }

        public virtual bool Ready()
        {
            if (control != null &&
                actionPlans.Length > 0 && agentConfiguration.Count() > 0 &&
                usedPOSHConfig.Length > 1)
                return true;

            return false;
        }

        /**
         * Interface from BWAPI starts below
         **/

        public void onStart()
        {
            SWIG.BWAPI.bwapi.Broodwar.sendText("Loaded C# Module!");
            loopsRunning = control.Running(verbose, agents, loopsRunning);

            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onStart();
        }

        public void onEnd(bool isWinner)
        {
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onEnd(isWinner);
        }

        public void onFrame()
        {
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onFrame();
        }

        public void onSendText(string text)
        {
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onSendText(text);
        }

        public void onReceiveText(long p_player, string text)
        {
            SWIG.BWAPI.Player player = BWAPI.Helper.NewPlayer(new IntPtr(p_player));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onReceiveText(player, text);
        }

        public void onPlayerLeft(long p_player)
        {
            SWIG.BWAPI.Player player = BWAPI.Helper.NewPlayer(new IntPtr(p_player));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onPlayerLeft(player);
        }

        public void onNukeDetect(long p_pos)
        {
            SWIG.BWAPI.Position pos = BWAPI.Helper.NewPosition(new IntPtr(p_pos));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onNukeDetect(pos);
        }

        public void onUnitDiscover(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitDiscover(unit);
        }

        public void onUnitEvade(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitEvade(unit);
        }

        public void onUnitShow(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitShow(unit);
        }

        public void onUnitHide(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitHide(unit);
        }

        public void onUnitCreate(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitCreate(unit);
        }

        public void onUnitDestroy(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitDestroy(unit);
        }

        public void onUnitMorph(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitMorph(unit);
        }

        public void onUnitRenegade(long p_unit)
        {
            SWIG.BWAPI.Unit unit = BWAPI.Helper.NewUnit(new IntPtr(p_unit));
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onUnitRenegade(unit);
        }

        public void onSaveGame(string gameName)
        {
            if (core == null)
                SWIG.BWAPI.bwapi.Broodwar.sendText("dotNet: " + this.engineLog);
            else
                foreach (IStarcraftBot client in core.clients.Values)
                    client.onSaveGame(gameName);
        }
    }
}
