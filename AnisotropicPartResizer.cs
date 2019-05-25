    //   HangarPartResizer.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

// This code is based on Procedural Fairings plug-in by Alexey Volynskov, KzPartResizer class
// And on ideas drawn from the TweakScale plugin

using System.Collections.Generic;
using UnityEngine;

namespace AT_Utils
{
    public class AnisotropicPartResizer : AnisotropicResizableBase
    {
        //GUI
        [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Size", guiFormat="S4")]
        [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.5f, maxValue=10, incrementLarge=1.0f, incrementSmall=0.1f, incrementSlide=0.001f, sigFigs = 4)]
        public float size = 1.0f;
        
        //module config
        [KSPField] public bool sizeOnly;
        [KSPField] public bool aspectOnly;

        [KSPField] public Vector4 specificMass = new Vector4(1.0f, 1.0f, 1.0f, 0f);
        [KSPField] public Vector4 specificCost = new Vector4(1.0f, 1.0f, 1.0f, 0f);

        //state
        [KSPField(isPersistant=true)] public float orig_size = -1;
        [KSPField(isPersistant=true)] public Vector3 orig_local_scale;
        Vector3 old_local_scale;
        float old_size  = -1;

        public Scale scale { get { return new Scale(size, old_size, orig_size, aspect, old_aspect, just_loaded); } }
        
        #region PartUpdaters
        readonly List<PartUpdater> updaters = new List<PartUpdater>();
        
        void create_updaters()
        {
            foreach(var updater_type in PartUpdater.UpdatersTypes) 
            {
                PartUpdater updater = updater_type.Value(part);
                if(updater == null) continue;
                if(updater.Init())
                {
                    updater.SaveDefaults();
                    updaters.Add(updater);
                }
                else part.RemoveModule(updater); 
            }
            updaters.Sort((a, b) => a.priority.CompareTo(b.priority));
        }
        #endregion

        protected override void prepare_model()
        {
            if(prefab_model == null) return;
            orig_local_scale = prefab_model.localScale;
            if(orig_size > 0)
            {
                model.localScale = Scale.ScaleVector(orig_local_scale, size/orig_size, aspect);
//                this.Log("size {}/{}, orig scale: {}, local scale: {}", size, orig_size, orig_local_scale, model.localScale);//debug
                model.hasChanged = true;
                part.transform.hasChanged = true;
            }
        }

        public override void SaveDefaults()
        {
            base.SaveDefaults();
            if(orig_size < 0 || HighLogic.LoadedSceneIsEditor)
            {
                var resizer = base_part.Modules.GetModule<AnisotropicPartResizer>();
                orig_size = resizer != null ? resizer.size : size;
            }
            old_size  = size;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            create_updaters();
            if(state == StartState.Editor) 
            {
                //init global limits
                if(minSize < 0) minSize = ResizerGlobals.Instance.AbsMinSize;
                if(maxSize < 0) maxSize = ResizerGlobals.Instance.AbsMaxSize;
                //get TechTree limits
                var limits = ResizerConfig.GetLimits(TechGroupID);
                if(limits != null)
                {
                    init_limit(limits.minSize, ref minSize, Mathf.Min(size, orig_size));
                    init_limit(limits.maxSize, ref maxSize, Mathf.Max(size, orig_size));
                }
                //setup sliders
                if(sizeOnly && aspectOnly) aspectOnly = false;
                if(aspectOnly || minSize.Equals(maxSize)) Fields["size"].guiActiveEditor=false;
                else setup_field(Fields["size"], minSize, maxSize, sizeStepLarge, sizeStepSmall);
                if(sizeOnly || minAspect.Equals(maxAspect)) Fields["aspect"].guiActiveEditor=false;
                else setup_field(Fields["aspect"], minAspect, maxAspect, aspectStepLarge, aspectStepSmall);
            }
            Rescale();
        }

        public void Update()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                if(old_local_scale != model.localScale)
                    Rescale();
                else if(unequal(old_size, size) || unequal(old_aspect, aspect))
                {
                    Rescale();
                    part.BreakConnectedCompoundParts();
                }
            }
        }

        void Rescale()
        {
            if(model == null) return;
            Scale _scale = scale;
            //change model scale
            model.localScale = _scale.ScaleVector(orig_local_scale);
//            this.Log("size {}/{}, orig scale: {}, local scale: {}", size, orig_size, orig_local_scale, model.localScale);//debug
            model.hasChanged = true;
            part.transform.hasChanged = true;
            //recalculate mass and cost
            mass = ((specificMass.x*_scale + specificMass.y)*_scale + specificMass.z)*_scale * _scale.aspect + specificMass.w;
            cost = ((specificCost.x*_scale + specificCost.y)*_scale + specificCost.z)*_scale * _scale.aspect + specificCost.w;
            //update nodes and modules
            updaters.ForEach(u => u.OnRescale(_scale));
            //save size and aspect
            old_size   = size;
            old_aspect = aspect;
            old_local_scale = model.localScale;
            Utils.UpdateEditorGUI();
            if(HighLogic.LoadedSceneIsFlight)
                StartCoroutine(CallbackUtil.DelayedCallback(1, UpdateDragCube));
            just_loaded = false;
        }
    }
}