﻿// <copyright file="CharacterManager.cs" company="Yemmlie - 252afh fork">
// Copyright policies set by https://github.com/yemmlie
// </copyright>

namespace CrusaderKingsStoryGen.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using CrusaderKingsStoryGen.Helpers;
    using CrusaderKingsStoryGen.Interfaces;
    using CrusaderKingsStoryGen.Parsers;
    using CrusaderKingsStoryGen.ScriptHelpers;

    internal class CharacterManager : ISerializeXml
    {
        public static CharacterManager instance = new CharacterManager();
        public List<CharacterParser> Characters = new List<CharacterParser>();
        public List<CharacterParser> AliveCharacters = new List<CharacterParser>();

        public List<CharacterParser> AddedSinceLastPrune = new List<CharacterParser>();
        public Dictionary<int, CharacterParser> CharacterMap = new Dictionary<int, CharacterParser>();
        private bool bInit;

        public void Init()
        {
            this.bInit = true;
            this.CharactersScript = new Script();
            this.CharactersScript.Name = Globals.ModDir + "history\\characters\\characters.txt";
            this.CharactersScript.Root = new ScriptScope();
            this.CharactersScript.Root.AllowDuplicates = false;
            this.CharactersScript.Root.Clear();

            foreach (var child in this.CharactersScript.Root.Children)
            {
                if (child is ScriptScope)
                {
                    this.Unpicked.Add((ScriptScope)child);
                }
            }

            //     CharactersScript.Save();
        }

        public void Save()
        {
            for (int index = 0; index < this.Characters.Count; index++)
            {
                var characterParser = this.Characters[index];
                //   characterParser.UpdateCultural();

                characterParser.UpdateCultural();

                if (characterParser.PrimaryTitle != null && characterParser.PrimaryTitle.Rank > 1)
                {
                    characterParser.AddRandomTraits();
                }

                if (characterParser.PrimaryTitle != null)
                {
                    characterParser.YearOfDeath += 1000;
                }
            }

            this.DoPurge();

            for (int index = 0; index < this.Characters.Count; index++)
            {
                var characterParser = this.Characters[index];
                //   characterParser.UpdateCultural();
                if (characterParser.YearOfDeath < characterParser.lastImportantYear)
                {
                    characterParser.YearOfDeath = characterParser.lastImportantYear;
                }

                if (characterParser.lastImportantYear > characterParser.YearOfDeath)
                {
                    characterParser.YearOfDeath = characterParser.lastImportantYear;
                }

                CharacterManager.instance.SetAllDates(characterParser.YearOfBirth, characterParser.YearOfDeath, characterParser.Scope, characterParser.Titles.Count > 0);
                this.CharactersScript.Root.SetChild(characterParser.Scope);
            }

            /*
            List<CharacterParser> withoutFamilies = new List<CharacterParser>(Characters);

            foreach (var characterParser in withoutFamilies)
            {
                if (characterParser.PrimaryTitle == null)
                    continue;
                if(characterParser.PrimaryTitle.Rank == 0)
                    continue;
                if (characterParser.PrimaryTitle.SubTitles.Count == 0 && characterParser.PrimaryTitle.Owns.Count == 0)
                    continue;

                if (Rand.Next(3) == 0)
                {
                 //  characterParser.PrimaryTitle.AdjacentToTitle
                }

                if (characterParser.PrimaryTitle.Rank == 1)
                    characterParser.CreateFamily(0, 1);
                else if (characterParser.PrimaryTitle.Rank == 2)
                    characterParser.CreateFamily(0, 2);
                else if (characterParser.PrimaryTitle.Rank == 3)
                    characterParser.CreateFamily(0, 6);
                else if (characterParser.PrimaryTitle.Rank == 4)
                    characterParser.CreateFamily(0, 6);
                characterParser.UpdateCultural();
            }
            */
            //  CharacterManager.instance.CalculateAllDates();

            this.CharactersScript.Save();
        }

        public void DoPurge()
        {
            for (int index = 0; index < this.AddedSinceLastPrune.Count; index++)
            {
                var characterParser = this.AddedSinceLastPrune[index];
                //   characterParser.UpdateCultural();

                if (characterParser == null)
                {
                    this.Characters.RemoveAt(index);
                    continue;
                }

                if (characterParser.ID == 1014792)
                {
                }

                if (characterParser.HadTitle)
                {
                    continue;
                }

                int age = characterParser.DistanceBeforeImportantDate;
                if (age > 60)
                {
                    this.Characters.Remove(characterParser);
                    this.CharacterMap.Remove(characterParser.ID);
                    this.AliveCharacters.Remove(characterParser);
                    characterParser.Purged = true;
                    continue;
                }

                if (age > 60)
                {
                    if (characterParser.Kids.Count > 0 && (!characterParser.HadTitle) && (characterParser.Spouses[characterParser.Spouses.Count - 1].HadTitle))
                    {
                        this.Characters.Remove(characterParser);
                        this.CharacterMap.Remove(characterParser.ID);
                        this.AliveCharacters.Remove(characterParser);
                        characterParser.Purged = true;
                    }
                    else if (!characterParser.HadTitle && characterParser.Kids.Count > 0)
                    {
                        bool keep = false;
                        foreach (var parser in characterParser.Kids)
                        {
                            if (parser.HadTitle)
                            {
                                keep = true;
                            }

                            if (parser.Kids.Count > 0)
                            {
                                keep = true;
                            }
                        }

                        if (!keep)
                        {
                            this.Characters.Remove(characterParser);
                            this.CharacterMap.Remove(characterParser.ID);
                            this.AliveCharacters.Remove(characterParser);
                            characterParser.Purged = true;
                        }
                    }
                }
            }

            this.AddedSinceLastPrune.Clear();
            for (int index = 0; index < this.AliveCharacters.Count; index++)
            {
                var characterParser = this.AliveCharacters[index];
                if (characterParser == null)
                {
                    this.AliveCharacters.RemoveAt(index);
                    index--;
                    continue;
                }

                if (characterParser.Age > 100)
                {
                    SimulationManager.DoDeath(characterParser);
                    this.AliveCharacters.Remove(characterParser);
                    index--;
                }
            }
        }

        public void SetAllDates(int birth, int death, ScriptScope scope, bool bHasTitle)
        {
            foreach (var child in scope.Children)
            {
                if (child is ScriptCommand)
                {
                    ScriptCommand c = (ScriptCommand)child;
                    if (c.Name == "birth")
                    {
                        // if (c.Value.ToString().Split('.').Length == 3)
                        {
                            c.Value = birth + ".1.1";
                            scope.Name = c.Value.ToString();
                        }
                    }

                    if (c.Name == "death")
                    {
                        //  if (c.Value.ToString().Split('.').Length == 3)
                        {
                            c.Value = death + ".3.1";
                            scope.Name = c.Value.ToString();
                        }
                    }
                }

                if (child is ScriptScope)
                {
                    ScriptScope c = (ScriptScope)child;

                    this.SetAllDates(birth, death, c, bHasTitle);
                }
            }
        }

        public Script CharactersScript { get; set; }

        public List<ScriptScope> Unpicked = new List<ScriptScope>();

        public CharacterParser GetNewCharacter(bool adult = false)
        {
            if (!this.bInit)
            {
                this.Init();
            }

            var chr = new CharacterParser();

            this.Characters.Add(chr);
            this.AliveCharacters.Add(chr);
            this.AddedSinceLastPrune.Add(chr);
            this.CharacterMap[chr.ID] = chr;
            return chr;
        }

        public ScriptScope GetNewCreatedCharacter()
        {
            var scope = new ScriptScope();
            scope.Name = CharacterParser.IDMax.ToString();

            scope.Add("name", "Bob");
            scope.Add("culture", "norse");
            scope.Add("religion", "pagan");
            var born = scope.AddScope("1.1.1");

            var died = scope.AddScope("3.1.1");
            born.Add("birth", "1.1.1");
            died.Add("death", "3.1.1");
            return scope;
        }

        public CharacterParser CreateNewCharacter(Dynasty dynasty, bool bFemale, int dateOfBirth, string religion, string culture)
        {
            if (!this.bInit)
            {
                this.Init();
            }

            var chr = new CharacterParser();

            this.Characters.Add(chr);
            this.AliveCharacters.Add(chr);
            chr.YearOfBirth = dateOfBirth;
            if (dynasty == null)
            {
                dynasty = DynastyManager.instance.GetDynasty(CultureManager.instance.CultureMap[culture]);
            }

            chr.Dynasty = dynasty;
            chr.religion = religion;
            chr.isFemale = bFemale;
            chr.culture = culture;
            chr.YearOfDeath = SimulationManager.instance.Year + 1000;
            this.AddedSinceLastPrune.Add(chr);
            this.CharacterMap[chr.ID] = chr;
            chr.SetupExistingDynasty();
            chr.UpdateCultural();

            return chr;
        }

        public CharacterParser CreateNewCharacter(string culture, string religion, bool bFemale)
        {
            if (!this.bInit)
            {
                this.Init();
            }

            var chr = new CharacterParser();

            this.Characters.Add(chr);
            this.AliveCharacters.Add(chr);
            chr.YearOfBirth = SimulationManager.instance.Year - 16;
            chr.Dynasty = DynastyManager.instance.GetDynasty(CultureManager.instance.CultureMap[culture]);
            chr.religion = religion;
            chr.isFemale = bFemale;
            chr.culture = culture;
            chr.YearOfDeath = SimulationManager.instance.Year + 1000;

            this.AddedSinceLastPrune.Add(chr);
            this.CharacterMap[chr.ID] = chr;
            chr.SetupExistingDynasty();
            chr.UpdateCultural();
            return chr;
        }

        public CharacterParser CreateNewHistoricCharacter(Dynasty dynasty, bool bFemale, string religion, string culture, int dateOfBirth, int dateOfDeath = -1, bool adult = true)
        {
            if (!this.bInit)
            {
                this.Init();
            }

            var chr = new CharacterParser();

            this.Characters.Add(chr);
            this.AliveCharacters.Add(chr);
            chr.YearOfBirth = dateOfBirth;
            chr.isFemale = bFemale;
            chr.culture = culture;
            chr.religion = religion;
            if (dateOfDeath != -1)
            {
                chr.YearOfDeath = dateOfDeath;
            }
            else
            {
                chr.YearOfDeath = dateOfBirth + 150 + RandomIntHelper.Next(40);
                if (RandomIntHelper.Next(4) == 0)
                {
                    chr.YearOfDeath = dateOfBirth + 150 + RandomIntHelper.Next(80);
                }

                if (adult)
                {
                    chr.YearOfDeath = dateOfBirth + 150 + 16 + RandomIntHelper.Next(80 - 16);
                }
            }

            this.CharactersScript.Root.SetChild(chr.Scope);
            this.AddedSinceLastPrune.Add(chr);
            this.CharacterMap[chr.ID] = chr;
            chr.Dynasty = dynasty;
            chr.SetupExistingDynasty();
            chr.UpdateCultural();

            return chr;
        }

        public CharacterParser FindPreviousOwner(CharacterParser prevOwner, List<CharacterParser> prevOwners, int year)
        {
            List<CharacterParser> choices = new List<CharacterParser>();
            List<CharacterParser> choicesDyn = new List<CharacterParser>();
            if (prevOwner == null)
            {
                return null;
            }

            CultureParser cul = prevOwner.Culture;

            for (int index = 0; index < this.Characters.Count; index++)
            {
                var characterParser = this.Characters[index];
                if (characterParser.isFemale)
                {
                    continue;
                }

                if (prevOwners.Contains(characterParser))
                {
                    continue;
                }

                if (year <= characterParser.YearOfBirth)
                {
                    continue;
                }

                if (year >= characterParser.YearOfDeath)
                {
                    continue;
                }

                if (characterParser.Culture != cul)
                {
                    continue;
                }

                if (characterParser.Father == null)
                {
                    continue;
                }

                if (prevOwner != null)
                {
                    if (prevOwner == characterParser)
                    {
                        continue;
                    }
                }

                if (prevOwner.Dynasty != characterParser.Dynasty)
                {
                    choices.Add(characterParser);
                }
                else
                {
                    choicesDyn.Add(characterParser);
                }
            }

            if (RandomIntHelper.Next(6) != 0 && choicesDyn.Count > 0)
            {
                return choicesDyn[RandomIntHelper.Next(choicesDyn.Count)];
            }

            if (choices.Count > 0)
            {
                return choices[RandomIntHelper.Next(choices.Count)];
            }

            return null;
        }

        public CharacterParser FindUnlandedMan(int year, string religion, string culture)
        {
            List<CharacterParser> choices = new List<CharacterParser>();
            if (RandomIntHelper.Next(8) != 0)
            {
                return CharacterManager.instance.CreateNewCharacter(null, false, SimulationManager.instance.Year - 16, religion, culture);
            }

            for (int index = 0; index < this.AliveCharacters.Count; index++)
            {
                var characterParser = this.AliveCharacters[index];
                if (characterParser.isFemale)
                {
                    continue;
                }

                if (year < characterParser.YearOfBirth + 16)
                {
                    continue;
                }

                if (year > characterParser.YearOfBirth + 65)
                {
                    continue;
                }

                if (year > characterParser.YearOfDeath)
                {
                    continue;
                }

                if (characterParser.Father == null)
                {
                    continue;
                }

                if (characterParser.religion != religion)
                {
                    continue;
                }

                if (characterParser.culture != culture)
                {
                    continue;
                }

                if (characterParser.PrimaryTitle != null)
                {
                    continue;
                }

                if (characterParser.Titles.Count > 0)
                {
                    continue;
                }

                choices.Add(characterParser);
                if (choices.Count > 30)
                {
                    break;
                }
            }

            if (choices.Count > 0)
            {
                return choices[RandomIntHelper.Next(choices.Count)];
            }

            return null;
        }

        public CharacterParser FindUnmarriedChildbearingAgeWomen(int year, string religion, string culture)
        {
            List<CharacterParser> choices = new List<CharacterParser>();
            if (RandomIntHelper.Next(2) != 0)
            {
                return CharacterManager.instance.CreateNewCharacter(null, true, SimulationManager.instance.Year - 16, religion, culture);
            }

            for (int index = 0; index < this.AliveCharacters.Count; index++)
            {
                var characterParser = this.AliveCharacters[index];
                if (!characterParser.isFemale)
                {
                    continue;
                }

                if (year < characterParser.YearOfBirth + 16)
                {
                    continue;
                }

                if (year > characterParser.YearOfBirth + 35)
                {
                    continue;
                }

                if (characterParser.Spouses.Count > 0)
                {
                    continue;
                }

                if (characterParser.Father == null)
                {
                    continue;
                }

                if (characterParser.religion != religion)
                {
                    continue;
                }

                choices.Add(characterParser);
                if (choices.Count > 30)
                {
                    break;
                }
            }

            if (choices.Count > 0)
            {
                return choices[RandomIntHelper.Next(choices.Count)];
            }

            return null;
        }

        public void RemoveCharacter(CharacterParser characterParser)
        {
            this.Characters.Remove(characterParser);
            this.CharacterMap.Remove(characterParser.ID);
            this.CharactersScript.Root.Remove(characterParser.Scope);
        }

        public void CalculateAllDates()
        {
            for (int index = 0; index < this.Characters.Count; index++)
            {
                var characterParser = this.Characters[index];
                if (characterParser.Titles.Count > 0)
                {
                    if (characterParser.YearOfDeath < characterParser.lastImportantYear)
                    {
                        characterParser.YearOfDeath = characterParser.lastImportantYear;
                    }

                    this.SetAllDates(characterParser.YearOfBirth, characterParser.YearOfDeath, characterParser.Scope, true);
                }
            }
        }

        public void SaveProject(XmlWriter writer)
        {
            writer.WriteStartElement("characters");
            foreach (var characterParser in this.Characters)
            {
                writer.WriteStartElement("chr");
                writer.WriteElementString("id", characterParser.ID.ToString());
                writer.WriteElementString("name", characterParser.ChrName);
                if (characterParser.Father != null)
                {
                    writer.WriteElementString("father", characterParser.Father.ID.ToString());
                }

                if (characterParser.Mother != null)
                {
                    writer.WriteElementString("mother", characterParser.Mother.ID.ToString());
                }

                writer.WriteElementString("born", characterParser.YearOfBirth.ToString());
                writer.WriteElementString("died", characterParser.YearOfDeath.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public void LoadProject(XmlReader node)
        {
        }

        public void LoadVanilla(int endDate)
        {
            var files = ModManager.instance.GetFileKeys("history\\characters");
            foreach (var file in files)
            {
                Script s = ScriptLoader.instance.LoadKey(file);
                foreach (var rootChild in s.Root.Children)
                {
                    var scope = rootChild as ScriptScope;

                    CharacterParser chr = new CharacterParser(scope);

                    chr.ID = Convert.ToInt32(scope.Name);
                    chr.ChrName = scope.GetString("name");
                    if (chr.ID == 144999)
                    {
                    }

                    if (chr.CalculateAgeFromScope(endDate))
                    {
                        if (chr.YearOfDeath >= SimulationManager.instance.Year)
                        {
                            this.AliveCharacters.Add(chr);
                        }

                        this.Characters.Add(chr);
                        this.CharacterMap[chr.ID] = chr;
                    }
                }
            }

            foreach (var characterParser in this.Characters)
            {
                characterParser.FixupFromScope(endDate);
            }
        }
    }
}