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
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Size", guiFormat = "S4")]
        [UI_FloatEdit(scene = UI_Scene.Editor, minValue = 0.5f, maxValue = 10, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f, sigFigs = 4)]
        public float size = 1.0f;

        void rescale_and_brake_struts()
        {
            Rescale();
            part.BreakConnectedCompoundParts();
        }
        protected virtual void on_size_changed(object value) => rescale_and_brake_struts();
        protected override void on_aspect_changed(object value) => rescale_and_brake_struts();

        //module config
        [KSPField] public bool sizeOnly;
        [KSPField] public bool aspectOnly;

        [KSPField] public Vector4 specificMass = new Vector4(1.0f, 1.0f, 1.0f, 0f);
        [KSPField] public Vector4 specificCost = new Vector4(1.0f, 1.0f, 1.0f, 0f);

        //state
        [KSPField(isPersistant = true)] public float orig_size = -1;
        [KSPField(isPersistant = true)] public Vector3 orig_local_scale;
        Vector3 old_local_scale;
        float old_size = -1;

        public Scale GetScale() => new Scale(size, old_size, orig_size, aspect, old_aspect, orig_aspect, just_loaded);

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

        readonly Dictionary<string, AttachNode> orig_nodes = new Dictionary<string, AttachNode>();

        private float get_scaled_attr(Vector4 specificAttr, float scale, float rel_aspect) =>
            ((specificAttr.x * scale + specificAttr.y) * scale + specificAttr.z) * scale * rel_aspect + specificAttr.w;

        protected override void update_orig_mass_and_cost()
        {
            orig_mass = get_scaled_attr(specificMass, 1, 1);
            orig_cost = get_scaled_attr(specificCost, 1, 1);
        }

        protected override void update_orig_attrs()
        {
            if(orig_size < 0 || HighLogic.LoadedSceneIsEditor)
            {
                var resizer = base_part.Modules.GetModule<AnisotropicPartResizer>();
                orig_size = resizer != null ? resizer.size : size;
            }
            base.update_orig_attrs();
        }

        protected override void prepare_model()
        {
            if(prefab_model == null) return;
            orig_nodes.Clear();
            base_part.attachNodes.ForEach(n => orig_nodes[n.id] = n);
            orig_local_scale = prefab_model.localScale;
            if(orig_size > 0)
                update_model(GetScale());
        }

        void update_model(Scale scale)
        {
            //this.Log("first {}, orig scale {}, old scale {}, scale {} => {}\nsize {} => {}, aspect {} => {}",
                     //scale.FirstTime, orig_local_scale, old_local_scale, model.localScale, scale.ScaleVector(orig_local_scale),
                     //old_size, size, old_aspect, aspect);//debug
            model.localScale = scale.ScaleVector(orig_local_scale);
            model.hasChanged = true;
            part.transform.hasChanged = true;
            //recalculate mass and cost
            mass = get_scaled_attr(specificMass, scale, scale.aspect);
            cost = get_scaled_attr(specificCost, scale, scale.aspect);
            //update CoM offset
            part.CoMOffset = scale.ScaleVector(base_part.CoMOffset);
            //change breaking forces (if not defined in the config, set to a reasonable default)
            part.breakingForce = Mathf.Max(50000f, base_part.breakingForce * scale.absolute.quad);
            part.breakingTorque = Mathf.Max(50000f, base_part.breakingTorque * scale.absolute.quad);
            //change other properties
            part.explosionPotential = base_part.explosionPotential * scale.absolute.volume;
            //move attach nodes and attached parts
            update_attach_nodes(scale);
        }

        void update_attach_nodes(Scale scale)
        {
            //update attach nodes and their parts
            foreach(AttachNode node in part.attachNodes)
            {
                //ModuleGrappleNode adds new AttachNode on dock
                if(!orig_nodes.ContainsKey(node.id)) continue;
                //update node position
                node.position = scale.ScaleVector(node.originalPosition);
                //update node size
                int new_size = orig_nodes[node.id].size + Mathf.RoundToInt(scale.size - scale.orig_size);
                if(new_size < 0) new_size = 0;
                node.size = new_size;
                //update node breaking forces
                node.breakingForce = orig_nodes[node.id].breakingForce * scale.absolute.quad;
                node.breakingTorque = orig_nodes[node.id].breakingTorque * scale.absolute.quad;
                //move the part
                if(!scale.FirstTime)
                    part.UpdateAttachedPartPos(node);
            }
            //update this surface attach node
            if(part.srfAttachNode != null)
            {
                Vector3 old_position = part.srfAttachNode.position;
                part.srfAttachNode.position = scale.ScaleVector(part.srfAttachNode.originalPosition);
                //don't move the part at start, its position is persistant
                if(!scale.FirstTime)
                {
                    Vector3 d_pos = part.transform.TransformDirection(part.srfAttachNode.position - old_position);
                    part.transform.position -= d_pos;
                }
            }
            //no need to update surface attached parts on start
            //as their positions are persistant; less calculations
            if(scale.FirstTime) return;
            //update parts that are surface attached to this
            foreach(Part child in part.children)
            {
                if(child.srfAttachNode != null && child.srfAttachNode.attachedPart == part)
                {
                    Vector3 attachedPosition = child.transform.localPosition + child.transform.localRotation * child.srfAttachNode.position;
                    Vector3 targetPosition = scale.ScaleVectorRelative(attachedPosition);
                    child.transform.Translate(targetPosition - attachedPosition, part.transform);
                }
            }
        }

        public override void SaveDefaults()
        {
            old_size = size;
            create_updaters();
            base.SaveDefaults();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
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
                if(aspectOnly || minSize.Equals(maxSize)) Fields["size"].guiActiveEditor = false;
                else setup_field(Fields["size"], minSize, maxSize, sizeStepLarge, sizeStepSmall);
                if(sizeOnly || minAspect.Equals(maxAspect)) Fields["aspect"].guiActiveEditor = false;
                else setup_field(Fields["aspect"], minAspect, maxAspect, aspectStepLarge, aspectStepSmall);
                var sizeField = Fields[nameof(size)];
                sizeField.OnValueModified += on_size_changed;
            }
            Rescale();
        }

        protected override void OnDestroy()
        {
            Fields[nameof(size)].OnValueModified -= on_size_changed;
            base.OnDestroy();
        }

        public void Update()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                if(old_local_scale != model.localScale)
                {
                    this.Log("Model local scale changed");
                    Rescale();
                }
            }
        }

        void Rescale()
        {
            if(model == null) return;
            var scale = GetScale();
            update_model(scale);
            //update modules
            updaters.ForEach(u => u.OnRescale(scale));
            //save size and aspect
            old_size = size;
            old_aspect = aspect;
            old_local_scale = model.localScale;
            Utils.UpdateEditorGUI();
            part.UpdatePartMenu();
            if(HighLogic.LoadedSceneIsFlight)
                StartCoroutine(CallbackUtil.DelayedCallback(1, UpdateDragCube));
            just_loaded = false;
        }
    }
}
