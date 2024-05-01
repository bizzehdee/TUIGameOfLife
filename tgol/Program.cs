using Spectre.Console;
using System.Text.Json;

namespace tgol
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var availableGames = new List<GameData>();
            int[,]? currentGameData;
            GameData? selectedGame;

            if (!Directory.Exists("data"))
            {
                AnsiConsole.WriteLine("Game data directory not found");
                return;
            }

            var jsonFiles = Directory.GetFiles("data", "*.json");

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var gameData = TryLoadingGameFile(jsonFile);
                    if (gameData != null)
                    {
                        availableGames.Add(gameData);
                    }
                }
                catch
                {
                    AnsiConsole.WriteLine($"{jsonFile} is an invalid game file");
                }
            }

            if (availableGames.Count == 0)
            {
                AnsiConsole.WriteLine("No valid game files found");
                return;
            }

            selectedGame = AnsiConsole.Prompt(
                new SelectionPrompt<GameData>()
                    .Title("Select a game:")
                    .PageSize(10)
                    .MoreChoicesText("Move up or down for more choices")
                    .AddChoices(
                        availableGames.ToArray()
                    ));

            AnsiConsole.WriteLine($"Selected {selectedGame.Name} ({selectedGame.Width} x {selectedGame.Height})");

            if (selectedGame.Width / 2 > Console.WindowWidth || selectedGame.Height > Console.WindowHeight)
            {
                var windowAnswer = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Game size is larger than window size. Do you want to change your window size:")
                        .AddChoices(
                            new[]
                            {
                                "Yes",
                                "No"
                            }
                        ));

                if (windowAnswer == "Yes")
                {
                    Console.SetWindowSize(selectedGame.Width * 2, selectedGame.Height);
                }
            }

            currentGameData = new int[selectedGame.Height, selectedGame.Width];

            var dataLength = Math.Min(selectedGame.Height * selectedGame.Width, selectedGame.Data.Length);

            for (int y = 0; y < selectedGame.Height; y++)
            {
                for (int x = 0; x < selectedGame.Width; x++)
                {
                    int i = (y * selectedGame.Width) + x;
                    if (i >= dataLength)
                    {
                        break;
                    }

                    currentGameData[y, x] = selectedGame.Data[i];
                }
            }

            var gameCanvas = new Canvas(selectedGame.Width, selectedGame.Height);

            await AnsiConsole.Live(gameCanvas)
                .AutoClear(true)
                .StartAsync(async ctx =>
                {
                    while (true)
                    {
                        UpdateUI(selectedGame.Width, selectedGame.Height, currentGameData, gameCanvas);
                        ctx.Refresh();

                        await Task.Delay(selectedGame.GameSpeedMS);

                        UpdateGame(selectedGame.Width, selectedGame.Height, currentGameData);

                    }
                });
        }

        private static void UpdateUI(int gameWidth, int gameHeight, int[,] gameData, Canvas canvas)
        {
            for (int y = 0; y < gameHeight; y++)
            {
                for (int x = 0; x < gameWidth; x++)
                {
                    var cell = gameData[y, x];
                    canvas.SetPixel(x, y, cell == 1 ? Color.White : Color.Black);
                }
            }
        }

        private static int GetCurrentState(int x, int y, int gameWidth, int gameHeight, int[,] gameData)
        {
            int realX = x; int realY = y;

            //do wrap-around for x
            if (x == -1)
            {
                realX = gameWidth - 1;
            }
            else if (x == gameWidth)
            {
                realX = 0;
            }

            //do wrap-around for y
            if (y == -1)
            {
                realY = gameHeight - 1;
            }
            else if (y == gameHeight)
            {
                realY = 0;
            }
            
            return gameData[realY, realX]; //we are within the game grid, so get whatever we need
        }

        public static int GetNeighbourCount(int x, int y, int gameWidth, int gameHeight, int[,] gameData)
        {
            int count = 0;

            for (int ny = y - 1; ny <= y + 1; ny++) //start 1 up and finish 1 below
            {
                for (int nx = x - 1; nx <= x + 1; nx++) //start 1 left and finish 1 right
                {
                    if (!(ny == y && nx == x))
                    {
                        if (GetCurrentState(nx, ny, gameWidth, gameHeight, gameData) == 1)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static void UpdateGame(int gameWidth, int gameHeight, int[,] gameData)
        {
            var nextGameData = new int[gameHeight, gameWidth];

            for (int y = 0; y < gameHeight; y++)
            {
                for (int x = 0; x < gameWidth; x++)
                {
                    int neighbourCount = GetNeighbourCount(x, y, gameWidth, gameHeight, gameData);
                    int currentState = GetCurrentState(x, y, gameWidth, gameHeight, gameData);

                    if (currentState == 1)
                    {
                        if (neighbourCount < 2)
                        {
                            nextGameData[y, x] = 0;
                        }
                        else if (neighbourCount == 2 || neighbourCount == 3)
                        {
                            nextGameData[y, x] = 1;
                        }
                        else if (neighbourCount > 3)
                        {
                            nextGameData[y, x] = 0;
                        }
                    }
                    else
                    {
                        if (neighbourCount == 3)
                        {
                            nextGameData[y, x] = 1;
                        }
                    }
                }
            }

            Array.Copy(nextGameData, gameData, nextGameData.Length);
        }

        private static GameData? TryLoadingGameFile(string jsonFile)
        {
            using (var fileStream = File.OpenRead(jsonFile))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var gameData = JsonSerializer.Deserialize<GameData>(fileStream, options);

                if (gameData == null)
                {
                    return null;
                }

                return gameData;
            }
        }
    }
}