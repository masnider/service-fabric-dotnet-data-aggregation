using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BladeRuiner.Common.ServiceUtilities
{
    public class ServiceConfigReader
    {
        private ConfigurationPackage ConfigurationPackage;
        private Dictionary<string, Dictionary<string, string>> Sections = new Dictionary<string, Dictionary<string, string>>();

        public ServiceConfigReader(string configPackageName)
        {
            var context = FabricRuntime.GetActivationContext();

            if (context.GetConfigurationPackageNames().Contains(configPackageName))
            {
                this.ConfigurationPackage = context.GetConfigurationPackageObject(configPackageName);

                this.UpdateConfigSettings();

                context.ConfigurationPackageModifiedEvent += ConfigPackageChangedEvent;

            }
            else
            {
                throw new ArgumentException("No Matching Config Package Found");
            }
        }

        private void ConfigPackageChangedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.ConfigurationPackage = e.NewPackage;
            this.UpdateConfigSettings();
        }

        private void UpdateConfigSettings()
        {
            try
            {
                lock (this)
                {
                    foreach (ConfigurationSection section in this.ConfigurationPackage.Settings.Sections)
                    {
                        this.Sections[section.Name] = new Dictionary<string, string>();

                        foreach (ConfigurationProperty property in section.Parameters)
                        {
                            this.Sections[section.Name][property.Name] = property.Value;
                        }

                    }
                }
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        public Dictionary<string, string> this[string index]
        {
            get
            {
                return this.Sections[index];
            }
            private set
            {
                this.Sections[index] = value;
            }
        }
    }
}
