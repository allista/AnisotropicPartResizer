//   PartUpdaters.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2015 Allis Tauri

// This code is based on Procedural Fairings plug-in by Alexey Volynskov, KzPartResizer class
// And on ideas drawn from the TweakScale plugin

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AT_Utils
{
    public class NodesUpdater : PartUpdater
    {
        readonly Dictionary<string, AttachNode> orig_nodes = new Dictionary<string, AttachNode>();

        public override void SaveDefaults()
        { base_part.attachNodes.ForEach(n => orig_nodes[n.id] = n); }

        public override void OnRescale(Scale scale)
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
    }

    public class PropsUpdater : PartUpdater
    {
        public override void OnRescale(Scale scale)
        {
            //update CoM offset
            part.CoMOffset = scale.ScaleVector(base_part.CoMOffset);
            //update drag cubes
            part.DragCubes.ForceUpdate(true, true, true);
            //change breaking forces (if not defined in the config, set to a reasonable default)
            part.breakingForce = Mathf.Max(22f, base_part.breakingForce * scale.absolute.quad);
            part.breakingTorque = Mathf.Max(22f, base_part.breakingTorque * scale.absolute.quad);
            //change other properties
            part.explosionPotential = base_part.explosionPotential * scale.absolute.volume;

        }
    }

    /// <summary>
    /// Emitter updater. Adapted from TweakScale.
    /// </summary>
    public class EmitterUpdater : PartUpdater
    {
        struct EmitterData
        {
            public readonly float MinSize, MaxSize, Shape1D;
            public readonly Vector2 Shape2D;
            public readonly Vector3 Shape3D, LocalVelocity, Force;
            public EmitterData(KSPParticleEmitter pe)
            {
                MinSize = pe.minSize;
                MaxSize = pe.maxSize;
                Shape1D = pe.shape1D;
                Shape2D = pe.shape2D;
                Shape3D = pe.shape3D;
                Force = pe.force;
                LocalVelocity = pe.localVelocity;
            }
        }

        Scale scale;
        readonly Dictionary<KSPParticleEmitter, EmitterData> orig_scales = new Dictionary<KSPParticleEmitter, EmitterData>();

        void UpdateParticleEmitter(KSPParticleEmitter pe)
        {
            if(pe == null) return;
            if(!orig_scales.ContainsKey(pe))
                orig_scales[pe] = new EmitterData(pe);
            var ed = orig_scales[pe];
            pe.minSize = ed.MinSize * scale;
            pe.maxSize = ed.MaxSize * scale;
            pe.shape1D = ed.Shape1D * scale;
            pe.shape2D = ed.Shape2D * scale;
            pe.shape3D = ed.Shape3D * scale;
            pe.force = ed.Force * scale;
            pe.localVelocity = ed.LocalVelocity * scale;
        }

        public override void OnUpdate()
        {
            if(scale == null) return;
            var emitters = part.gameObject.GetComponentsInChildren<KSPParticleEmitter>();
            if(emitters == null) return;
            emitters.ForEach(UpdateParticleEmitter);
            scale = null;
        }

        public override void OnRescale(Scale scale)
        {
            if(part.FindModelComponent<KSPParticleEmitter>() != null ||
               part.GetComponents<EffectBehaviour>()
               .Any(e => e is ModelMultiParticleFX || e is ModelParticleFX))
                this.scale = scale;
        }
    }

    public class ResourcesUpdater : PartUpdater
    {
        public override void OnRescale(Scale scale)
        {
            //no need to update resources on start
            //as they are persistant; less calculations
            if(scale.FirstTime) return;
            foreach(PartResource r in part.Resources)
            {
                var surface = r.resourceName == "AblativeShielding" ||
                    r.resourceName == "Ablator";
                var s = surface ?
                    scale.relative.quad : scale.relative.volume;
                r.amount *= s; r.maxAmount *= s;
            }
        }
    }

    public class RCS_Updater : ModuleUpdater<ModuleRCS>
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Thrust")]
        public string thrustDisplay;

        string all_thrusts()
        {
            return modules
                .Aggregate("", (s, mp) => s + mp.module.thrusterPower + ", ")
                .Trim(", ".ToCharArray());
        }

        public override void OnStart(StartState state) { base.OnStart(state); thrustDisplay = all_thrusts(); }
        public override void OnRescale(Scale scale) { base.OnRescale(scale); thrustDisplay = all_thrusts(); }

        protected override void on_rescale(ModulePair<ModuleRCS> mp, Scale scale)
        { mp.module.thrusterPower = mp.base_module.thrusterPower * scale.absolute.quad; }
    }

    public class DockingNodeUpdater : ModuleUpdater<ModuleDockingNode>
    {
        protected override void on_rescale(ModulePair<ModuleDockingNode> mp, Scale scale)
        {
            AttachNode node = part.FindAttachNode(mp.module.referenceAttachNode);
            if(node == null) return;
            if(mp.module.nodeType.StartsWith("size"))
                mp.module.nodeType = string.Format("size{0}", node.size);
        }
    }

    public class ReactionWheelUpdater : ModuleUpdater<ModuleReactionWheel>
    {
        protected override void on_rescale(ModulePair<ModuleReactionWheel> mp, Scale scale)
        {
            mp.module.PitchTorque = mp.base_module.PitchTorque * scale.absolute.quad * scale.absolute.aspect;
            mp.module.YawTorque = mp.base_module.YawTorque * scale.absolute.quad * scale.absolute.aspect;
            mp.module.RollTorque = mp.base_module.RollTorque * scale.absolute.quad * scale.absolute.aspect;
            var input_resources = mp.base_module.resHandler.inputResources.ToDictionary(r => r.name);
            mp.module.resHandler.inputResources.ForEach(r => r.rate = input_resources[r.name].rate * scale.absolute.quad * scale.absolute.aspect);
        }
    }

    public class GeneratorUpdater : ModuleUpdater<ModuleGenerator>
    {
        protected override void on_rescale(ModulePair<ModuleGenerator> mp, Scale scale)
        {
            var input_resources = mp.base_module.resHandler.inputResources.ToDictionary(r => r.name);
            var output_resources = mp.base_module.resHandler.inputResources.ToDictionary(r => r.name);
            mp.module.resHandler.inputResources.ForEach(r => r.rate = input_resources[r.name].rate * scale.absolute.volume);
            mp.module.resHandler.inputResources.ForEach(r => r.rate = output_resources[r.name].rate * scale.absolute.volume);
        }
    }

    public class AsteroidDrillUpdater : ModuleUpdater<ModuleAsteroidDrill>
    {
        protected override void on_rescale(ModulePair<ModuleAsteroidDrill> mp, Scale scale)
        {
            mp.module.PowerConsumption = mp.base_module.PowerConsumption * scale;
            mp.module.Efficiency = mp.base_module.Efficiency * scale;
        }
    }

    public class ResourceHarvesterUpdater : ModuleUpdater<ModuleResourceHarvester>
    {
        protected override void on_rescale(ModulePair<ModuleResourceHarvester> mp, Scale scale)
        {
            mp.module.Efficiency = mp.base_module.Efficiency * scale;
            var def_ratios = mp.base_module.inputList.ToDictionary(r => r.ResourceName);
            mp.module.inputList.ForEach(r => r.Ratio = def_ratios[r.ResourceName].Ratio * scale);
        }
    }

    public class ResourceConverterUpdater : ModuleUpdater<ModuleResourceConverter>
    {
        static void scale_res_list(List<ResourceRatio> cur, List<ResourceRatio> def, float scale)
        {
            var def_ratios = def.ToDictionary(r => r.ResourceName);
            cur.ForEach(r => r.Ratio = def_ratios[r.ResourceName].Ratio * scale);
        }

        protected override void on_rescale(ModulePair<ModuleResourceConverter> mp, Scale scale)
        {
            scale_res_list(mp.module.Recipe.Inputs, mp.base_module.Recipe.Inputs, scale);
            scale_res_list(mp.module.Recipe.Outputs, mp.base_module.Recipe.Outputs, scale);
            scale_res_list(mp.module.Recipe.Requirements, mp.base_module.Recipe.Requirements, scale);
        }
    }

    public class SolarPanelUpdater : ModuleUpdater<ModuleDeployableSolarPanel>
    {
        protected override void on_rescale(ModulePair<ModuleDeployableSolarPanel> mp, Scale scale)
        {
            mp.module.chargeRate = mp.base_module.chargeRate * scale.absolute.quad * scale.absolute.aspect;
            mp.module.flowRate = mp.base_module.flowRate * scale.absolute.quad * scale.absolute.aspect;
        }
    }

    public class DecoupleUpdater : ModuleUpdater<ModuleDecouple>
    {
        protected override void on_rescale(ModulePair<ModuleDecouple> mp, Scale scale)
        { mp.module.ejectionForce = mp.base_module.ejectionForce * scale.absolute.cube; }
    }

    public class EngineUpdater : ModuleUpdater<ModuleEngines>
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Max. Thrust")]
        public string thrustDisplay;

        string all_thrusts()
        {
            return modules
                .Aggregate("", (s, mp) => s + mp.module.maxThrust + ", ")
                .Trim(", ".ToCharArray());
        }

        public override void OnStart(StartState state) { base.OnStart(state); thrustDisplay = all_thrusts(); }
        public override void OnRescale(Scale scale) { base.OnRescale(scale); thrustDisplay = all_thrusts(); }

        protected override void on_rescale(ModulePair<ModuleEngines> mp, Scale scale)
        {
            mp.module.minThrust = mp.base_module.minThrust * scale.absolute.quad;
            mp.module.maxThrust = mp.base_module.maxThrust * scale.absolute.quad;
            //            mp.module.heatProduction = mp.base_module.heatProduction * scale.absolute;
        }
    }

    public class ResourceIntakeUpdater : ModuleUpdater<ModuleResourceIntake>
    {
        protected override void on_rescale(ModulePair<ModuleResourceIntake> mp, Scale scale)
        { mp.module.area = mp.base_module.area * scale.absolute.quad; }
    }

    public class JettisonUpdater : ModuleUpdater<ModuleJettison>
    {
        public override void SaveDefaults()
        {
            base.SaveDefaults();
            foreach(var mp in modules)
                mp.orig_data["local_scale"] = mp.module.jettisonTransform.localScale;
        }

        protected override void on_rescale(ModulePair<ModuleJettison> mp, Scale scale)
        {
            mp.module.jettisonedObjectMass = mp.base_module.jettisonedObjectMass * scale.absolute.volume;
            mp.module.jettisonForce = mp.base_module.jettisonForce * scale.absolute.volume;
            if(mp.module.jettisonTransform != null)
            {
                var p = mp.module.jettisonTransform.parent.gameObject.GetComponent<Part>();
                if(p == null || p == mp.module.part) return;
                object orig_scale;
                if(!mp.orig_data.TryGetValue("local_scale", out orig_scale) ||
                   !(orig_scale is Vector3)) return;
                mp.module.jettisonTransform.localScale = scale.ScaleVector((Vector3)orig_scale);
            }
        }
    }

    public class ControlSurfaceUpdater : ModuleUpdater<ModuleControlSurface>
    {
        protected override void on_rescale(ModulePair<ModuleControlSurface> mp, Scale scale)
        {
            mp.module.ctrlSurfaceArea = mp.base_module.ctrlSurfaceArea * scale.absolute.quad;
        }
    }
}
