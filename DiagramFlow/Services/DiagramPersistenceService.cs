using System.IO;
using DiagramFlow.Models;
using Newtonsoft.Json;

namespace DiagramFlow.Services
{
    public interface IDiagramPersistenceService
    {
        void Save(DiagramDto diagram, string filePath);
        DiagramDto Load(string filePath);
    }

    public class DiagramPersistenceService : IDiagramPersistenceService
    {
        public void Save(DiagramDto diagram, string filePath)
        {
            var json = JsonConvert.SerializeObject(diagram, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public DiagramDto Load(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<DiagramDto>(json);
        }
    }
}