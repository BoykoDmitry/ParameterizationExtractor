﻿using Quipu.ParameterizationExtractor.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Quipu.ParameterizationExtractor.Logic.Configs
{
    public class Package : IPackage
    {
        public Package()
        {
            Scripts = new List<SourceForScript>();
        }
        public List<SourceForScript> Scripts { get; set; }
        [XmlIgnore]
        IList<ISourceForScript> IPackage.Scripts
        {
            get
            {
                return Scripts.Select(_ => _ as ISourceForScript).ToList();
            }
        }
    }
}
