using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using WarMage = Dawnsbury.Mods.Ooster.Subclasses.WarMage.DawnsburyWarMage;

namespace DawnsburyWarMage
{
    public class WarMageFeats
    {
        public static FeatName warMageDedicationName;
        public static FeatName magesFieldDressingName;
        public static FeatName shieldReinforcementName;
        public static FeatName intimidatingSpellName;
        public static FeatName shieldingFormationFeatName;
        public static FeatName arcanaOfIronName;
        public static FeatName drainBondedItemName;
        public static QEffectId dressingTagId;
        public static QEffect arcaneBond;
        public static Feat GenerateDedicationFeat()
        {
            TrueFeat warMageDedicationFeat = new TrueFeat(warMageDedicationName, 2, "You have begun to hone your skills as a war mage.", "Your studies into the battlefield applications of magic have made your spells particularly effective at disrupting enemy formations or manipulating enemy troops into positions where they are more vulnerable to wide-scale magical attacks. When you cast a non-cantrip spell that deals damage in an area, choose a number of targets equal to your Intelligence modifier who failed their saving throw against the effect. Move each one up to 10 feet from their original position after they take damage and any other effects from the spell. You can't move a creature into or through obstacles. A Medium or smaller creature counts as one target; a Large creature counts as two Medium creatures; and a Huge creature counts as four Medium creatures. You can't move Gargantuan creatures in this way.\n\nYou also gain the Additional Lore general feat for Warfare Lore.", [Trait.Archetype, Trait.Dedication]);
            QEffect warMageRider = new QEffect();
            warMageRider.YouBeginAction = async (self, action) =>
            {
                ChosenTargets targets = action.ChosenTargets;
                targets.ChosenCreatures.ForEach(target =>
                {
                    QEffect qf = new QEffect("warMageTag", "");
                    qf.Innate = false;
                    qf.ExpiresAt = ExpirationCondition.ExpiresAtEndOfAnyTurn;
                    qf.YouAreDealtDamage = async delegate (QEffect qf, Creature attacker, DamageStuff damageStuff, Creature defender)
                    {
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                        QEffect damagingSpell = new QEffect("wasDamagingSpell", "");
                        damagingSpell.Innate = false;
                        defender.AddQEffect(damagingSpell);
                        return null;
                    };
                    qf.CountsAsABuff = true;
                    target.AddQEffect(qf);
                });
            };

            warMageRider.AfterYouDealDamage = async (attacker, action, target) =>
            {

            };

            warMageRider.AfterYouTakeAction = async (qf, action) =>
            {
                Creature attacker = action.Owner;
                var targets = action.ChosenTargets;
                int remainingTargets = attacker.Abilities.Intelligence;
                if (action.SpellcastingSource != null && !action.HasTrait(Trait.Cantrip) && action.Target.IsAreaTarget && action.WithSpellSavingThrow != null)
                {
                    List<Creature> eligibleTargets = targets.ChosenCreatures.Where(creature => creature.QEffects.Any(qf => qf.Name == "wasDamagingSpell") && (int)creature.Space.Size <= 3 && (int)creature.Space.Size <= remainingTargets && (int)action.ChosenTargets.CheckResults[creature] <= 1 && creature.HP > 0).ToList();
                    while (eligibleTargets.Count > 0 && remainingTargets > 0)
                    {
                        var targetCreature = await attacker.Battle.AskToChooseACreature(attacker, eligibleTargets, IllustrationName.TelekineticManeuver, $"Choose a creature to displace. You can displace {remainingTargets.ToString()} more creatures.", "", "Cancel");

                        if (targetCreature == null)
                        {
                            break;
                        }

                        List<Tile> validTiles = attacker.Battle.Map.AllTiles.Where(tile => tile.DistanceTo(targetCreature) <= 2 && tile.CanIStopMyMovementHere(targetCreature) && (int)targetCreature.HasLineOfEffectTo(tile) < 4).ToList();
                        var result = await attacker.Battle.AskToChooseATile(attacker, validTiles, action.Illustration, "Choose tile to move creature to", "", true, true, targetCreature, "cancel");
                        await targetCreature.MoveTo(result, CombatAction.CreateSimple(attacker, "War mage"), new MovementStyle()
                        {
                            Shifting = true,
                            ForcedMovement = true,
                            MaximumSquares = 3
                        });

                        eligibleTargets.Remove(targetCreature);
                        eligibleTargets.RemoveAll(creature => (int)creature.Space.Size > remainingTargets);
                        remainingTargets -= (int)targetCreature.Space.Size;
                    }
                }
                foreach (Creature creature in targets.ChosenCreatures)
                {
                    creature.RemoveAllQEffects(qf => qf.Name == "wasDamagingSpell" || qf.Name == "warMageTag");
                }
            };

            warMageDedicationFeat.WithOnSheet(sheet =>
            {
                sheet.AddFeat(AllFeats.All.Find(feat => feat.Name == "Additional Lore - Warfare"), null);
            });

            warMageDedicationFeat.WithOnCreature(creature =>
            {
                creature.AddQEffect(warMageRider);
            });

            warMageDedicationFeat.WithPrerequisite(sheet => sheet.HasFeat(WarMage.warMageFeat), "You must be a War Mage wizard.");

            return warMageDedicationFeat;
        }

        private static QEffect GenerateDressingTag(Creature owner)
        {
            QEffect dressingTag = new QEffect("dressingTag", "");
            dressingTag.Id = dressingTagId;
            dressingTag.Innate = false;
            dressingTag.Source = owner;
            dressingTag.WithExpirationAtEndOfThisTurn();
            return dressingTag;
        }

        //Mage's Field Dressing
        public static Feat GenerateFieldDressingFeat()
        { 
            TrueFeat fieldDressingFeat = new TrueFeat(magesFieldDressingName, 4, "As your spell takes hold on your ally, you use some of its magic to quickly dress their wounds.", "You use Battle Medicine on one ally affected by the required spell. You conjure glowing threads, bandages, or similar supplies out of pure magic, which adds the arcane trait to Battle Medicine.", [WarMage.warMageTrait, Trait.Archetype], null);
            fieldDressingFeat.WithOnCreature(creature =>
            {
                QEffect qf = new QEffect("Mage's Field Dressing", "");
                qf.AfterYouTakeAction = async (qf, action) =>
                {
                    foreach (Creature creature in action.Owner.Battle.AllCreatures)
                    {
                        creature.RemoveAllQEffects(effect => effect.Name == "dressingTag");
                    };
                    if (action.SpellcastingSource != null && action.SpellcastingSource.ClassOfOrigin == Trait.Wizard && !action.HasTrait(Trait.Cantrip) && !action.HasTrait(Trait.Focus))
                    {
                        foreach (Creature target in action.ChosenTargets.ChosenCreatures)
                        {
                            if (action.ChosenTargets.AffectedThisCreatureSomehow(action, target));
                            target.AddQEffect(GenerateDressingTag(qf.Owner));
                        }
                    }
                };
                qf.Traits.Add(Trait.Basic);
                creature.AddQEffect(qf);
            });
            fieldDressingFeat.WithPrerequisite(sheet => sheet.HasFeat(AllFeats.GetFeatByFeatName(FeatName.BattleMedicine)), "Battle Medicine");
            fieldDressingFeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);

            fieldDressingFeat.WithOnCreature(creature =>
            {
                CombatAction? battleMedicineAction = null;
                List<CombatAction> battleMedicineActions = [];

                creature.AddQEffect(new QEffect
                {
                    ProvideActionIntoPossibilitySection = (self, section) =>
                    {
                        if (section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers && battleMedicineActions.Count == 1)
                        {
                            return GenerateFieldDressingAction(self.Owner, battleMedicineActions[0]);
                        }
                        if (section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers && battleMedicineActions.Count > 0)
                        {
                            List<ActionPossibility> battleMedicinePossibilities = battleMedicineActions.Select(action => GenerateFieldDressingAction(action.Owner, action)).ToList();
                            PossibilitySection fieldDressingPossibilities = new PossibilitySection("Mage's Field Dressing");
                            foreach (ActionPossibility possibility in battleMedicinePossibilities)
                            {
                                fieldDressingPossibilities.AddPossibility(possibility);
                            }

                            return new SubmenuPossibility(IllustrationName.LayOnHands, "Mage's Field Dressing")
                            {
                                Subsections =
                                {
                                    fieldDressingPossibilities
                                }
                            };
                        }
                        return null;
                    },
                    ModifyActionPossibility = (self, combatAction) =>
                    {
                        if (combatAction.Action.Name.Contains("Battle Medicine"))
                        {
                            battleMedicineActions.Add(combatAction);
                            battleMedicineActions = battleMedicineActions.DistinctBy(action => action.Name).ToList();
                        }
                    }
                });
            });
            return fieldDressingFeat;
        }

        private static ActionPossibility GenerateFieldDressingAction(Creature owner, CombatAction battleMedicineAction)
        {
            Target dressingTarget = new CreatureTarget(RangeKind.Melee, [new MaximumRangeCreatureTargetingRequirement(60), new FriendOrSelfCreatureTargetingRequirement(), new TargetHasQEffectCreatureTargetingRequirement(dressingTagId, ""), new DamagedCreatureTargetingRequirement()], (_, _, _) => 0);
            CombatAction fieldDressingAction = new CombatAction(owner, IllustrationName.LayOnHands, "Mage's Field Dressing", [Trait.Arcane, Trait.Manipulate, Trait.Healing], "{b}Range:{/b} 60 feet\n{b}Requirement:{/b} Your last action was to cast a spell that affected at least one ally.\n\nYou use Battle Medicine on a creature affected by your previous spell. This does not require a free hand as you magically conjure the required tools.", Target.RangedFriend(60)
                .WithAdditionalConditionOnTargetCreature((user, target) =>
                {
                    if (!target.HasEffect(dressingTagId))
                    {
                        return Usability.NotUsableOnThisCreature("Target has not been affected by your spell.");
                    }

                    if (target.Damage == 0)
                    {
                        return Usability.NotUsableOnThisCreature("healthy");
                    }


                    if (!target.PersistentUsedUpResources.UsedUpActions.Contains($"BattleMedicineFrom:{user.BaseName}"))
                    {
                        return Usability.Usable;
                    }

                    else if (!user.PersistentUsedUpResources.UsedUpActions.Contains("BattleMedicineImmunityBypassUsed") && user.PersistentCharacterSheet.Calculated.AllFeats.Any(feat => feat.Name == "Medic Dedication"))
                    {
                        return Usability.Usable;
                    }

                    return Usability.NotUsableOnThisCreature("Target has been already affected by your Battle Medicine today.");
                }));
            
            if (battleMedicineAction.Name.Contains("DC 20"))
            {
                fieldDressingAction.Name += " (DC 20)";
                fieldDressingAction.Traits.Add(Trait.Basic);
            }

            else if (battleMedicineAction.Name.Contains("DC 30"))
            {
                fieldDressingAction.Name += " (DC 30)";
                fieldDressingAction.Traits.Add(Trait.Basic);
            }

            else if (battleMedicineAction.Name.Contains("DC 40"))
            {
                fieldDressingAction.Name += " (DC 40)";
                fieldDressingAction.Traits.Add(Trait.Basic);
            }

            fieldDressingAction.WithActionCost(1);
            fieldDressingAction.WithActiveRollSpecification(battleMedicineAction.ActiveRollSpecification);
            fieldDressingAction.EffectOnChosenTargets = battleMedicineAction.EffectOnChosenTargets;
            fieldDressingAction.EffectOnOneTarget = battleMedicineAction.EffectOnOneTarget;
            fieldDressingAction.Illustration = IllustrationName.LayOnHands;
            fieldDressingAction.WithShortDescription("Use Battle Medicine at a distance on an ally affected by your spell.");
            return fieldDressingAction;
        }

        public static async Task<DamageModification?> RequestAllyShieldBlock(Creature attacker, Creature defender, DamageStuff damageStuff, Creature blocker)
        {
            int hardness = 5 * (((blocker.Level - 1) / 4) + 1);

            if (await blocker.Battle.AskToUseReaction(blocker, $"{defender.Name} would be dealt damage by {damageStuff.Power.Name}. Use your magic shield to block up to {hardness} damage? (You won't be able to cast Shield again for the rest of the encounter."))
            {
                Sfxs.Play(SfxName.ShieldSpell);
                return new ReduceDamageModification(hardness, "Shield Block");
            }
            return null;
        }

        public static TrueFeat GenerateShieldSpellReinforcement()
        {
            TrueFeat ShieldFeat = new TrueFeat(shieldReinforcementName, 4, "You can empower your defensive magic by channeling it through a physical shield. If your next action is to cast the shield cantrip, you use your shield as a locus to cast the spell, raising a magical barrier in the form of a large force projection of your worn shield.", "You can choose for an adjacent ally to gain the benefits of the shield spell instead of yourself. If the ally would take damage from a physical attack or a Magic Missile spell while protected by your shield cantrip, you can use your reaction to Shield Block with the spell on their behalf.", [Trait.Metamagic], null);
            ShieldFeat.WithOnCreature(creature =>
            {
                QEffect qf = new QEffect("Shield Spell Reinforcement", "Protect allies with your Shield spell while you are holding a physical shield.");
                MetamagicProvider shieldMetamagic = new MetamagicProvider("Shield Spell Reinforcement", spell =>
                {
                    if (spell.Owner.HeldItems.Any(item => item.HasTrait(Trait.Shield) && spell.Name == "Shield"))
                    {
                        CombatAction modifiedShieldSpell = new CombatAction(spell.Owner, spell.Illustration, spell.Name, spell.Traits.ToArray(), spell.Description, Target.AdjacentFriend());
                        modifiedShieldSpell.SpellcastingSource = spell.SpellcastingSource;
                        modifiedShieldSpell.SpellLevel = spell.SpellLevel;
                        modifiedShieldSpell.SpellId = spell.SpellId;
                        modifiedShieldSpell.WithEffectOnChosenTargets(async (spell, caster, target) =>
                        {
                            if (!spell.Disrupted)
                            {
                                QEffect shieldEffect = new QEffect("Shield", "A magical shield of force grants you a +1 circumstance bonus to AC and allows " + caster.Name + " to Shield Block for you.");
                                shieldEffect.Id = QEffectId.ShieldSpell;
                                shieldEffect.CountsAsABuff = true;
                                shieldEffect.Innate = false;
                                shieldEffect.BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => (defense == Defense.AC) ? new Bonus(+1, BonusType.Circumstance, "Shield") : null;
                                shieldEffect.Illustration = spell.Illustration;
                                shieldEffect.WithExpirationAtStartOfSourcesTurn(spell.Owner, 1);
                                shieldEffect.Source = caster;
                                shieldEffect.YouAreDealtDamage = async (effect, source, damageStuff, defender) =>
                                {
                                    if (damageStuff.Power != null && (damageStuff.Kind.IsPhysical() || (damageStuff.Power.Name == "Force Barrage" || damageStuff.Power.Name == "Magic Missile")))
                                    {
                                        DamageModification? result = await RequestAllyShieldBlock(source, defender, damageStuff, spell.Owner);
                                        if (result != null)
                                        {
                                            spell.SpellcastingSource.Cantrips.RemoveAll(sp => sp.SpellId == SpellId.Shield);
                                            defender.RemoveAllQEffects(qf => qf.Id == QEffectId.ShieldSpell);
                                            spell.Owner.AddQEffect(new QEffect("Shield Remover", "")
                                            {
                                                StateCheck = (self) =>
                                                {
                                                    self.Owner.Possibilities.Filter(possibility => possibility.CombatAction.SpellId != SpellId.Shield);
                                                }
                                            });
                                            return result;
                                        }
                                    }
                                    return null;
                                };
                                target.ChosenCreature.AddQEffect(shieldEffect);
                            }
                        });
                        modifiedShieldSpell.WithSoundEffect(SfxName.ShieldSpell);
                        return modifiedShieldSpell;
                    }
                    return null;
                });
                qf.MetamagicProvider = shieldMetamagic;
                creature.AddQEffect(qf);
            });
            ShieldFeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);
            return ShieldFeat;
        }

        public static TrueFeat GenerateArcanaOfIron()
        {
            TrueFeat ArcanaOfIronfeat = new TrueFeat(arcanaOfIronName, 6, "You eschew wands and staves for more advanced weaponry.", "You become trained in advanced weapons. If you gain the weapon expertise class feature, your proficiency in martial and advanced weapons increases to expert. The extra damage you deal with Bespell Weapon increases to 1d8.", [Trait.Archetype], null);
            QEffect qf = new QEffect("Arcana of Iron", "");
            qf.StateCheck = self => {
                self.Owner.RemoveAllQEffects(effect => effect.Name == "Bespelled Weapon" && !effect.Description.Contains("1d8"));
            };
            qf.AfterYouTakeAction = async (effect, action) =>
            {
                if (action.SpellcastingSource != null && !action.HasTrait(Trait.Cantrip))
                {
                    DamageKind damageKind = DamageKind.Force;

                    if (action.HasTrait(Trait.Necromancy))
                    {
                        damageKind = DamageKind.Negative;
                    }

                    else if (action.HasTrait(Trait.Divination) || action.HasTrait(Trait.Enchantment) || action.HasTrait(Trait.Illusion))
                    {
                        damageKind = DamageKind.Mental;
                    }

                    else if (action.HasTrait(Trait.Conjuration) || action.HasTrait(Trait.Transmutation))
                    {
                        damageKind = DamageKind.Untyped;
                    }

                    QEffect bespellWeapon = new QEffect("Bespelled Weapon", "Your weapon deals 1d8 extra damage.");
                    if (damageKind == DamageKind.Untyped)
                    {
                        bespellWeapon.YouDealDamageWithStrike = (effect, action, diceFormula, target) =>
                        {
                            return diceFormula.Add(DiceFormula.FromText("1d8", "Bespell Weapon"));
                        };
                    }
                    else
                    {
                        bespellWeapon.AddExtraKindedDamageOnStrike = (action, target) =>
                        {
                            return new KindedDamage(DiceFormula.FromText("1d8", "Bespell Weapon"), damageKind);
                        };
                    }
                    bespellWeapon.Illustration = IllustrationName.TrueStrike;
                    bespellWeapon.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                    bespellWeapon.CountsAsABuff = true;

                    if (action.Owner.QEffects.First(effect => effect.Name == "Bespelled Weapon") != null)
                    {
                        action.Owner.AddQEffect(bespellWeapon);
                    }
                }
            };

            ArcanaOfIronfeat.WithOnSheet(sheet =>
            {
                sheet.IncreaseProficiency(6, Trait.Advanced, Proficiency.Trained);
                sheet.IncreaseProficiency(11, Trait.Advanced, Proficiency.Expert);
            });
            ArcanaOfIronfeat.WithOnCreature(creature =>
            {
                creature.AddQEffect(qf);
            });
            ArcanaOfIronfeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);
            ArcanaOfIronfeat.WithPrerequisite(sheet => sheet.HasFeat(AllFeats.GetFeatByFeatName(FeatName.BespellWeapon)), "Bespell Weapon");
            return ArcanaOfIronfeat;
        }
        public static TrueFeat GenerateIntimidatingSpell()
        {
            TrueFeat IntimidatingSpellFeat = new TrueFeat(intimidatingSpellName, 6, "The devastation wrought by your large-scale spells is particularly terrifying.", "If the next action you use is to Cast a Spell that deals damage in an area, any target who fails their saving throw is also frightened 1 (or frightened 2 on a critical failure).", [Trait.Archetype, Trait.Concentrate, Trait.Emotion, Trait.Mental, Trait.Metamagic], null);
              IntimidatingSpellFeat.WithOnCreature(creature =>  {
                QEffect qf = new QEffect("Intimidating Spell", "Frighten enemies with your spells.");
                MetamagicProvider metaMagic = new MetamagicProvider("Intimidating Spell", spell =>
                {
                    if (spell.Target.IsAreaTarget && spell.ActionCost < 3 && spell.Description.ToLower().Contains("deal") && spell.Description.ToLower().Contains("damage")) {
                        CombatAction modifiedSpell = Spell.DuplicateSpell(spell).CombatActionSpell;
                        modifiedSpell.Name = $"Intimidating {modifiedSpell.Name}";
                        CommonSpellEffects.IncreaseActionCostByOne(modifiedSpell);
                        
                        modifiedSpell.WithEffectOnEachTarget(async (combatAction, attacker, target, checkResult) =>
                        {
                            if (checkResult == CheckResult.Failure)
                            {
                                target.AddQEffect(QEffect.Frightened(1));
                            }
                            else if (checkResult == CheckResult.CriticalFailure)
                            {
                                target.AddQEffect(QEffect.Frightened(2));
                            }
                        });
                        return modifiedSpell;
                    }
                    return null;
                });
                qf.MetamagicProvider = metaMagic;
                creature.AddQEffect(qf);  
            });
            IntimidatingSpellFeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);
            return IntimidatingSpellFeat;
        }
        public static TrueFeat GenerateShieldingFormation()
        {
            TrueFeat ShieldingFormationFeat = new TrueFeat(shieldingFormationFeatName, 8, "You have mastered unique magical techniques designed to protect your allies from harm. ", "You gain the {link:shieldingFormation}Shielding Formation{/link} focus spell.", [Trait.Archetype], null);
            ShieldingFormationFeat.WithOnSheet(sheet =>
            {
                sheet.AddFocusSpellAndFocusPoint(Trait.Wizard, Ability.Intelligence, WarMageSpells.shieldingFormationId);
            });
            ShieldingFormationFeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);
            return ShieldingFormationFeat;
        }

        public static TrueFeat GenerateDrainBondedItem()
        {
            TrueFeat DrainBondedItemFeat = new TrueFeat(drainBondedItemName, 8, "You have learned to harness the magic of your less martially inclined peers.", "You gain the arcane bond class feature and the Drain Bonded Item action.", [Trait.Archetype], null);
            DrainBondedItemFeat.WithAvailableAsArchetypeFeat(WarMage.warMageTrait);
            return DrainBondedItemFeat;
        }
    }
}
