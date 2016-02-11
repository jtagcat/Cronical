﻿using System;
using System.IO;
using Cronical.Misc;

namespace Cronical.Configuration
{
  public class ConfigReader
  {
    public class DefinitionArgs : EventArgs
    {
      public string Definition;
      public string Value;
    }

    public class JobArgs : EventArgs
    {
      public bool Reboot;
      public bool Service;
      public string Minute;
      public string Hour;
      public string Day;
      public string Month;
      public string Weekday;
      public string Command;
    }

    public class InvalidConfigArgs : EventArgs
    {
      public string Text;
      public int LineNo;
    }

    public event EventHandler<JobArgs> JobRead;
    public event EventHandler<DefinitionArgs> DefinitionRead;
    public event EventHandler<InvalidConfigArgs> InvalidConfig; 

    public void Read(string filename)
    {
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
      using (var reader = new StreamReader(fs))
      {
        var c = 0;
        while (!reader.EndOfStream)
        {
          var line = reader.ReadLine();
          c++;

          if (ParseComment(line))
            continue;

          if (ParseCommand(line))
            continue;

          if (ParseJob(line))
            continue;
            
          if (InvalidConfig != null)
            InvalidConfig(this, new InvalidConfigArgs { LineNo = c, Text = line });
        }
      }
    }

    private bool ParseComment(string line)
    {
      return string.IsNullOrWhiteSpace(line) || line.Trim()[0] == '#';
    }

    private bool ParseCommand(string line)
    {
      var s = line.Trim();

      var cmd = StringParser.ExtractWord(ref s);
      var equals = StringParser.ExtractWord(ref s);

      if (equals != "=")
        return false;

      if (DefinitionRead != null)
        DefinitionRead(this, new DefinitionArgs { Definition = cmd, Value = s });

      return true;
    }

    private bool ParseJob(string line)
    {
      var job = DoParseJobLine(line);
      if (job != null && JobRead != null)
        JobRead(this, job);

      return job != null;
    }

    public static JobArgs DoParseJobLine(string line)
    {
      var def = (line ?? "").Trim().Replace("\t", " ");
      if (string.IsNullOrWhiteSpace(def) || def[0] == '#')
        return null;

      var result = new JobArgs();

      if (def[0] == '@')
      {
        var spec = StringParser.ExtractWord(ref def).ToLower();

        switch (spec)
        {
          case "@service":
            result.Service = true;
            result.Command = def;
            return result;

          case "@reboot":
            result.Reboot = true;
            result.Command = def;
            return result;

          case "@yearly": def = "0 0 1 1 * " + def; break;
          case "@annually": def = "0 0 1 1 * " + def; break;
          case "@monthly": def = "0 0 1 * * " + def; break;
          case "@weekly": def = "0 0 * * 0 " + def; break;
          case "@daily": def = "0 0 * * * " + def; break;
          case "@hourly": def = "0 * * * * " + def; break;

          default:
            return null;
        }
      }

      try
      {
        result.Minute = StringParser.ExtractWord(ref def);
        result.Hour = StringParser.ExtractWord(ref def);
        result.Day = StringParser.ExtractWord(ref def);
        result.Month = StringParser.ExtractWord(ref def);
        result.Weekday = StringParser.ExtractWord(ref def);

        result.Command = def;

        return result;
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
