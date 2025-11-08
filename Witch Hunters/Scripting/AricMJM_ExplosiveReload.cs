using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts;

namespace XRL.World.Parts
{
    public class AricMJM_ExplosiveReload : IPart
    {
        int force = 2000;
        string damage = "2d6";


        


        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade) || ID == CommandReloadEvent.ID || ID == MissilePenetrateEvent.ID;


        }

        public bool ObjectHasEffectFromThisWeapon(GameObject GO) => GO.TryGetEffect(out AricMJM_ExplosiveBullet eb) && eb.Origin == ParentObject;

        public override bool HandleEvent(CommandReloadEvent E)
        {
            int counter = 0;
            foreach (GameObject affectedObject in The.ActiveZone.GetObjects(ObjectHasEffectFromThisWeapon))
            {
                counter++;
                //AddPlayerMessage("Trigger " + counter + " incoming!");
                foreach (Effect effect in new List<Effect>(affectedObject.Effects))
                {
                    if (effect is AricMJM_ExplosiveBullet eb
                        && eb.Origin == ParentObject)
                    {
                        eb.Trigger();
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(MissilePenetrateEvent E)
        {

            
            E.Defender.ApplyEffect(new AricMJM_ExplosiveBullet(damage, force, ParentObject));

    

            return base.HandleEvent(E);
        }
    }
}
namespace XRL.World.Effects
{
    [Serializable]

    public class AricMJM_ExplosiveBullet : Effect
    {
        
        public string damage;
        public int force;
        public GameObject Origin;



        public override int GetEffectType() => TYPE_CONTACT | TYPE_NEGATIVE | TYPE_REMOVABLE;

        public AricMJM_ExplosiveBullet(string ConDamage, int Conforce, GameObject origin)
        {
            this.damage = ConDamage;
            this.force = Conforce;
            
            Origin = origin;
            Duration = DURATION_INDEFINITE;
            DisplayName = "{{dark firey|primed}}";
        }
        public bool Trigger()
        {


           // foreach (Effect effect in Object.Effects)
            {
                //if (effect is AricMJM_ExplosiveBullet)
                {
                    
                    XRL.World.Parts.Physics.ApplyExplosion(Force: force, UsedCells: null, Hit: null, Local: false, Show: true, Owner: null, BonusDamage: damage, C: currentCell, WhatExploded: Object);

                    //AddPlayerMessage("SPLOSION");
                   // break;

                }

            }
                   Object.RemoveEffect(this);
                    return true;
            
        }


        public override bool CanApplyToStack()
        {
            return true;

        }
        public override string GetDetails()
        {
            StringBuilder stringBuilder = Event.NewStringBuilder();
            stringBuilder.Append("An explosive bullet is lodged within");
            return stringBuilder.ToString();
        }
    }
}
