﻿using System;
using System.Collections.Generic;
using NDepend.Path;
using Newtonsoft.Json;

namespace Zsync_the_things
{
    public class MainControlFile
    {

        public MainControlFile()
        {
            Content = new List<ControlFileItem>();
            Version = new Version(0, 2);
        }

        public Version Version { get; set; }
        public List<ControlFileItem> Content { get; set; }
        public Uri RootUri { get; set; }
    }

    public class ControlFileItem
    {
        public Uri RelativeContentUrl { get; set; }
        [JsonIgnore]
        public IRelativeFilePath InstallPath { get; set; }
        public string FileHash { get; set; }


        public string _installPath {
            get {
                if (InstallPath == null)
                    return null;
                return InstallPath.ToString();
            }
            set {
                if (value == null)
                    InstallPath = null;
                InstallPath = value.ToRelativeFilePath();
            }
        }
    }
}