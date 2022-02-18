//   AnisotropicResizable.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using JetBrains.Annotations;
using UnityEngine;

namespace AT_Utils
{
    public abstract class AnisotropicResizableBase : PartUpdaterBase, IPartCostModifier, IPartMassModifier
    {
        [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Aspect", guiFormat="S4")]
        [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.5f, maxValue=10, incrementLarge=1.0f, incrementSmall=0.1f, incrementSlide=0.001f, sigFigs = 4)]
        public float aspect = 1.0f;
        [UsedImplicitly] private FloatFieldWatcher aspectWatcher;
        protected abstract void on_aspect_changed();

        [UsedImplicitly]
        [KSPField(isPersistant=false, guiActiveEditor=true, guiName="Mass")]
        public string MassDisplay;

        //module config
        [KSPField] public string TechGroupID = "";

        [KSPField] public float minSize = -1;
        [KSPField] public float maxSize = -1;
        [KSPField] public float sizeStepLarge = 1.0f;
        [KSPField] public float sizeStepSmall = 0.1f;

        [KSPField] public float minAspect = -1;
        [KSPField] public float maxAspect = -1;
        [KSPField] public float aspectStepLarge = 0.5f;
        [KSPField] public float aspectStepSmall = 0.1f;

        protected float old_aspect  = -1;
        [KSPField(isPersistant=true)] public float orig_aspect = -1;

        Transform _model;
        protected Transform model 
        { 
            get 
            { 
                if(_model == null)
                    _model = part.transform.Find("model"); 
                return _model;
            } 
        }

        Transform _prefab_model;
        protected Transform prefab_model 
        { 
            get 
            { 
                if(_prefab_model == null && 
                   part.partInfo != null && 
                   part.partInfo.partPrefab != null)
                    _prefab_model = part.partInfo.partPrefab.transform.Find("model"); 
                return _prefab_model;
            } 
        }

        public float cost, orig_cost;
        public float mass, orig_mass;
        protected bool just_loaded = true;

        protected abstract void prepare_model();

        public void UpdateDragCube()
        {
            part.DragCubes.Procedural = true;
            part.DragCubes.ForceUpdate(true, true, true);
            part.DragCubes.SetDragWeights();
        }

        #region TechTree
        protected void init_limit(ResizerLimits.UpdatableFloat tech_limit, ref float limit, float current_value)
        {
            float val = tech_limit.Value;
//            this.Log("initializeing {}: current_value {}, tech_limit {}, val {}, current is better than val: {}", 
//                     tech_limit.GetType().Name, current_value, tech_limit.Value, val, tech_limit.Compare(current_value, val));//debug
            if(tech_limit.Compare(current_value, val)) val = current_value;
//            this.Log("initializeing: limit {}, val {}, limit is better than val: {}", 
//                     limit, val, tech_limit.Compare(limit, val));//debug
            if(limit < 0 || tech_limit.Compare(limit, val)) limit = val;
//            this.Log("initialized: limit {}", limit);//debug
        }

        protected static void setup_field(BaseField field, float minval, float maxval, float l_increment, float s_increment)
        {
            if(field.uiControlEditor is UI_FloatEdit fe)
            {
                fe.minValue = minval;
                fe.maxValue = maxval;
                fe.incrementLarge = l_increment;
                fe.incrementSmall = s_increment;
            }
        }
        #endregion

        public void UpdateGUI(ShipConstruct ship)
        { 
            if(isActiveAndEnabled)
                StartCoroutine(CallbackUtil.DelayedCallback(1, () => MassDisplay = Utils.formatMass(part.TotalMass())));
        }

        public override void OnAwake()
        {
            base.OnAwake();
            GameEvents.onEditorShipModified.Add(UpdateGUI);
        }

        protected virtual void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(UpdateGUI);
        }

        protected abstract void update_orig_mass_and_cost();
        protected virtual void update_orig_attrs()
        {
            if(orig_aspect < 0 || HighLogic.LoadedSceneIsEditor)
            {
                var resizer = base_part.Modules.GetModule<AnisotropicResizableBase>();
                orig_aspect = resizer != null ? resizer.aspect : aspect;
            }
        }

        protected override void SaveDefaults()
        {
            old_aspect = aspect;
            update_orig_attrs();
            update_orig_mass_and_cost();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            prepare_model();
            if(state == StartState.Editor) 
            {
                //init global limits
                if(minAspect < 0) minAspect = ResizerGlobals.Instance.AbsMinAspect;
                if(maxAspect < 0) maxAspect = ResizerGlobals.Instance.AbsMaxAspect;
                //get TechTree limits
                var limits = ResizerConfig.GetLimits(TechGroupID);
                if(limits != null)
                {
                    init_limit(limits.minAspect, ref minAspect, Mathf.Min(aspect, orig_aspect));
                    init_limit(limits.maxAspect, ref maxAspect, Mathf.Max(aspect, orig_aspect));
                }
                aspectWatcher = new FloatFieldWatcher(Fields[nameof(aspect)])
                {
                    epsilon = 1e-4f, onValueChanged = on_aspect_changed
                };
            }
            else 
                UpdateDragCube();
            just_loaded = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            prepare_model();
        }

        #region IPart*Modifiers
        public virtual float GetModuleCost(float defaultCost, ModifierStagingSituation sit) =>
            cost - orig_cost;

        public virtual ModifierChangeWhen GetModuleCostChangeWhen() =>
            ModifierChangeWhen.CONSTANTLY;

        public virtual float GetModuleMass(float defaultMass, ModifierStagingSituation sit) =>
            mass - orig_mass;

        public virtual ModifierChangeWhen GetModuleMassChangeWhen() =>
            ModifierChangeWhen.CONSTANTLY;
        #endregion
    }
}

