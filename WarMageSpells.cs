using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using WarMage = Dawnsbury.Mods.Ooster.Subclasses.WarMage.DawnsburyWarMage;
using Dawnsbury.Core.Animations;
using Microsoft.Xna.Framework;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Auxiliary;

public class ResistanceAddedDamageModification : DamageModification
{
    private readonly int reductionAmount;

    private readonly string explanation;

    private readonly List<Resistance> appliedResistances;

    public ResistanceAddedDamageModification(int amount, string explanation, List<Resistance> resistances)
    {
        reductionAmount = amount;
        this.explanation = explanation;
        appliedResistances = resistances;
    }

    public override void Apply(DamageEvent damageEvent)
    {
        int num = 0;
        int num2 = reductionAmount;
        List<DamageKind> existingResistances = appliedResistances.Select(resistance => resistance.DamageKind).ToList();

        //Group together damages of the same kind
        List<DamageKind> uniqueDamageKinds = damageEvent.KindedDamages.Select(kd => kd.DamageKind).Distinct().ToList();
        Dictionary<DamageKind, int> summedDamages = damageEvent.KindedDamages.GroupBy(kd => kd.DamageKind).ToDictionary(group => group.Key, group => group.Sum(kd => kd.ResolvedDamage));
        Dictionary<DamageKind, int> amountsReduced = summedDamages.Keys.ToDictionary(k => k, k => 0);

        //throw new Exception(uniqueDamageKinds[0].ToString());

        foreach (DamageKind damageKind in uniqueDamageKinds)
        {
            if (existingResistances.Contains(damageKind)) {
                foreach (Resistance appliedResistance in appliedResistances)
                {
                    if (appliedResistance.Value > reductionAmount)
                    {
                        int difference = reductionAmount - appliedResistance.Value;
                        if (difference > summedDamages[appliedResistance.DamageKind])
                        {
                            amountsReduced[damageKind] = summedDamages[damageKind];
                        } else
                        {
                            amountsReduced[damageKind] = difference;
                        }
                    }
                    else
                    {
                        if (summedDamages[damageKind] >= reductionAmount)
                        {
                            amountsReduced[damageKind] = reductionAmount;
                        }
                        else
                        {
                            amountsReduced[damageKind] = summedDamages[damageKind];
                        }
                    }
                }
            } else
            {
                if (summedDamages[damageKind] >= reductionAmount)
                {
                    amountsReduced[damageKind] = reductionAmount;
                } else
                {
                    amountsReduced[damageKind] = summedDamages[damageKind];
                }
            }
        }

            num = amountsReduced.Values.Sum();

        foreach (KindedDamage kindedDamage in damageEvent.KindedDamages)
        {
            if (amountsReduced[kindedDamage.DamageKind] <= 0)
            {
                continue;
            }
            if (amountsReduced[kindedDamage.DamageKind] >= kindedDamage.ResolvedDamage)
            {
                kindedDamage.ResolvedDamage = 0;
                amountsReduced[kindedDamage.DamageKind] -= kindedDamage.ResolvedDamage;
            } else
            {
                kindedDamage.ResolvedDamage -= amountsReduced[kindedDamage.DamageKind];
                amountsReduced[kindedDamage.DamageKind] = 0;
            }
        };

        if (num != 0)
        {
            damageEvent.DamageEventDescription.AppendLine("{b}-" + num + "{/b} " + explanation);
        }
    }
}

namespace DawnsburyWarMage
{
    public class WarMageSpells
    {
        public static SpellId shieldingFormationId;
        public static CombatAction GenerateShieldingFormationSpell()
        {
             CombatAction shieldingFormationSpell = Spells.CreateModern(IllustrationName.ShieldSpell, "Shielding Formation", [Trait.Concentrate, Trait.Focus, Trait.Force, Trait.Manipulate, Trait.Wizard], "You conjure magical shields of force to protect your allies around you.", "You and each ally who ends their turn within the emanation gain a +1 circumstance bonus to AC until they leave the emanation or the spell ends, whichever comes first. If an ally takes physical damage or damage from a spell or magical effect while being granted this bonus, they can choose to end the bonus for themselves as a free action to gain resistance 10 to all damage against the triggering damage. If they do, they become temporarily immune to the effects of shielding formation for 10 minutes. You can do the same by spending your reaction; if you do, you can’t cast shielding formation again for 10 minutes, though you can continue Sustaining the benefits for others.", Target.Self(), 4, null);
            shieldingFormationSpell.WithActionCost(2);
            shieldingFormationSpell.WithSoundEffect(SfxName.ShieldSpell);
            shieldingFormationSpell.Illustration = WarMage.ShieldingFormationIllustration;

            QEffect shieldingFormationDisabler = new QEffect("shieldingFormationDisabler", "");
            shieldingFormationDisabler.ExpiresAt = ExpirationCondition.Never;

            QEffect shieldingFormationEffect = new QEffect("Shielding Formation", "You gain a +1 circumstance bonus to AC until you leave the emanation.");
            shieldingFormationEffect.CountsAsABuff = true;
            shieldingFormationEffect.BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => (defense == Defense.AC) ? new Bonus(+1, BonusType.Circumstance, "Shielding Formation") : null;
            shieldingFormationEffect.Illustration = WarMage.ShieldingFormationIllustration;
            shieldingFormationEffect.YouAreDealtDamage = async delegate (QEffect qf, Creature attacker, DamageStuff damageStuff, Creature defender)
            {
                bool result = await defender.AskForConfirmation(WarMage.ShieldingFormationIllustration, $"You are about to take {damageStuff.Amount} damage. End the effects of Shielding Formation on yourself to gain resistance 10 to all damage against the triggering attack?\nYou will become immune to the effects of Shielding Formation until the end of this encounter.", "Confirm");
                if (result)
                {
                    shieldingFormationDisabler.Source = qf.Source;
                    defender.AddQEffect(shieldingFormationDisabler);
                    qf.ExpiresAt = ExpirationCondition.Ephemeral;
                    return new ResistanceAddedDamageModification(10, "Shielding Formation", defender.WeaknessAndResistance.Resistances);
                }
                return null;
            };
            shieldingFormationEffect.StateCheck = async delegate (QEffect qf)
            {
                if (qf.Owner.QEffects.Any(eff => (eff.Name == "shieldingFormationDisabler" && eff.Source == qf.Source)) || !qf.Owner.QEffects.Any(eff => eff.Name == "shieldingFormationGranter"))
                {
                    qf.Owner.QEffects.ForEach(eff =>
                    {
                        if (eff.Name == "shieldingFormationGranter" && eff.Source == qf.Source)
                        {
                            eff.ExpiresAt = ExpirationCondition.Ephemeral;
                        }
                    });
                }
            };

            QEffect shieldingFormationGranter = new QEffect("shieldingFormationGranter", "");
            shieldingFormationGranter.EndOfYourTurnBeneficialEffect = async delegate (QEffect self, Creature creature)
            {
                creature.RemoveAllQEffects(effect => effect.Name == shieldingFormationEffect.Name && effect.Source == self.Source);
                shieldingFormationEffect.Source = self.Source;
                creature.AddQEffect(shieldingFormationEffect);
            };
            shieldingFormationGranter.ExpiresAt = ExpirationCondition.Ephemeral;

            QEffect shieldingFormationSelfEffect = new QEffect("Shielding Formation", "You gain a +1 circumstance bonus to AC until you leave the emanation.");
            shieldingFormationSelfEffect.CountsAsABuff = true;
            shieldingFormationSelfEffect.BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => (defense == Defense.AC) ? new Bonus(+1, BonusType.Circumstance, "Shielding Formation") : null;
            shieldingFormationSelfEffect.ExpiresAt = ExpirationCondition.Ephemeral;
            shieldingFormationSelfEffect.Illustration = WarMage.ShieldingFormationIllustration;
            shieldingFormationSelfEffect.YouAreDealtDamage = async delegate (QEffect qf, Creature attacker, DamageStuff damageStuff, Creature defender)
            {
                bool result = await defender.AskToUseReaction($"You are about to take {damageStuff.Amount} damage. End the effects of Shielding Formation on yourself to gain resistance 10 to all damage against the triggering attack?\nYou will become immune to the effects of Shielding Formation until the end of this encounter and won't be able to cast the spell again, though you can sustain it for your allies.");
                if (result)
                {
                    shieldingFormationDisabler.Source = qf.Source;
                    defender.AddQEffect(shieldingFormationDisabler);
                    qf.ExpiresAt = ExpirationCondition.Ephemeral;
                    return new ResistanceAddedDamageModification(10, "Shielding Formation", defender.WeaknessAndResistance.Resistances);
                }
                return null;
            };

            shieldingFormationSpell.WithEffectOnSelf(async (action, self) =>
            {
                AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle, 6);
                auraAnimation.Color = Color.Orange;
                QEffect qf = new QEffect("shieldingFormationAura", "", ExpirationCondition.Never, self);
                qf.CannotExpireThisTurn = true;
                qf.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                qf.WhenExpires = (_) =>
                {
                    auraAnimation.MoveTo(0f);
                };
                qf.StateCheck = (effect) =>
                {
                    foreach (Creature target in effect.Owner.Battle.AllCreatures.Where((creature) => !creature.QEffects.Any(effect => effect.Name == "shieldingFormationDisabler") && creature.DistanceTo(effect.Owner) <= 6 && !creature.EnemyOf(effect.Owner))) {
                        if (target == effect.Owner)
                        {
                            target.AddQEffect(shieldingFormationSelfEffect);
                        } else
                        {
                            shieldingFormationGranter.Source = self;
                            target.AddQEffect(shieldingFormationGranter);
                        }
                    };
                    foreach (Creature target in effect.Owner.Battle.AllCreatures.Where((creature) => creature.DistanceTo(effect.Owner) > 6)) {
                        target.RemoveAllQEffects(eff => eff.Name == "Shielding Formation" && eff.Source == self);
                    };
                };
                self.AddQEffect(qf.WithExpirationSustained(shieldingFormationSpell, self));
            });
            return shieldingFormationSpell;
        }
    }
}
