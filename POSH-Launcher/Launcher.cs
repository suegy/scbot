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
#endif

namespace POSH_Launcher
{
    /// <summary>
    /// Launches a POSH agent or a set of agents.
    /// 
    /// 
        /// Synopsis:
    ///     launch.py [OPTIONS] library

    /// Description:
    ///     Launches a POSH agent by fist initialising the world and then the
    ///     agents. The specified library is the behaviour library that will be used.
    /// 
    ///     -v, --verbose
    ///         writes more initialisation information to the standard output.
    /// 
     ///    -h, --help
    ///         print this help message.
    /// 
    ///     World initialisation:
    /// 
    ///     -w, --init-world-file=INITSCRIPT
    ///         the python script that initialises the world. To communicate with
    ///         launch.py, an instance of class World called 'world' is passed to the
    ///         world initialisation script. Its most important methods:
    ///             world.args() : Returns the arguments given by the -a options.
    ///                 If -a is not given, None is returned.
    ///             world.set(x) : Passes x as the world object to the agents upon
    ///                 initialising them.
    ///             world.createsAgents() : Needs to be called if the
    ///                 world initialisation script rather than launch.py creates
    ///                 and runs the agents.
    ///         More information on the World class can be found in the API
    ///         documenatation of the POSH.utils.World class.
    ///         If no world initialisation script is specified, then the default world
    ///         initialisation function of the library is called.
    /// 
    ///     -a, --init-world-args=ARGS
    ///         the argument string given to the function init_world(args) in the
    ///         script specified by -w. If no such script is given, the the arguments
    ///         are given to the default world initialisation function of the library.
    /// 
    ///     Agent initialisation:
    ///         If none of the below options are given, then the default library
    ///         initialisation file is used to initialise the agent(s).
    /// 
    ///     -i, --init-agent-file=INITFILE
    ///         initialises the agent(s) according to the given file. The file format
    ///         is described below.
    /// 
    ///     -p, --plan-file=PLANFILE
    ///         initialises a single agent to use the given plan. Only the name of
    ///         the plan without the path needs to be given, as it is assumed to have
    ///         the ending '.lap' and reside in the default location in the
    ///         corresponding behaviour library. This option is only valid if -i 
    ///         is not given.

    /// Agent initialisation file format:
    ///     The agent initialisation file allows the initialisation of one or several
    ///     agents at once. The file is a simple text file that is read line by line.
    ///     Each new agents starts with a '[plan]' line that specifyies the plan that
    ///     the agent uses. This is followed by a list of attributes and values to
    ///     initialise the behaviours of the agent. Empty lines, and lines starting
    ///     with '#' are ignored.
    /// 
    ///     An example file would be:
    ///     
    ///         [plan1]
    ///         beh1.x = 10
    ///         beh2.y = 20
    ///     
    ///         [plan2]
    ///         beh1.x = 20
    /// 
    ///     This file initialises two agents, one with plan1 and the other with plan2.
    ///     Additionally, the attribute 'x' of behaviour 'beh1' of the first agent is
    ///     set to 10, and attribute 'y' of behaviour 'beh2' to 20. For the second
    ///     agent, the attribute 'x' of behaviour 'beh1' is set to 20.
    /// </summary>
    class Launcher
    {
        const string helpText = @"
          Launches a POSH agent or a set of agents.
     
     
             Synopsis:
             launcher [OPTIONS] -a=<assembly> library

         Description:
             Launches a POSH agent by fist initialising the world and then the
             agents. The specified library is the behaviour library that will be used.

             -h, --help
                 print this help message.
             
             -v, --verbose
                 writes more initialisation information to the standard output.
     
         Environment:
             ALL used paths are currently relative to the execution assembly directory. 
             Ideally all POSH related elements should be in the same folder anyway. 

             -a --assembly
                 the assembly which contains the libraries to load

             -i, --init-dir=INITDIR
                 folder which contains the init files for the agents and worlds

             -p, --plan-dir=PLANDIR
                 folder which contains the plan files for the agents

             -w, --init-world-args=ARGS
                the argument string given to the constructor World(args). 
                This setting overrides the values specified in init file for the library.

             Agent initialisation:
                 If none of the below options are given, then the default library
                 initialisation file is used to initialise the agent(s).
     
             -s, --suffix=AGENT
                 a suffix added to the agent init file to load a different set instead of the standard one.
                 Example: <agentLibrary>_init_<suffix>.txt             

             

         Agent initialisation file format:
             The agent initialisation file allows the initialisation of one or several
             agents at once. The file is a simple text file that is read line by line.
             Each new agents starts with a '[plan]' line that specifyies the plan that
             the agent uses. This is followed by a list of attributes and values to
             initialise the behaviours of the agent. Empty lines, and lines starting
             with '#' are ignored.
     
             An example file would be:
         
                 [plan1]
                 beh1.x = 10
                 beh2.y = 20
         
                 [plan2]
                 beh1.x = 20

                 [world]
                 gravitation=7.1
     
             This file initialises two agents, one with plan1 and the other with plan2.
             Additionally, the attribute 'x' of behaviour 'beh1' of the first agent is
             set to 10, and attribute 'y' of behaviour 'beh2' to 20. For the second
             agent, the attribute 'x' of behaviour 'beh1' is set to 20.";

        internal AssemblyControl control;

        public Launcher()
        {
            control = AssemblyControl.GetControl();
        }

        /// <summary>
        /// Parses the command line options and returns them.
        /// 
        /// The are returned in the order help, verbose, world_file, world_args,
        /// agent_file, plan_file. help and verbose are boolean variables. All the
        /// other variables are strings. If they are not given, then an empty string
        /// is returned.
        /// </summary>
        /// <param name="argv"></param>
        /// <returns></returns>
        /// <exception cref="UsageException"> whenever something goes wrong with the input string</exception>
        protected Tuple<bool,bool,string,string,string,string>  ProcessOptions(string [] args)
        {
            // default values
            bool help = false, verbose = false;
            string worldArgs = "", agentSuffix = "", agentLibrary = "";
            string assembly = "";
            // parse options

            foreach(string arg in args)
            {   
                string [] tuple = arg.Split(new string [] {"="},2,StringSplitOptions.None);
                switch (tuple[0])
                {
                    case "-h":
                    case "--help":
                        help = true;
                        break;
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    case "-i":
                    case "--init-dir":
                        // TODO: currenly disabled as it is not working properly
                        Console.WriteLine("Initialisation by WorldFile is currenly disabled");
                        control.config["InitPath"] = tuple[1];
                        break;
                    case "-w":
                    case "--init-world-args":
                        worldArgs = tuple[1];
                        break;
                    case "-s":
                    case "--suffix":
                        agentSuffix = tuple[1];
                        break;
                    case "-p":
                    case "--plan-dir":
                        control.config["PlanPath"] = tuple[1];
                        break;
                    case "-a": 
                    case "--assembly":
                        if (!control.IsAssembly(tuple[1]))
                            throw new UsageException(string.Format("cannot find specified assembly '{0}' containing the agent libraries", tuple[1]));
                        assembly = tuple[1];
                        break;
                    default:
                        if (tuple[0].StartsWith("-") || tuple.Length > 1)
                            throw new UsageException("unrecognised option: " + tuple[0]);
                        break;
                }
            }
            if (help)
                return new Tuple<bool,bool,string,string,string,string,string>(help,false,"","","","","");

            // get agentLibrary from last element arguments
            if (args[args.Length-1].StartsWith("-"))
                throw new UsageException("requires as last argument (the library); plus optional options");
            agentLibrary = args[args.Length-1];
            if (!control.IsLibraryInAssembly(assembly,agentLibrary))
                throw new UsageException(string.Format("cannot find specified library '{0}'",agentLibrary));
                
            // check for option consistency
            if (!control.checkDirectory(control.config["PlanPath"]))
                throw new UsageException(string.Format("cannot find specified plan directory '{0}' which should contain the '{1}'plan files", control.config["PlanPath"], control.config["PlanEnding"]));

            if (!control.checkDirectory(control.config["InitPath"]))
                throw new UsageException(string.Format("cannot find specified directory '{0}' containing the agent parameter files", control.config["InitPath"]));
                        
            // all fine
            return new Tuple<bool,bool,string,string,string,string>(help,verbose,assembly,worldArgs,agentSuffix,agentLibrary);
        }

        /// <summary>
        /// Calls WorldControl.run_world_script() to initialise the world and returns the
        /// worldl object.
        /// </summary>
        /// <param name="worldFile"></param>
        /// <param name="library"></param>
        /// <param name="worldArgs"></param>
        /// <param name="agentsInit"></param>
        /// <param name="?"></param>
        protected Tuple<World, bool> InitWorld(string worldArgs, string assembly, List<Tuple<string, object>> agentsInit, bool verbose, Type world = null)
        {
            if (verbose)
                Console.Out.WriteLine("- initialising world");
            if (worldArgs.Trim() == string.Empty)
                worldArgs = null;
            
            // find which world script to run
            if (world != null && world.IsSubclassOf(typeof(World)))
            {
                
                if (verbose)
                    Console.Out.WriteLine(string.Format("running '{0}'", world));
                return control.runWorldScript(world,assembly,worldArgs,agentsInit);
            }
            if (verbose)
                Console.Out.WriteLine("no default world initialisation script");

            return new Tuple<World,bool>(null,false);
        }

        public static void Main(string [] args)
        {
            // OLDCOMMENT: There must be a beter way to do this... see jyposh.py and utils compile_mason_java() 

            bool help = false, verbose = false;
            string assembly = "", worldArgs = "", agentSuffix = "", agentLibrary = "";

            List<Tuple<string, object>> agentsInit = null;
            Tuple<World, bool> setting = null;
            AgentBase[] agents = null;

            // process command line arguments
            Launcher application = new Launcher();
            
            Tuple<bool,bool,string,string,string,string> arguments = null;
            if (args is string[] && args.Length > 0)
                arguments = application.ProcessOptions(args);
            else
            {
                Console.Out.WriteLine("for help use --help");
                return;
            }
            if (arguments != null && arguments.First)
            {
                Console.Out.WriteLine(helpText);
                return;
            }

            help = arguments.First;
            verbose = arguments.Second;
            assembly = arguments.Third;
            worldArgs = arguments.Forth;
            agentSuffix = arguments.Fifth;
            agentLibrary = arguments.Sixth;

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
            // read agent initialisation. this needs to be done before initialising
            // the world, as the agent initialisation needs to be give to the world
            // initialisation script

            if (verbose)
                Console.Out.WriteLine("- collect agent initialisation options");
            agentsInit = application.control.InitAgents(verbose, assembly, agentLibrary);

            if (verbose)
                Console.Out.WriteLine(string.Format("will create {0} agent(s)", agentsInit.Count));

            // init the world
            //if (worldFile == string.Empty)
            //    worldFile = application.control.defaultWorldScript(library);
            try
            {
                setting = application.InitWorld(worldArgs, assembly,agentsInit, verbose);
            }
            catch (Exception e)
            {

                    Console.Out.WriteLine("world initialisation Failed");
                    Console.Out.WriteLine("-------");
                    if (verbose)
                        Console.Out.WriteLine(e);
            }

            if (setting != null && setting.Second)
            {
                if (verbose)
                    Console.Out.WriteLine("- world initialisation script indicated that it created " +
                        "agents. nothing more to do.");
                return;
            }

            agents = application.control.CreateAgents(verbose, assembly, agentsInit, setting);
            if (agents == null)
                return;
            // start the agents
            bool loopsRunning = application.control.StartAgents(verbose, agents);

            loopsRunning = application.control.Running(verbose, agents, loopsRunning);

        }
    }
}
