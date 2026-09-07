using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RND = UnityEngine.Random;

namespace VoidTemplate.MenuTinkery
{
    public class LeaderTableMenu : Menu.Menu, SelectOneButton.SelectOneButtonOwner
    {
        public RoundedRect speedrunRect;

        public DisablableButton[] buttons;

        public SimpleButton buttonRules;

        public Dictionary<SlugcatStats.Name, object[][,]> cashedTableData;

        public string playerTestName = "TestPlayer";

        public SlugcatSelect slugcatSelect;

        public LeaderTable table;

        public bool scoreRunMode;

        public int percentIndex;

        public bool lastPauseButton;

        private bool exitRequest;

        public LeaderTableMenu(ProcessManager manager) : base(manager, VoidEnums.ProcessID.LeaderTableMenu)
        {
            pages.Add(new(this, null, "main", 0));
            backObject = new SimpleButton(this, pages[0], Translate("BACK"), "BACK", new(50f, 728f), new(110f, 30f));
            pages[0].subObjects.Add(backObject);
            buttons = new DisablableButton[4];
            buttons[0] = new(this, pages[0], "SPEEDRUN", "VIEWMODE", new(10, 668), new(668, 40), buttons, 0);
            pages[0].subObjects.Add(buttons[0]);
            buttons[1] = new(this, pages[0], "SCORE RUN", "VIEWMODE", new(688, 668), new(668, 40), buttons, 1);
            pages[0].subObjects.Add(buttons[1]);
            buttons[2] = new(this, pages[0], "VOIDSEA %", "PERCENTRUN", new(10, 618), new(668, 40), buttons, 0);
            pages[0].subObjects.Add(buttons[2]);
            buttons[3] = new(this, pages[0], "ANY %", "PERCENTRUN", new(688, 618), new(668, 40), buttons, 1);
            pages[0].subObjects.Add(buttons[3]);
            buttonRules = new(this, pages[0], "RULES", "RULES", new(1206, 728), new(110f, 30f));
            pages[0].subObjects.Add(buttonRules);

            speedrunRect = new(this, pages[0], new(10, 40), new(1346, 568), true);
            pages[0].subObjects.Add(speedrunRect);
            slugcatSelect = new(this, speedrunRect, new(10, speedrunRect.size.y - 100), speedrunRect.size.x - 20);
            speedrunRect.subObjects.Add(slugcatSelect);
            cashedTableData = GenerateTestData(slugcatSelect.slugcats);
            LeaderTableMenuSyncHooks.SeedCachedTables(this);
            table = new(this, speedrunRect, new(10, 70), new(speedrunRect.size.x - 20, slugcatSelect.pos.y - 160), false);
            speedrunRect.subObjects.Add(table);
            table.LoadSlugcatTable(cashedTableData[slugcatSelect.CurrentSlug][0]);
        }

        public static Dictionary<SlugcatStats.Name, object[][,]> GenerateTestData(SlugcatStats.Name[] names)
        {
            var res = new Dictionary<SlugcatStats.Name, object[][,]>();
            for (int i = 0; i < names.Length; i++)
            {
                var m = new object[2][,];
                res.Add(names[i], m);
                var state = RND.state;
                RND.InitState(names[i].Index);
                for (int percentRun = 0; percentRun < 2; percentRun++)
                {
                    m[percentRun] = new object[7, RND.Range(50, 151)];

                    int playerIndex = RND.Range(0, m[percentRun].GetLength(1));

                    for (int j = 0; j < m[percentRun].GetLength(1); j++)
                    {
                        m[percentRun][0, j] = j + names[i].Index;
                        m[percentRun][1, j] = j == playerIndex ? "TestPlayer" : "User" + (j + names[i].Index).ToString();
                        m[percentRun][2, j] = new TimeSpan(percentRun * RND.Range(0, 30) + j + names[i].Index);
                    }
                }
                RND.state = state;
            }
            return res;
        }

        public int GetCurrentlySelectedOfSeries(string series)
        {
            if (series == "VIEWMODE")
            {
                return scoreRunMode ? 1 : 0;
            }
            return percentIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            if (series == "VIEWMODE")
            {
                scoreRunMode = to == 1;
                table.SwapTimeAndScore(scoreRunMode);
            }
            else
            {
                percentIndex = to;
                table.LoadSlugcatTable(cashedTableData[slugcatSelect.CurrentSlug][to]);
            }
        }

        public override void Init()
        {
            base.Init();
            selectedObject = slugcatSelect.slugcatButtons[0];
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message == "BACK")
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
                exitRequest = true;
            }
        }

        public override void Update()
        {
            base.Update();
            bool pauseExit = RWInput.CheckPauseButton(0);
            if (pauseExit && !exitRequest && !lastPauseButton && manager.dialog == null)
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
                exitRequest = true;
            }
            lastPauseButton = pauseExit;
        }

        public class DisablableButton(Menu.Menu menu, MenuObject owner, string displayText, string signalText, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex) : SelectOneButton(menu, owner, displayText, signalText, pos, size, buttonArray, buttonArrayIndex)
        {
            public override void Update()
            {
                buttonBehav.greyedOut = AmISelected;
                base.Update();
            }
        }

        public class SlugcatSelect : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
        {
            public readonly SlugcatStats.Name[] slugcats;

            public new LeaderTableMenu menu;

            public BigArrowButton nextButton;

            public BigArrowButton prevButton;

            public readonly SlugcatButton[] slugcatButtons;

            private (SlugcatStats.Name slugcat, int index) selectedSlugButton;

            public SlugcatStats.Name CurrentSlug => selectedSlugButton.slugcat;

            public SlugcatSelect(LeaderTableMenu menu, MenuObject owner, Vector2 pos, float lineX) : base(menu, owner, pos)
            {
                this.menu = menu;
                slugcats =
                [
                    SlugcatStats.Name.Yellow,
                    SlugcatStats.Name.White,
                    SlugcatStats.Name.Red,
                    VoidEnums.SlugcatID.Void,
                    VoidEnums.SlugcatID.Viy,
                    MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                    MoreSlugcatsEnums.SlugcatStatsName.Spear,
                    MoreSlugcatsEnums.SlugcatStatsName.Saint
                ];
                slugcats = [.. slugcats.Union(SlugcatStats.Name.values.entries.Select((s) => new SlugcatStats.Name(s)).Where((s) => !SlugcatStats.HiddenOrUnplayableSlugcat(s)))];
                selectedSlugButton.slugcat = slugcats[0];
                float sizeX = lineX;
                prevButton = new(menu, this, "SLUGCAT_SELECT_PREV", new(), -1);
                prevButton.pos.y -= prevButton.size.y / 2;
                subObjects.Add(prevButton);
                sizeX -= prevButton.size.x;
                nextButton = new(menu, this, "SLUGCAT_SELECT_NEXT", new(lineX, 0), 1);
                nextButton.pos -= new Vector2(nextButton.size.x, nextButton.size.y / 2);
                subObjects.Add(nextButton);
                sizeX -= nextButton.size.x;
                if (slugcats.Length <= 6)
                {
                    prevButton.buttonBehav.greyedOut = true;
                    nextButton.buttonBehav.greyedOut = true;
                }
                slugcatButtons = new SlugcatButton[Mathf.Min(6, slugcats.Length)];
                for (int i = 0; i < slugcatButtons.Length; i++)
                {
                    slugcatButtons[i] = new(menu, this, slugcats[i], new(Mathf.Lerp(0, sizeX, (float)i / slugcatButtons.Length) + 100, 0), new(100, 100), slugcatButtons, i);
                    subObjects.Add(slugcatButtons[i]); ;
                }
            }

            public SlugcatStats.Name NextSlug(SlugcatStats.Name current)
            {
                int nextInd = slugcats.IndexOf(current) + 1;
                if (nextInd > slugcats.Length - 1)
                {
                    return slugcats[0];
                }
                return slugcats[nextInd];
            }

            public SlugcatStats.Name PrevSlug(SlugcatStats.Name current)
            {
                int prevInd = slugcats.IndexOf(current) - 1;
                if (prevInd < 0)
                {
                    return slugcats[slugcats.Length - 1];
                }
                return slugcats[prevInd];
            }

            public void MoveNext()
            {
                if (selectedSlugButton.index + 1 > slugcatButtons.Length - 1)
                    selectedSlugButton.index = -1;

                for (int i = 1; i < slugcatButtons.Length; i++)
                {
                    slugcatButtons[i - 1].UpdateNewSlug(slugcatButtons[i].currentSlug);
                    if (i == slugcatButtons.Length - 1)
                    {
                        slugcatButtons[i].UpdateNewSlug(NextSlug(slugcatButtons[i].currentSlug));
                    }
                }
            }

            public void MovePrev()
            {
                if (selectedSlugButton.index - 1 < 0)
                    selectedSlugButton.index = -1;

                for (int i = slugcatButtons.Length - 2; i >= 0; i--)
                {
                    slugcatButtons[i + 1].UpdateNewSlug(slugcatButtons[i].currentSlug);
                    if (i == 0)
                    {
                        slugcatButtons[0].UpdateNewSlug(PrevSlug(slugcatButtons[0].currentSlug));
                    }
                }
            }

            public static string GetPortraitFileName(SlugcatStats.Name name)
            {
                if (name == SlugcatStats.Name.White)
                {
                    return "multiplayerportrait01";
                }
                else if (name == SlugcatStats.Name.Yellow)
                {
                    return "multiplayerportrait11";
                }
                else if (name == SlugcatStats.Name.Red)
                {
                    return "multiplayerportrait21";
                }
                return "multiplayerportrait41-" + name.ToString();
            }

            public override void Singal(MenuObject sender, string message)
            {
                if (message == "SLUGCAT_SELECT_NEXT")
                {
                    MoveNext();
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
                else if (message == "SLUGCAT_SELECT_PREV")
                {
                    MovePrev();
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }

            public int GetCurrentlySelectedOfSeries(string series)
            {
                if (selectedSlugButton.slugcat.ToString() == series)
                {
                    return selectedSlugButton.index;
                }

                return -1;
            }

            public void SetCurrentlySelectedOfSeries(string series, int to)
            {
                selectedSlugButton.slugcat = new(series);
                selectedSlugButton.index = to;
                menu.table.LoadSlugcatTable(menu.cashedTableData[selectedSlugButton.slugcat][menu.percentIndex]);
            }

            public class SlugcatButton : DisablableButton
            {
                public SlugcatStats.Name currentSlug;

                public MenuIllustration slugcatPortrait;

                public MenuLabel name;

                public SlugcatSelect Select => owner as SlugcatSelect;

                public SlugcatButton(Menu.Menu menu, SlugcatSelect owner, SlugcatStats.Name slugClass, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex) : base(menu, owner, "", slugClass.ToString(), pos, size, buttonArray, buttonArrayIndex)
                {
                    currentSlug = slugClass;
                    this.pos.y -= 35;
                    slugcatPortrait = new(menu, this, "", GetPortraitFileName(currentSlug), size / 2, true, true);
                    subObjects.Add(slugcatPortrait);
                    name = new(menu, this, menu.Translate(SlugcatStats.getSlugcatName(currentSlug)), new(), new(100, 30), false);
                    name.pos.y -= 30;
                    subObjects.Add(name);
                }

                public void UpdateNewSlug(SlugcatStats.Name newSlug)
                {
                    currentSlug = newSlug;
                    if (newSlug == Select.selectedSlugButton.slugcat)
                    {
                        Select.selectedSlugButton.index = buttonArrayIndex;
                    }
                    signalText = newSlug.ToString();
                    slugcatPortrait.fileName = GetPortraitFileName(newSlug);
                    slugcatPortrait.LoadFile();
                    slugcatPortrait.sprite.SetElementByName(slugcatPortrait.fileName);
                    name.label.text = menu.Translate(SlugcatStats.getSlugcatName(newSlug));
                }
            }
        }

        public class LeaderTable : RoundedRect
        {
            public new LeaderTableMenu menu;

            readonly TableCell[,] cells;

            public TableSwitch tSwitch;

            private bool timeAndScoreSwapped;

            public object[][,] currentData;

            public string[] playerInfo;

            public LeaderTable(LeaderTableMenu menu, MenuObject owner, Vector2 pos, Vector2 size, bool filled) : base(menu, owner, pos, size, filled)
            {
                this.menu = menu;
                cells = new TableCell[7, 13];
                var lerpSize = LerpCellSize(1, 12);
                GetCell(Cols.Places, 0) = new(menu, this, new(0, LerpCellSizeY(1)), lerpSize, data: "Место");
                GetCell(Cols.Usernames, 0) = new(menu, this, LerpCellSize(1, 1), lerpSize, leftConnection: true, data: "Ник в стиме");
                GetCell(Cols.Times, 0) = new(menu, this, LerpCellSize(2, 1), lerpSize, leftConnection: true, data: "Время");
                GetCell(Cols.Scores, 0) = new(menu, this, LerpCellSize(3, 1), lerpSize, leftConnection: true, data: "Очки");
                GetCell(Cols.Cycles, 0) = new(menu, this, LerpCellSize(4, 1), lerpSize, leftConnection: true, data: "Циклы");
                GetCell(Cols.Deaths, 0) = new(menu, this, LerpCellSize(5, 1), lerpSize, leftConnection: true, data: "Смерти");
                GetCell(Cols.Quits, 0) = new(menu, this, LerpCellSize(6, 1), lerpSize, leftConnection: true, data: "Прерванные попытки");
                for (int i = 0; i < cells.GetLength(0); i++)
                {
                    subObjects.Add(cells[i, 0]);
                }

                for (int row = 1; row < cells.GetLength(1); row++)
                {
                    bool playerRow = row == cells.GetLength(1) - 1;
                    for (int col = 0; col < cells.GetLength(0); col++)
                    {
                        cells[col, row] = new(menu, this, LerpCellSize(col, row + 1), lerpSize, true, col > 0, playerRow ? "—" : "");
                        subObjects.Add(cells[col, row]);
                    }
                }
                GetCell(Cols.Places, 11).UpdateData(". . .");
            }

            public void Reset()
            {
                for (int row = 1; row < cells.GetLength(1); row++)
                {
                    if (row == cells.GetLength(1) - 2) continue;
                    for (int col = 0; col < cells.GetLength(0); col++)
                    {
                        if (row == cells.GetLength(1) - 1) cells[col, row].UpdateData("—");
                        else cells[col, row].Clear();
                    }
                }
            }

            protected ref TableCell GetCell(Cols col, int row)
            {
                if (timeAndScoreSwapped)
                {
                    if (col == Cols.Times)
                    {
                        col = Cols.Scores;
                    }
                    else if (col == Cols.Scores)
                    {
                        col = Cols.Times;
                    }
                }
                return ref cells[(int)col, row];
            }

            public void SwapTimeAndScore(bool scoreRun)
            {
                if (scoreRun != timeAndScoreSwapped)
                {
                    for (int row = 0; row < cells.GetLength(1); row++)
                    {
                        var cellTime = GetCell(Cols.Times, row);
                        var cellScore = GetCell(Cols.Scores, row);
                        (cellScore.cellData.text, cellTime.cellData.text) = (cellTime.cellData.text, cellScore.cellData.text);
                    }
                    timeAndScoreSwapped = scoreRun;
                }
            }

            public void LoadSlugcatTable(object[,] newSlugData)
            {
                playerInfo = null;
                int cellRowLenght = cells.GetLength(1) - 3;
                int newDataRowLenght = newSlugData.GetLength(1);
                bool lenghtsDevides = newDataRowLenght % cellRowLenght == 0;
                currentData = new object[Mathf.Max(1, Mathf.CeilToInt((float)newDataRowLenght / cellRowLenght))][,];
                int lastIndexY = 0;
                for (int number = 0; number < currentData.Length; number++)
                {
                    int newY = cellRowLenght;
                    if (newDataRowLenght == 0)
                    {
                        newY = 0;
                    }
                    else if (number == currentData.Length - 1 && !lenghtsDevides)
                    {
                        newY = newDataRowLenght % newY;
                    }
                    currentData[number] = new object[cells.GetLength(0), newY];
                    for (int y = 0; y < currentData[number].GetLength(1); y++, lastIndexY++)
                    {
                        bool playerRow = string.Equals(newSlugData[(int)Cols.Usernames, lastIndexY]?.ToString(), menu.playerTestName, StringComparison.Ordinal);
                        for (int x = 0; x < currentData[number].GetLength(0); x++)
                        {
                            currentData[number][x, y] = newSlugData[x, lastIndexY];
                            if (playerRow)
                            {
                                playerInfo ??= new string[7];
                                playerInfo[x] = newSlugData[x, lastIndexY]?.ToString();
                            }
                        }

                    }
                }
                if (playerInfo != null)
                {
                    for (int i = 0; i < cells.GetLength(0); i++)
                    {
                        GetCell((Cols)i, cells.GetLength(1) - 1).UpdateData(playerInfo[i]);
                    }
                }
                if (tSwitch != null)
                {
                    tSwitch.RemoveSprites();
                    RemoveSubObject(tSwitch);
                    tSwitch = null;
                }
                if (currentData.Length > 1)
                {
                    tSwitch = new(menu, this, new(size.x / 2, -40), size.x / 3.5f, currentData.Length);
                    subObjects.Add(tSwitch);
                }
                UpdateTableData(0);
            }

            public void UpdateTableData(int index)
            {
                var newData = currentData[index];
                for (int row = 1; row < cells.GetLength(1) - 2; row++)
                {
                    for (int col = 0; col < cells.GetLength(0); col++)
                    {
                        var cell = GetCell((Cols)col, row);
                        cell.Clear();
                        if (row - 1 < newData.GetLength(1))
                            cell.UpdateData(newData[col, row - 1]?.ToString());
                    }
                }
            }

            public float LerpCellSizeY(float t)
            {
                return Mathf.Lerp(size.y, 0, t / cells.GetLength(1));
            }

            public float LerpCellSizeX(float t)
            {
                return Mathf.Lerp(0, size.x, t / cells.GetLength(0));
            }

            public Vector2 LerpCellSize(int x, int y)
            {
                return new(LerpCellSizeX(x), LerpCellSizeY(y));
            }

            public class TableCell : RectangularMenuObject
            {
                public FSprite[] sprites;

                public MenuLabel cellData;

                public readonly bool upConnection;

                public readonly bool leftConnection;

                public TableCell(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool upConnection = false, bool leftConnection = false, string data = "") : base(menu, owner, pos, size)
                {
                    this.upConnection = upConnection;
                    this.leftConnection = leftConnection;
                    cellData = new(menu, this, data, new(), size, false);
                    subObjects.Add(cellData);
                    sprites = new FSprite[2];
                    if (upConnection)
                    {
                        sprites[0] = new("pixel");
                        sprites[0].SetAnchor(0, 0);
                        sprites[0].scaleY = 2f;
                        Container.AddChild(sprites[0]);
                    }
                    if (leftConnection)
                    {
                        sprites[1] = new("pixel");
                        sprites[1].SetAnchor(0, 0);
                        sprites[1].scaleX = 2f;
                        Container.AddChild(sprites[1]);
                    }
                }

                public void UpdateData(string data)
                {
                    cellData.text = data ?? "NULL";
                }

                public void Clear()
                {
                    cellData.text = "";
                }

                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        sprites[i]?.RemoveFromContainer();
                    }
                }

                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    var pos = DrawPos(timeStacker);
                    var size = DrawSize(timeStacker);
                    if (upConnection)
                    {
                        sprites[0].SetPosition(pos.x, pos.y + size.y);
                        sprites[0].scaleX = size.x;
                    }
                    if (leftConnection)
                    {
                        sprites[1].SetPosition(pos);
                        sprites[1].scaleY = size.y;
                    }
                }
            }

            public class TableSwitch : PositionedMenuObject
            {
                public LevelSelector.ScrollButton nextButton;

                public LevelSelector.ScrollButton prevButton;

                public MenuLabel[] numbers;

                public RoundedRect numberViewer;

                public LeaderTable Table => owner as LeaderTable;

                public int CurrentNumber { get; private set; } = 1;

                public int maxNumber;

                public float lineX;

                public int CurrentIndex => CurrentNumber - 1;

                public int EndNumbers => maxNumber - numbers.Length + 1;

                private bool extraMode = true;

                public TableSwitch(Menu.Menu menu, LeaderTable tableOwner, Vector2 pos, float lineX, int maxNumber) : base(menu, tableOwner, pos)
                {
                    this.lineX = lineX;
                    this.maxNumber = maxNumber;
                    numbers = new MenuLabel[Mathf.Min(9, maxNumber)];
                    extraMode = maxNumber > 9;
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        string text;
                        if (extraMode)
                        {
                            text = "...";
                            if (i < numbers.Length - 2)
                            {
                                text = (i + 1).ToString();
                            }
                            else if (i == numbers.Length - 1)
                            {
                                text = maxNumber.ToString();
                            }
                        }
                        else
                        {
                            text = (i + 1).ToString();
                        }
                        numbers[i] = new(menu, this, text, new(Mathf.Lerp(0, lineX, (float)i / numbers.Length) + 30, 0), new(30, 24), false);
                        subObjects.Add(numbers[i]);
                    }
                    prevButton = new(menu, this, "PREV", numbers[0].pos, -1);
                    prevButton.pos.x -= 34;
                    subObjects.Add(prevButton);
                    nextButton = new(menu, this, "NEXT", numbers[numbers.Length - 1].pos, 1);
                    nextButton.pos.x += 40;
                    subObjects.Add(nextButton);
                    this.pos.x = Table.size.x / 2 - (nextButton.pos.x + nextButton.size.x + prevButton.pos.x) / 2;

                    numberViewer = new(menu, this, numbers[0].pos, new(30, 24), false);
                    subObjects.Add(numberViewer);
                }

                public override void Singal(MenuObject sender, string message)
                {
                    if (message == "NEXT")
                    {
                        CurrentNumber++;

                        if (extraMode && CurrentIndex > numbers.Length / 2 && CurrentIndex < maxNumber - numbers.Length / 2)
                        {
                            numbers[1].text = "...";
                            numbers[numbers.Length / 2].text = CurrentNumber.ToString();
                            for (int i = numbers.Length / 2 - 1, j = 1; i >= 2; i--, j++)
                            {
                                numbers[i].text = (CurrentNumber - j).ToString();
                            }
                            for (int i = numbers.Length / 2 + 1, j = 1; i < numbers.Length - 2; i++, j++)
                            {
                                numbers[i].text = (CurrentNumber + j).ToString();
                            }
                            if (CurrentIndex == maxNumber - numbers.Length / 2 - 1)
                            {
                                numbers[numbers.Length - 2].text = (CurrentNumber + numbers.Length / 2 - 1).ToString();
                            }
                        }
                        Table.UpdateTableData(CurrentIndex);
                    }
                    else if (message == "PREV")
                    {
                        CurrentNumber--;

                        if (extraMode && CurrentIndex > numbers.Length / 2 - 1 && CurrentIndex < maxNumber - numbers.Length / 2)
                        {
                            numbers[numbers.Length - 2].text = "...";
                            numbers[numbers.Length / 2].text = CurrentNumber.ToString();
                            for (int i = numbers.Length / 2 - 1, j = 1; i >= 2; i--, j++)
                            {
                                numbers[i].text = (CurrentNumber - j).ToString();
                            }
                            for (int i = numbers.Length / 2 + 1, j = 1; i < numbers.Length - 2; i++, j++)
                            {
                                numbers[i].text = (CurrentNumber + j).ToString();
                            }
                            if (CurrentIndex == numbers.Length / 2)
                            {
                                numbers[1].text = (CurrentNumber - numbers.Length / 2 + 1).ToString();
                            }
                        }
                        Table.UpdateTableData(CurrentIndex);
                    }
                }

                public override void Update()
                {
                    base.Update();
                    nextButton.buttonBehav.greyedOut = CurrentNumber == maxNumber;
                    prevButton.buttonBehav.greyedOut = CurrentNumber == 1;
                    if (!extraMode || CurrentIndex <= numbers.Length / 2)
                    {
                        numberViewer.pos = numbers[CurrentIndex].pos;
                    }
                    else if (CurrentIndex <= maxNumber - numbers.Length / 2 - 1)
                    {
                        numberViewer.pos = numbers[numbers.Length / 2].pos;
                    }
                    else
                    {
                        numberViewer.pos = numbers[numbers.Length - 1 - (maxNumber - CurrentNumber)].pos;
                    }
                }
            }

            public enum Cols : byte
            {
                Places = 0,
                Usernames,
                Times,
                Scores,
                Cycles,
                Deaths,
                Quits
            }
        }
    }
}
