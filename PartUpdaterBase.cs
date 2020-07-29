//   PartUpdaterBase.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System.Collections.Generic;

namespace AT_Utils
{
    public abstract class PartUpdaterBase : PartModule
    {
        protected Part base_part
        {
            get
            {
                if(part.partInfo != null && part.partInfo.partPrefab != null)
                    return part.partInfo.partPrefab;
                return part;
            }
        }

        protected bool initialized { get; private set; }

        public virtual bool Init() => true;

        public abstract void SaveDefaults();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if(initialized)
                return;
            if(Init())
            {
                SaveDefaults();
                initialized = true;
            }
            else
                this.EnableModule(false);
        }
    }

    public abstract class PartUpdater : PartUpdaterBase
    {
        public uint priority = 0; // 0 is highest

        public override void SaveDefaults() { }

        public abstract void OnRescale(Scale scale);

        #region ModuleUpdaters
        public delegate PartUpdater Constructor(Part part);

        public readonly static Dictionary<string, Constructor> UpdatersTypes = new Dictionary<string, Constructor>();

        static Constructor create_constructor<UpdaterType>() where UpdaterType : PartUpdater
        { return part => part.Modules.GetModule<UpdaterType>() ?? part.AddModule(typeof(UpdaterType).Name) as PartUpdater; }

        public static void RegisterUpdater<UpdaterType>()
            where UpdaterType : PartUpdater
        {
            string updater_name = typeof(UpdaterType).FullName;
            if(UpdatersTypes.ContainsKey(updater_name)) return;
            UpdatersTypes[updater_name] = create_constructor<UpdaterType>();
            Utils.Log("PartUpdater registered: {}", updater_name);
        }
        #endregion
    }

    public abstract class ModuleUpdater<T> : PartUpdater where T : PartModule
    {
        protected struct ModulePair<M>
        {
            public M base_module;
            public M module;
            public Dictionary<string, object> orig_data;

            public ModulePair(M base_module, M module)
            {
                this.module = module;
                this.base_module = base_module;
                orig_data = new Dictionary<string, object>();
            }
        }

        protected readonly List<ModulePair<T>> modules = new List<ModulePair<T>>();

        public override bool Init()
        {
            if(!base.Init())
                return false;
            priority = 100;
            modules.Clear();
            var m = part.Modules.GetEnumerator();
            var b = base_part.Modules.GetEnumerator();
            while(b.MoveNext() && m.MoveNext())
            {
                if(b.Current is T bT && m.Current is T mT)
                    modules.Add(new ModulePair<T>(bT, mT));
            }
            m.Dispose();
            b.Dispose();
            return modules.Count > 0;
        }

        protected abstract void on_rescale(ModulePair<T> mp, Scale scale);

        public override void OnRescale(Scale scale)
        {
            if(enabled)
                modules.ForEach(mp => on_rescale(mp, scale));
        }
    }
}

