using Newtonsoft.Json.Linq;

namespace BeatSaverNotifier.BeatSaver.Models;

public class DifficultyModel
{
    public int NoteCount { get; private set; }
    public int BombCount { get; private set; }
    public int WallCount { get; private set; }
    public float NotesPerSecond { get; private set; }
    public DifficultyTypes Difficulty { get; private set; }
    public CharacteristicTypes Characteristic { get; private set; }

    private DifficultyModel(int noteCount, int bombCount, int wallCount, float notesPerSecond, CharacteristicTypes characteristic, DifficultyTypes difficulty)
    {
        this.NoteCount = noteCount;
        this.BombCount = bombCount;
        this.WallCount = wallCount;
        this.NotesPerSecond = notesPerSecond;
        this.Characteristic = characteristic;
        this.Difficulty = difficulty;
    }
    
    public static DifficultyTypes getSelectedDifficultyFromText(string text) => text switch
    {
        "Easy" => DifficultyModel.DifficultyTypes.Easy,
        "Normal" => DifficultyModel.DifficultyTypes.Normal,
        "Hard" => DifficultyModel.DifficultyTypes.Hard,
        "Expert" => DifficultyModel.DifficultyTypes.Expert,
        "Expert+" => DifficultyModel.DifficultyTypes.ExpertPlus,
        _ => DifficultyModel.DifficultyTypes.Unknown
    };
    
    public static CharacteristicTypes getSelectedCharacteristicFromText(string text) => text switch
    {
        "Standard" => DifficultyModel.CharacteristicTypes.Standard,
        "OneSaber" => DifficultyModel.CharacteristicTypes.OneSaber,
        "NoArrows" => DifficultyModel.CharacteristicTypes.NoArrows,
        "Legacy" => DifficultyModel.CharacteristicTypes.Legacy,
        "360Degree" => DifficultyModel.CharacteristicTypes.ThreeSixtyDegree,
        "90Degree" => DifficultyModel.CharacteristicTypes.NintetyDegree,
        "Lawless" => DifficultyModel.CharacteristicTypes.Lawless,
        "Lightshow" => DifficultyModel.CharacteristicTypes.Lightshow,
        _ => DifficultyModel.CharacteristicTypes.Unknown
    };

    public static DifficultyModel Parse(string json)
    {
        var jObject = JObject.Parse(json);
        var notes = jObject["notes"]?.Value<int>() ?? 0;
        var bombs = jObject["bombs"]?.Value<int>() ?? 0;
        var walls = jObject["obstacles"]?.Value<int>() ?? 0;
        var notesPerSecond = jObject["nps"]?.Value<float>() ?? 0;
        var characteristic = jObject["characteristic"]?.Value<string>() switch
        {
            "Standard" => CharacteristicTypes.Standard,
            "OneSaber" => CharacteristicTypes.OneSaber,
            "360Degree" => CharacteristicTypes.ThreeSixtyDegree,
            "90Degree" => CharacteristicTypes.NintetyDegree,
            "NoArrows" => CharacteristicTypes.NoArrows,
            "Legacy" => CharacteristicTypes.Legacy,
            "Lawless" => CharacteristicTypes.Lawless,
            "Lightshow" => CharacteristicTypes.Lightshow,
            _ => CharacteristicTypes.Unknown,
        };
        
        var difficulty = jObject["difficulty"]?.Value<string>() switch
        {
            "Easy" => DifficultyTypes.Easy,
            "Normal" => DifficultyTypes.Normal,
            "Hard" => DifficultyTypes.Hard,
            "Expert" => DifficultyTypes.Expert,
            "ExpertPlus" => DifficultyTypes.ExpertPlus,
            _ => DifficultyTypes.Unknown,
        };
        
        return new DifficultyModel(notes, bombs, walls, notesPerSecond, characteristic, difficulty);
    }
    
    public enum CharacteristicTypes
    {
        Standard,
        OneSaber,
        ThreeSixtyDegree,
        NintetyDegree,
        NoArrows,
        Legacy,
        Lawless,
        Lightshow,
        Unknown
    }

    public enum DifficultyTypes
    {
        ExpertPlus,
        Expert,
        Hard,
        Normal,
        Easy,
        Unknown
    }
}