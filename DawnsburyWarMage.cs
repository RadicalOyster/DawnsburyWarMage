using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using DawnsburyWarMage;

namespace Dawnsbury.Mods.Ooster.Subclasses.WarMage
{
    public class DawnsburyWarMage
    {
        public static QEffectId warMagicDisablerId;
        public static QEffectId warMagicId;
        public static FeatName schoolSelectionFeatName;
        public static Feat warMageFeat;
        public static FeatName warMageFeatName;
        public static Trait warMageTrait;
        public static ModdedIllustration ShieldingFormationIllustration;

        [DawnsburyDaysModMainMethodAttribute]
        public static void LoadMod()
        {
            ShieldingFormationIllustration = new ModdedIllustration("WarMageAssets/shieldingformation.png");
            RegisterSpells();
            RegisterTraits();
            RegisterFeats();

            warMagicDisablerId = ModManager.RegisterEnumMember<QEffectId>("warMagicDisabler");
            warMagicDisablerId = ModManager.RegisterEnumMember<QEffectId>("warMagicId");
            WarMageFeats.dressingTagId = ModManager.RegisterEnumMember<QEffectId>("dressingTagId");

            SelectionOption? arcaneThesis = null;

            ClassSelectionFeat wizardBaseFeat = (ClassSelectionFeat)AllFeats.GetFeatByFeatName(FeatName.Wizard);

            wizardBaseFeat.WithOnSheet(sheet =>
            {
                arcaneThesis = sheet.SelectionOptions.Find(option => option.Name == "Arcane thesis");
                sheet.SelectionOptions.RemoveAll(option => option.Name == "Arcane thesis");
            });

            warMageFeat = GenerateSubclass();
            wizardBaseFeat.Subfeats!.Add(warMageFeat);

            wizardBaseFeat.Subfeats.ForEach(feat =>
            {
                feat.WithOnSheet(sheet =>
                {
                    if (!sheet.HasFeat(warMageFeat))
                    {
                        sheet.AddSelectionOption(new SingleFeatSelectionOption("arcaneThesis", "Arcane thesis", 1, feat => feat.HasTrait(Trait.ArcaneThesis)));
                    }
                });
            });

        }

        private static void RegisterSpells()
        {
            WarMageSpells.shieldingFormationId = ModManager.RegisterNewSpell("shieldingFormation", 1, (_, _, _, _, _) => WarMageSpells.GenerateShieldingFormationSpell());
        }
        private static void RegisterFeats()
        {
            WarMageFeats.warMageDedicationName = ModManager.RegisterFeatName("warMageDedication", "War Mage Dedication");
            WarMageFeats.magesFieldDressingName = ModManager.RegisterFeatName("magesFieldDressing", "Mage's Field Dressing");
            WarMageFeats.shieldReinforcementName = ModManager.RegisterFeatName("shieldSpellReinforcement", "Shield Spell Reinforcement");
            WarMageFeats.intimidatingSpellName = ModManager.RegisterFeatName("intimidatingSpell", "Intimidating Spell");
            WarMageFeats.shieldingFormationFeatName = ModManager.RegisterFeatName("shieldingFormation", "Shielding Formation");
            WarMageFeats.arcanaOfIronName = ModManager.RegisterFeatName("arcanaOfIron", "Arcana of Iron");
            WarMageFeats.drainBondedItemName = ModManager.RegisterFeatName("warMageDrainBondedItem", "Bonded Item");
            warMageFeatName = ModManager.RegisterFeatName("warMageClass", "War Mage");

            ModManager.AddFeat(GenerateSubclass());
            ModManager.AddFeat(WarMageFeats.GenerateDedicationFeat());
            ModManager.AddFeat(WarMageFeats.GenerateFieldDressingFeat());
            ModManager.AddFeat(WarMageFeats.GenerateShieldSpellReinforcement());
            ModManager.AddFeat(WarMageFeats.GenerateArcanaOfIron());
            ModManager.AddFeat(WarMageFeats.GenerateIntimidatingSpell());
            ModManager.AddFeat(WarMageFeats.GenerateShieldingFormation());
            ModManager.AddFeat(WarMageFeats.GenerateDrainBondedItem());
        }

        private static void RegisterTraits()
        {
            warMageTrait = ModManager.RegisterTrait("War Mage", new TraitProperties("War Mage", false));
        }
        private static Feat GenerateSubclass()
        {
            Feat evocationSchool = AllFeats.GetFeatByFeatName(FeatName.EvocationSchool);
            Feat? battleMagicSchool = AllFeats.GetFeatByFeatNameOrStringOptional(null, "BattleMagic");

            String description = $"{{b}}War Mage Adjustments:{{/b}} You become trained in light and medium armor. At 11th level, you gain expert proficiency with light and medium armor, as well as unarmored defense.\n\nYou gain the {{link:ShieldBlock}}Shield Block{{/link}} general feat at 1st level.\n\nYou Gain the War Magic class feature at 1st level.\n\nYou must choose the {{link:{evocationSchool.ToTechnicalName()}}}School of Evocation{{/link}} as your arcane school.\n\nYou become trained in all simple martial weapons. When your proficiency with simple weapons increases, your proficiency with martial weapons increases to the same level.\n\nYou do not gain the arcane bond or arcane thesis class features.\n\n{{b}}War Magic:{{/b}} as a free action once per round, you can exchange one of your prepared spells for True Strike until the end of the encounter.";

            if (battleMagicSchool != null)
            {
                description = $"{{b}}War Mage Adjustments:{{/b}} You become trained in light and medium armor. At 11th level, you gain expert proficiency with light and medium armor, as well as unarmored defense.\n\nYou gain the {{link:ShieldBlock}}Shield Block{{/link}} general feat at 1st level.\n\nYou Gain the War Magic class feature at 1st level.\n\nYou must choose the {{link:{battleMagicSchool.ToTechnicalName()}}}{battleMagicSchool.BaseName}{{/link}} as your arcane school.\n\nYou become trained in all simple martial weapons. When your proficiency with simple weapons increases, your proficiency with martial weapons increases to the same level.\n\nYou do not gain the arcane bond or arcane thesis class features.\n\n{{b}}War Magic:{{/b}} as a free action once per round, you can exchange one of your prepared spells for True Strike until the end of the encounter.";
            }
            
            Feat warMage = new Feat(warMageFeatName, "You learned the arcane craft at a war college, focusing less on theory and scholarship and more on applied magic and how best to leverage spellcraft on the battlefield. You were schooled in arcana, tried and tested in war, and learned how and when best to employ those spells to turn the tide of battle. Your training included lessons on swordplay and the upkeep of armor alongside the preparation of spells and the deciphering of arcane runes.", description, [], null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AdditionalClassTraits.Add(warMageTrait);
                sheet.NumberOfFeatsForDedication.TryAdd(warMageTrait, 0);
                sheet.IncreaseProficiency(1, Trait.Simple, Proficiency.Trained);
                sheet.IncreaseProficiency(1, Trait.Martial, Proficiency.Trained);
                sheet.IncreaseProficiency(11, Trait.Simple, Proficiency.Expert);
                sheet.IncreaseProficiency(11, Trait.Martial, Proficiency.Expert);
                sheet.IncreaseProficiency(1, Trait.LightArmor, Proficiency.Trained);
                sheet.IncreaseProficiency(1, Trait.MediumArmor, Proficiency.Trained);
                sheet.IncreaseProficiency(13, Trait.LightArmor, Proficiency.Expert);
                sheet.IncreaseProficiency(13, Trait.MediumArmor, Proficiency.Expert);
                sheet.AddFeat(AllFeats.GetFeatByFeatName(FeatName.ShieldBlock), null);

                if (battleMagicSchool == null)
                {
                    sheet.AddFeat(evocationSchool, null);
                } else
                {
                    sheet.AddFeat(battleMagicSchool, null);
                }

                    var freeArchetypeOption = sheet.SelectionOptions.Find(option => option.Name == "Free Archetype feat" && option.OptionLevel == 2);
                if (freeArchetypeOption != null)
                {
                    sheet.SelectionOptions.Remove(freeArchetypeOption);
                    sheet.AddAtLevel(2, sheet2 => sheet2.AddSelectionOption(new SingleFeatSelectionOption(freeArchetypeOption.Key, freeArchetypeOption.Name, 2, feat => feat.FeatName == WarMageFeats.warMageDedicationName)));
                } else
                {
                    SelectionOption firstWizardFeat = sheet.SelectionOptions.Find(option => option.Name == "Wizard feat" && option.OptionLevel == 2);
                    sheet.SelectionOptions.Remove(firstWizardFeat);
                    sheet.AddAtLevel(2, sheet2 => sheet2.AddSelectionOption(new SingleFeatSelectionOption("wizardFeat1", "Wizard feat", 2, feat => feat.FeatName == WarMageFeats.warMageDedicationName)));
                }

            }).WithOnCreature(creature =>
            {
                if (!creature.HasFeat(WarMageFeats.drainBondedItemName))
                {
                    creature.RemoveAllQEffects((QEffect effect) => effect.Name == "Arcane Bond");
                }
                creature.AddQEffect(new QEffect
                {
                    Name = "War Magic",
                    Description = "Once per round, swap a prepared spell for True Strike as a free action.",
                    Innate = true,
                    Id = warMagicId,
                    ProvideActionIntoPossibilitySection = (self, section) =>
                    {
                        if (section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers)
                        {
                            return GenerateWarMagicAction(creature);
                        }
                        return null;
                   }
                });
            }).WithIllustration(IllustrationName.BastardSword);
            return warMage;
        }

        public static ActionPossibility GenerateWarMagicAction(Creature owner)
        {
            CombatAction? originalSpell = null;
            CombatAction warMagicAction = new CombatAction(owner, IllustrationName.TrueStrike, "War Magic", [], ".", Target.Self()).WithActionCost(0).WithEffectOnSelf(async (action, self) =>
            {
                var spellSource = owner.Spellcasting?.GetSourceByOrigin(Trait.Wizard);
                var usedUpSource = owner.PersistentUsedUpResources.GetSpellcasting(Trait.Wizard);

                if (spellSource != null)
                {
                    var spellSlots = spellSource.Spells;
                    if (spellSlots != null)
                    {
                        var choice = await self.AskForChoiceAmongButtons(IllustrationName.TrueStrike, "Choose a spell to replace", [.. spellSlots.Select(item => item.Name + " (" + item.SpellLevel.ToString() + ")"), "Cancel"]);
                        if (choice.Text == "Cancel")
                        {
                            action.RevertRequested = true;
                            return;
                        }
                        else
                        {

                            var indexToReplace = spellSlots.FindIndex(spell => spell.Name + " (" + spell.SpellLevel.ToString() + ")" == choice.Text);
                            originalSpell = spellSlots[indexToReplace];
                            CombatAction trueStrike = AllSpells.AllByName["True Strike"].CombatActionSpell;

                            trueStrike.Owner = originalSpell.Owner;
                            trueStrike.SpellLevel = originalSpell.SpellLevel;
                            trueStrike.SpellcastingSource = originalSpell.SpellcastingSource;
                            trueStrike.RepresentsPreparedSpell = trueStrike;
                            trueStrike.SpellcastingSource = spellSource;
                            trueStrike.SpellcastingSource.Kind = SpellcastingKind.Prepared;
                            trueStrike.Traits.Add(Trait.Prepared);
                            spellSlots[indexToReplace] = trueStrike;

                            QEffect disableWarMagic = new QEffect("", "", ExpirationCondition.ExpiresAtEndOfAnyTurn, owner);
                            disableWarMagic.Id = warMagicDisablerId;
                            owner.AddQEffect(disableWarMagic);
                        }
                    }
                    {

                    }
                }
            });
            ActionPossibility output = new ActionPossibility(warMagicAction);
            if (owner.FindQEffect(warMagicDisablerId) == null)
            {
                return output;
            }
            else
            {
                return null;
            }
        }
    }
}