﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VoiceCraft.Client.Assets.Locals {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("VoiceCraft.Client.Assets.Locals.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string VoiceCraft {
            get {
                return ResourceManager.GetString("VoiceCraft", resourceCulture);
            }
        }
        
        internal static string Settings_General {
            get {
                return ResourceManager.GetString("Settings.General", resourceCulture);
            }
        }
        
        internal static string Settings_General_Language {
            get {
                return ResourceManager.GetString("Settings.General.Language", resourceCulture);
            }
        }
        
        internal static string Home_Servers {
            get {
                return ResourceManager.GetString("Home.Servers", resourceCulture);
            }
        }
        
        internal static string Home_Settings {
            get {
                return ResourceManager.GetString("Home.Settings", resourceCulture);
            }
        }
        
        internal static string Home_Credits {
            get {
                return ResourceManager.GetString("Home.Credits", resourceCulture);
            }
        }
        
        internal static string Home_AddServer {
            get {
                return ResourceManager.GetString("Home.AddServer", resourceCulture);
            }
        }
    }
}
