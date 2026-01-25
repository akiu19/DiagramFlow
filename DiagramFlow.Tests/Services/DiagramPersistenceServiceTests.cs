using System.Collections.Generic;
using System.IO;
using DiagramFlow.Models;
using DiagramFlow.Services;
using Newtonsoft.Json;
using Xunit;

namespace DiagramFlow.Tests.Services
{
    public class DiagramPersistenceServiceTests
    {
        [Fact]
        public void Save_ShouldWriteJsonToFile()
        {
            var service = new DiagramPersistenceService();
            var diagram = new DiagramDto
            {
                ZoomLevel = 1.5,
                Nodes = new List<NodeDto>
                {
                    new NodeDto { Id = System.Guid.NewGuid(), Text = "Test Node", X = 10, Y = 20 }
                }
            };
            
            var tempFile = Path.GetTempFileName();
            try
            {
                service.Save(diagram, tempFile);

                Assert.True(File.Exists(tempFile));
                string json = File.ReadAllText(tempFile);
                Assert.Contains("Test Node", json);
                Assert.Contains("1.5", json);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_ShouldReadJsonFromFile()
        {
            var service = new DiagramPersistenceService();
            var id = System.Guid.NewGuid();
            var diagram = new DiagramDto
            {
                ZoomLevel = 2.0,
                Nodes = new List<NodeDto>
                {
                    new NodeDto { Id = id, Text = "Loaded Node", X = 100, Y = 200 }
                }
            };

            var tempFile = Path.GetTempFileName();
            try
            {
                string json = JsonConvert.SerializeObject(diagram);
                File.WriteAllText(tempFile, json);

                var loaded = service.Load(tempFile);

                Assert.NotNull(loaded);
                Assert.Equal(2.0, loaded.ZoomLevel);
                Assert.Single(loaded.Nodes);
                Assert.Equal(id, loaded.Nodes[0].Id);
                Assert.Equal("Loaded Node", loaded.Nodes[0].Text);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var service = new DiagramPersistenceService();
            var result = service.Load("non_existent_file.json");
            Assert.Null(result);
        }
    }
}