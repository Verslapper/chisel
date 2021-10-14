using Chisel.Model.Enums;
using Chisel.Model.Models;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chisel
{
    class Program
    {
        static void Main(string[] args)
        {
            var username = ConfigurationManager.AppSettings["Username"] ?? "";

            var games = new List<Game>();
            // TODO: load in games from file

            // The Data Extraction Part
            var listingUrl = $"https://ballchasing.com/";
            if (!string.IsNullOrWhiteSpace(username))
            {
                listingUrl += $"?title=&player-name={username}";
            }
            var listingWeb = new HtmlWeb();
            var csv = new StringBuilder();
            var header = $"GameId,GameMode,IsRanked,BlueScore,OrangeScore,BlueWin,OrangeWin," +
                $"Blue1Name,Blue1Rank,Blue1MVP,Blue1Score,Blue1Goals,Blue1Assists,Blue1Saves,Blue1Shots,Blue1Win," +
                $"Blue2Name,Blue2Rank,Blue2MVP,Blue2Score,Blue2Goals,Blue2Assists,Blue2Saves,Blue2Shots,Blue2Win," +
                $"Blue3Name,Blue3Rank,Blue3MVP,Blue3Score,Blue3Goals,Blue3Assists,Blue3Saves,Blue3Shots,Blue3Win," +
                $"Blue4Name,Blue4Rank,Blue4MVP,Blue4Score,Blue4Goals,Blue4Assists,Blue4Saves,Blue4Shots,Blue4Win," +
                $"Orange1Name,Orange1Rank,Orange1MVP,Orange1Score,Orange1Goals,Orange1Assists,Orange1Saves,Orange1Shots,Orange1Win," +
                $"Orange2Name,Orange2Rank,Orange2MVP,Orange2Score,Orange2Goals,Orange2Assists,Orange2Saves,Orange2Shots,Orange2Win," +
                $"Orange3Name,Orange3Rank,Orange3MVP,Orange3Score,Orange3Goals,Orange3Assists,Orange3Saves,Orange3Shots,Orange3Win," +
                $"Orange4Name,Orange4Rank,Orange4MVP,Orange4Score,Orange4Goals,Orange4Assists,Orange4Saves,Orange4Shots,Orange4Win,Margin";
            
            // REVISIT: Looping just takes any old results, and loops at least one rotation longer than I expected, so it's out for now.
            //var loops = 0;
            //while (loops++ < 2 && !string.IsNullOrWhiteSpace(listingUrl))
            //{
            var listingDoc = listingWeb.Load(listingUrl).DocumentNode;
            var listingItems = listingDoc.QuerySelectorAll("ul.creplays li .replay-title");
            foreach (var listing in listingItems)
            {
                var gameId = listing.InnerText.Trim().Split(' ')[0];

                if (!games.Any(game => game.Id == gameId))
                {
                    Thread.Sleep(800);
                    var nextUrl = listing.QuerySelector(".replay-link").GetAttributeValue("href", string.Empty);
                    var url = $"https://ballchasing.com{nextUrl}";
                    var web = new HtmlWeb();
                    var doc = web.Load(url);

                    var title = doc.DocumentNode.SelectSingleNode("//h2").InnerText.Trim().Split(' ');
                    var document = doc.DocumentNode;

                    try
                    {
                        var teams = new List<Team>();
                        var teamScores = document.QuerySelectorAll("table.replay-stats thead h3");
                        foreach (var teamScore in teamScores)
                        {
                            teams.Add(new Team { Score = int.Parse(teamScore.InnerText.Trim().Split(' ')[0]) });
                        }

                        var blueScores = document.QuerySelectorAll("table.replay-stats tbody.blue tr");
                        teams.First().Players = GetPlayerStats(blueScores);

                        var orangeScores = document.QuerySelectorAll("table.replay-stats tbody.orange tr");
                        teams.Last().Players = GetPlayerStats(orangeScores);
                        
                        var uploader = username ?? title[1];
                        var result = title[title.Length - 1];
                        var uploaderTeam = new Team();
                        var winningTeam = new Team();

                        foreach (var team in teams)
                        {
                            foreach (var player in team.Players)
                            {
                                if (player.Name == uploader)
                                {
                                    uploaderTeam = team;
                                }
                            }
                        }

                        foreach (var team in teams)
                        {
                            var isTeamUploader = team == uploaderTeam;
                            var won = isTeamUploader ? (result == "Win" ? true : false) : !(result == "Win" ? true : false);
                            team.Win = won;
                            if (won)
                            {
                                winningTeam = team;
                                foreach (var player in team.Players)
                                {
                                    player.Win = won;
                                }
                            }
                        }
                        
                        winningTeam.Players.First().MVP = true;

                        var gameMode = GameMode.Other;
                        if (!Enum.TryParse(title[title.Length - 2], out gameMode))
                        {
                            if (title.Length == 3)
                            {
                                gameMode = GameMode.Tourney;
                            }
                            else
                            {
                                gameMode = GameMode.Other;
                            }
                        }

                        var margin = Math.Max(teams.First().Score - teams.Last().Score, teams.Last().Score - teams.First().Score);

                        games.Add(new Game
                        {
                            Id = gameId,
                            GameMode = gameMode,
                            Ranked = title[title.Length - 3] == "Ranked",
                            Teams = teams,
                            Margin = margin
                        });

                        Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: Added {listing.InnerText.Trim()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Skipping {gameId} because data is not querying cleanly. {ex.InnerException} {ex.Message}");
                    }
                }
            }

            // The I/O Part
            csv.AppendLine(header);

            foreach (var game in games)
            {
                var gameLine = $"{game.Id},{game.GameMode},{game.Ranked},{game.Teams.First().Score},{game.Teams.Last().Score},{game.Teams.First().Win},{game.Teams.Last().Win},";
                foreach (var team in game.Teams)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        // Fill commas for max players regardless of number of players in the game
                        if (team.Players.Count() <= i)
                        {
                            gameLine += $",,,,,,,,,";
                        }
                        else
                        {
                            var player = team.Players[i];
                            gameLine += $"{player.Name},{player.Rank},{player.MVP},{player.Score},{player.Goals},{player.Assists},{player.Saves},{player.Shots},{player.Win},";
                        }
                    }
                }
                gameLine += $"{game.Margin},";
                csv.AppendLine(gameLine);
            }

            // Detect next page
            var next = listingDoc.QuerySelector(".pagination-next");
            if (next != null && !string.IsNullOrWhiteSpace(next.GetAttributeValue("href", string.Empty)))
            {
                // TODO: FIX: This appears correct via debug, but the loop pulls in randos games. Why?
                listingUrl = $"https://ballchasing.com/" + next.GetAttributeValue("href", string.Empty);
            } 
            else
            {
                listingUrl = "";
            }
            //}

            File.WriteAllText($"C:\\chisel\\chisel-{Sanitise(username)}.csv", csv.ToString());

            // The Analysis Part
            // might have to do the summaries and insights here, I'm not sure I can work Excel magic on this ... unless I hardcode columns to the main user
            Console.WriteLine($"Games: {games.Count()}");
            var userResults = games.SelectMany(g => g.Teams).SelectMany(t => t.Players).Where(p => p.Name == username);
            Console.WriteLine($"Wins: {userResults.Count(r => r.Win)}");
            Console.WriteLine($"MVPs: {userResults.Count(r => r.MVP)}");
            Console.WriteLine($"Goals: {userResults.Sum(r => r.Goals)}");
            Console.WriteLine($"Assists: {userResults.Sum(r => r.Assists)}");
            Console.WriteLine($"Saves: {userResults.Sum(r => r.Saves)}");
            Console.WriteLine($"Shots: {userResults.Sum(r => r.Shots)}");
            Console.WriteLine($"Win %: {userResults.Count(r => r.Win) * 100.0 / userResults.Count()}");
        }

        private static string Sanitise(string username)
        {
            return username.Replace("\\", "").Replace(".", "").Replace(" ", "");
        }

        private static List<Player> GetPlayerStats(IEnumerable<HtmlNode> boxScores)
        {
            var players = new List<Player>();
            foreach (var tr in boxScores)
            {
                try
                {
                    var player = new Player();
                    var tds = tr.QuerySelectorAll("td");
                    int i = 0;
                    // Table goes Rank|Name|Score|Goals|Assists|Saves|Shots
                    foreach (var td in tds)
                    {
                        switch (i)
                        {
                            case 0:
                                var img = td.QuerySelector("img");
                                if (img == null)
                                {
                                    player.Tier = Tier.Unranked;
                                    player.Rank = Rank.Unranked;
                                }
                                else
                                {
                                    var tierAndDivision = td.QuerySelector("img").GetAttributeValue("title", string.Empty);
                                    player.Tier = GetTier(tierAndDivision);
                                    player.Rank = GetRank(tierAndDivision);
                                }
                                break;
                            case 1:
                                player.Name = td.InnerText.Trim().Split('\n')[0];
                                break;
                            case 2:
                                player.Score = int.Parse(td.InnerText.Trim());
                                break;
                            case 3:
                                player.Goals = int.Parse(td.InnerText.Trim());
                                break;
                            case 4:
                                player.Assists = int.Parse(td.InnerText.Trim());
                                break;
                            case 5:
                                player.Saves = int.Parse(td.InnerText.Trim());
                                break;
                            case 6:
                                player.Shots = int.Parse(td.InnerText.Trim());
                                break;
                            default:
                                break;
                        }
                        i++;
                    }
                    if (!string.IsNullOrWhiteSpace(player.Name)) // skip tr.timeline-row
                    {
                        players.Add(player);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException);
                }
            }

            return players;
        }

        private static Tier GetTier(string tierAndDivision)
        {
            if (tierAndDivision.Length == 0)
            {
                return Tier.Unranked;
            }

            // strip division
            var divisionStart = tierAndDivision.IndexOf("Division");
            var rank = tierAndDivision.Substring(0, divisionStart - 1).Trim();
            var spacelessRank = rank.Replace(" ", "");
            return (Tier)Enum.Parse(typeof(Tier), spacelessRank);
        }

        private static Rank GetRank(string tierAndDivision)
        {
            if (tierAndDivision.Length == 0)
            {
                return Rank.Unranked;
            }

            var spacelessRank = tierAndDivision.Replace("Division", "").Replace(" ", "");
            return (Rank)Enum.Parse(typeof(Rank), spacelessRank);
        }
    }
}
