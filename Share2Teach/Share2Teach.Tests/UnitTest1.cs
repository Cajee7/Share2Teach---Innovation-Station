using Xunit;
using Moq;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Document_Model.Models;
using Combined.Controllers;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Threading;
using Search.Models;

namespace Share2Teach.Tests
{
    public class FileControllerTests
    {
        private readonly Mock<IMongoDatabase> _mockDb;
        private readonly Mock<IMongoCollection<Documents>> _mockCollection;
        private readonly FileController _controller;

        public FileControllerTests()
        {
            // Setup MongoDB mocks
            _mockDb = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<Documents>>();
            
            // Setup the collection mock to handle index creation
            var mockIndexManager = new Mock<IMongoIndexManager<Documents>>();
            _mockCollection.Setup(c => c.Indexes).Returns(mockIndexManager.Object);
            
            // Setup the database to return our mock collection
            _mockDb.Setup(db => db.GetCollection<Documents>("Documents", null))
                .Returns(_mockCollection.Object);

            _controller = new FileController(_mockDb.Object);
        }

        [Fact]
        public async Task UploadFile_WithValidPdfFile_ReturnsOkResult()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "This is a test PDF file content";
            var fileName = "test.pdf";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.ContentType).Returns("application/pdf");

            var request = new CombinedUploadRequest
            {
                UploadedFile = fileMock.Object,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<Documents>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UploadFile_WithNoFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new CombinedUploadRequest
            {
                UploadedFile = null,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value;
            Assert.Equal("No file was uploaded.", (string)value.message);
        }

        [Fact]
        public async Task UploadFile_WithOversizedFile_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "large.pdf";
            var fileSize = 26 * 1024 * 1024; // 26MB (exceeds 25MB limit)

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(fileSize);

            var request = new CombinedUploadRequest
            {
                UploadedFile = fileMock.Object,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value;
            Assert.Contains("File size exceeds the limit", (string)value.message);
        }

        [Fact]
        public async Task UploadFile_WithInvalidFileType_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "test.txt";
            
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(1000);

            var request = new CombinedUploadRequest
            {
                UploadedFile = fileMock.Object,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value;
            Assert.Contains("File type '.txt' is not allowed", (string)value.message);
        }

        [Fact]
        public async Task UploadFile_WithValidDocxFile_ConvertsToValidPdfAndReturnsOkResult()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "test.docx";
            var content = new byte[1000]; // Sample content
            var stream = new MemoryStream(content);

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new CombinedUploadRequest
            {
                UploadedFile = fileMock.Object,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<Documents>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Verify that MongoDB insertion was called
            _mockCollection.Verify(c => c.InsertOneAsync(
                It.Is<Documents>(d => d.File_Type == ".pdf"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task UploadFile_WhenMongoDbFails_ReturnsInternalServerError()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "test.pdf";
            var stream = new MemoryStream(new byte[1000]);

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new CombinedUploadRequest
            {
                UploadedFile = fileMock.Object,
                Title = "Test Document",
                Subject = "Mathematics",
                Grade = 10,
                Description = "Test description"
            };

            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<Documents>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("MongoDB connection failed"));

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value;
            Assert.Contains("Internal server error", (string)value.message);
        }
    }
}