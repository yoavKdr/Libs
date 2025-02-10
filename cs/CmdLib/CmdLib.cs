using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace CmdLib
{
    public static class cmd
    {
        public static string Run(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
    public class Commands
    {
        private List<CustomCommand> customCommands;
        private string lineEnd;

        // builder
        public Commands(string lineEnd)
        {
            this.lineEnd = lineEnd;
            customCommands = new List<CustomCommand>();
        }

        // getters
        public List<CustomCommand> CustomCommands
        {
            get
            {
                return customCommands;
            }
        }
        public string LineEnd
        {
            get
            {
                return lineEnd;
            }
        }

        // command management
        public void CreatCommand(string name, string command, string description, int level)
        {
            AddCommand(new CustomCommand(name, command, description, level));
        }
        public void CreatCommand(string name, string command, string description)
        {
            AddCommand(new CustomCommand(name, command, description));
        }
        public void CreatCommand(string name, string command, int level)
        {
            AddCommand(new CustomCommand(name, command, level));
        }
        public void CreatCommand(string name, string command)
        {
            AddCommand(new CustomCommand(name, command));
        }

        public void AddCommand(CustomCommand customCommand)
        {
            customCommands.Add(customCommand);
        }
        public void RemoveCommand(CustomCommand customCommand)
        {
            customCommands.Remove(customCommand);
        }

        // chacks
        public bool ExistsCommand(string command)
        {
            foreach (CustomCommand customCommand in customCommands)
            {
                if (customCommand.Command == command)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public struct CustomCommand
    {
        private string name;
        private string command;
        private string description;
        private int level;

        public CustomCommand(string name, string command, string description, int level)
        {
            this.name = name;
            this.command = command;
            this.description = description;
            this.level = level;
        }
        public CustomCommand(string name, string command, string description)
        {
            this.name = name;
            this.command = command;
            this.description = description;
            level = 0;
        }
        public CustomCommand(string name, string command, int level)
        {
            this.name = name;
            this.command = command;
            description = "null";
            this.level = level;
        }
        public CustomCommand(string name, string command)
        {
            this.name = name;
            this.command = command;
            description = "null";
            level = 0;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }
        public string Command
        {
            get
            {
                return command;
            }
        }
        public string Description
        {
            get
            {
                return description;
            }
        }
        public int Level
        {
            get
            {
                return level;
            }
        }


    }
    public enum Mode
    {
        Normal = 0,
        Get = 1
    }
}

