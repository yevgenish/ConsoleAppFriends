// See https://aka.ms/new-console-template for more information
using System.Text;

Console.WriteLine("Starting.");

//Friends Edge source files by episodes - Download all  (179.66 kB)
//https://adelaide.figshare.com/articles/dataset/Friends_edge_lists/7413593?file=13720637

//init class
Friends friends = new Friends();
friends.JoinSeason();

public class Friends
{
    //template variables
    const string FOLDER_EPISODES = @"D:\Friends\Friends edges";
    const string EPISODE_EDGES_TEMPLATE_NAME = "edges_{0}{1}.csv"; //season, episode
    const string FOLDER_EXPORTED = @"D:\Friends\Friends edges\Friends season data";

    const string EXPORTED_EDGES_SEASON_NAME = "edges_s_{0}.csv"; //season
    const string EXPORTED_NODES_SEASON_NAME = "nodes_s_{0}.csv"; //season
    const string EXPORTED_NODES_EXT_SEASON_NAME = "nodes_ext_s_{0}.csv"; //season

    const string SOURCE_TARGET_DELIMETER = "^^^^^";

    const string SEASON_NUMBER_STRING_FORMAT = "D2";

    public class CharacterNode
    {
        public string Name { get; set; } = String.Empty;
        public int WeightWithSelf { get; set; }
        public int WeightWithoutSelf { get; set; }
    }

    //Join all seasons and episodes to 1 file for each season and file for all seasons.
    //extract nodes from edges data
    public void JoinSeason()
    {
        int season = 1;        
        bool allSeasonsCompleted = false;

        //variables for all seasons aggregations
        Dictionary<string, int>? all_seasons_edges = new Dictionary<string, int>();
        HashSet<string>? all_seasons_nodes = new HashSet<string>();
        Dictionary<string, CharacterNode>? all_seasons_nodes_ext = new Dictionary<string, CharacterNode>();

        while (!allSeasonsCompleted)
        {
            int episode = 1;
            bool episodeCompleted = false;

            //variables for each season aggregations
            Dictionary<string, int>? season_edges = new Dictionary<string, int>();
            HashSet<string>? season_nodes = new HashSet<string>();
            Dictionary<string, CharacterNode>? season_nodes_ext = new Dictionary<string, CharacterNode>();

            while (!episodeCompleted)
            {
                //constructing episode file location (s01e01)
                string episode_name = string.Format(EPISODE_EDGES_TEMPLATE_NAME,
                    season.ToString(SEASON_NUMBER_STRING_FORMAT), episode.ToString(SEASON_NUMBER_STRING_FORMAT));
                string episode_path = System.IO.Path.Combine(FOLDER_EPISODES, episode_name);
       
                if (File.Exists(episode_path))
                {
                    //reading line by line, where each line contains edge data
                    var fileData = File.ReadAllLines(episode_path);
                    for (int i = 0; i < fileData.Length; i++)
                    {
                        //skipping header line
                        if (i > 0)
                        {
                            var line = fileData[i];
                            var line_arr = line.Split(',');
                            var source = line_arr[0];
                            var target = line_arr[1];
                            var weight = Int32.Parse(line_arr[2]);

                            // to prevent monica-chandler vs chandler-monica appearances
                            // sorting by name asc
                            List<string> lst_dic_key = new List<string>() { source, target };
                            lst_dic_key.Sort();
                            var dic_key = lst_dic_key[0] + SOURCE_TARGET_DELIMETER + lst_dic_key[1];

                            //collectiong data for each season and all seasons files: nodes and edges
                            //each season - begin
                            AddNodeData(
                                season_edges,
                                season_nodes,
                                season_nodes_ext,
                                dic_key,
                                source,
                                target,
                                weight);
                            //each season - end

                            //all seasons - begin
                            AddNodeData(
                                all_seasons_edges,
                                all_seasons_nodes,
                                all_seasons_nodes_ext,
                                dic_key,
                                source,
                                target,
                                weight);   
                            //all seasons - end
                        }
                    }

                    episode++;
                }
                else
                {
                    episodeCompleted = true;
                    if(episode == 1)
                    {
                        allSeasonsCompleted = true;
                    }
                }                
            }

            if (!allSeasonsCompleted)
            {
                //save data for each specific season
                SaveSeasonData(season, season_nodes!, season_nodes_ext!, season_edges);
            }
            
            season++;
        }
        //save aggregated data for all seasons
        SaveSeasonData(-1, all_seasons_nodes!, all_seasons_nodes_ext!, all_seasons_edges);

        Console.WriteLine("Completed.");
    }

    private static void AddNodeData(Dictionary<string, int> edges,
        HashSet<string> nodes,
        Dictionary<string, CharacterNode> nodes_ext,
        string dic_key,
        string source,
        string target,
        int weight)
    {
        if (edges.ContainsKey(dic_key))
        {
            edges[dic_key] += weight;
        }
        else
        {
            edges.Add(dic_key, weight);
        }

        if (!nodes!.Contains(source))
        {
            nodes.Add(source);
        }

        bool is_self = source == target;

        CountAllWeights(nodes_ext, source, weight, is_self);

        //prevent counting twice for same character
        if (!is_self)
        {
            CountAllWeights(nodes_ext, target, weight, is_self);
        }
    }

    //aggregating weights for each character for nodes file - for internal purposes
    private static void CountAllWeights(Dictionary<string, CharacterNode> nodes, string character, int weight, bool is_self)
    {
        if (nodes.ContainsKey(character))
        {
            nodes[character].WeightWithSelf += weight;
            nodes[character].WeightWithoutSelf += is_self ? 0 : weight;
        }
        else
        {
            nodes.Add(character, new CharacterNode()
            {
                Name = character,
                WeightWithSelf = weight,
                WeightWithoutSelf = is_self ? 0 : weight
            });
        }
    }

    //Save to file
    private void SaveSeasonData(int seasonNum,
        HashSet<string> season_nodes,
        Dictionary<string, CharacterNode> season_nodes_ext,
        Dictionary<string, int> season_edges)
    {
        //nodes - begin
        var lst_season_nodes = season_nodes.ToList();
        lst_season_nodes.Sort(StringComparer.OrdinalIgnoreCase);

        var sb_season_nodes_ordered = new StringBuilder();
        lst_season_nodes.ForEach(el =>
        {
            //Id,Label
            var row = $"{el},{el}";
            sb_season_nodes_ordered.AppendLine(row);
        });        
        var str_season_nodes = "Id,Label" + Environment.NewLine + sb_season_nodes_ordered;
        //nodes - end

        //nodes ext - begin
        var season_nodes_ext_ordered = season_nodes_ext.OrderByDescending(el => el.Value.WeightWithSelf);
        var sb_season_nodes_ext_ordered = new StringBuilder();
        season_nodes_ext_ordered.ToList().ForEach(el =>
        {
            //Node,WeightWithSelf,WeightWithoutSelf
            var row = $"{el.Key},{el.Key},{el.Value.WeightWithSelf},{el.Value.WeightWithoutSelf}";
            sb_season_nodes_ext_ordered.AppendLine(row);
        });
        var str_season_nodes_ext = "Id,Label,WeightWithSelf,WeightWithoutSelf" +
            Environment.NewLine + sb_season_nodes_ext_ordered;
        //nodes ext - end

        //edges - begin
        var season_edges_ordered = season_edges.OrderByDescending(el => el.Value);
        var sb_season_edges_ordered = new StringBuilder();
        season_edges_ordered.ToList().ForEach(el =>
        {
            //Source,Target,Weight
            var keyArr = el.Key.Split(SOURCE_TARGET_DELIMETER);
            var row = $"{keyArr[0]},{keyArr[1]},{el.Value}";
            sb_season_edges_ordered.AppendLine(row);
        });
        var str_season_edges = "Source,Target,Weight" + Environment.NewLine + sb_season_edges_ordered;
        //edges - end

        string fileSeasonNumber = String.Empty;
        if (seasonNum != -1)
        {
            fileSeasonNumber = seasonNum.ToString(SEASON_NUMBER_STRING_FORMAT);
        }
        else
        {
            fileSeasonNumber = "all";
        }

        //constructing result file names
        var nodes_file_name = String.Format(EXPORTED_NODES_SEASON_NAME, fileSeasonNumber);
        var nodes_ext_file_name = String.Format(EXPORTED_NODES_EXT_SEASON_NAME, fileSeasonNumber);
        var edges_file_name = String.Format(EXPORTED_EDGES_SEASON_NAME, fileSeasonNumber);

        //creating directory
        if (!Directory.Exists(FOLDER_EXPORTED))
        {
            Directory.CreateDirectory(FOLDER_EXPORTED);
        }

        //deleting existing file
        File.Delete(nodes_file_name);
        File.Delete(nodes_ext_file_name);
        File.Delete(edges_file_name);

        //creating and writing content to files
        using (StreamWriter sw = File.CreateText(Path.Combine(FOLDER_EXPORTED, nodes_file_name)))
        {
            sw.Write(str_season_nodes);
        }

        using (StreamWriter sw = File.CreateText(Path.Combine(FOLDER_EXPORTED, nodes_ext_file_name)))
        {
            sw.Write(str_season_nodes_ext);
        }

        using (StreamWriter sw = File.CreateText(Path.Combine(FOLDER_EXPORTED, edges_file_name)))
        {
            sw.Write(str_season_edges);
        }
    }
}
