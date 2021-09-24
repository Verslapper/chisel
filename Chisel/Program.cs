using Chisel.Model.Enums;
using Chisel.Model.Models;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chisel
{
    class Program
    {
        static void Main(string[] args)
        {
            // make HTTP request to URL
            var content = string.Empty;
            var url = "https://ballchasing.com/replay/30793a71-afd8-45a6-afb1-8ed41a357eea";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            // parse response
            var title = doc.DocumentNode.SelectSingleNode("//h2").InnerText.Trim().Split(' ');
            var titleElements = title.Length;

            // using Fizzler syntax
            var document = doc.DocumentNode;
            var boxScore = document.QuerySelectorAll("table.replay-stats");

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

            var winningTeam = teams.Last();
            if (teams.First().Score > teams.Last().Score)
            {
                winningTeam = teams.First();
            }
            winningTeam.Win = true;
            winningTeam.Players.First().MVP = true;

            var game = new Game
            {
                Id = title[0],
                Ranked = title[titleElements - 3] == "Ranked",
                GameMode = (GameMode)Enum.Parse(typeof(GameMode), title[titleElements - 2]),
                Teams = teams
            };
            // save parsed info

            // traverse listing page
            // detect duplicates
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
                                    player.Rank = Rank.Unranked;
                                }
                                else
                                {
                                    player.Rank = ConvertTitleToRank(td.QuerySelector("img").GetAttributeValue("title", string.Empty));
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

        private static Rank ConvertTitleToRank(string rankAndDivision)
        {
            if (rankAndDivision.Length == 0)
            {
                return Rank.Unranked;
            }

            // strip division
            var divisionStart = rankAndDivision.IndexOf("Division");
            var rank = rankAndDivision.Substring(0, divisionStart - 1).Trim();
            var spacelessRank = rank.Replace(" ", "");
            return (Rank)Enum.Parse(typeof(Rank), spacelessRank);
        }
    }
}
