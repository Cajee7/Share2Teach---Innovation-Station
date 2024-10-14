using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ReportManagement.Controllers;
using ReportManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Share2Teach.Tests.Controllers
{
    public class ReportingControllerTests
    {
        private readonly ReportingController _controller;
        private readonly Mock<IMongoCollection<ReportDto>> _mockReportCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;

        public ReportingControllerTests()
        {
            _mockReportCollection = new Mock<IMongoCollection<ReportDto>>();
            _mockDatabase = new Mock<IMongoDatabase>();

            _mockDatabase.Setup(db => db.GetCollection<ReportDto>("Reports", null))
                .Returns(_mockReportCollection.Object);

            _controller = new ReportingController(_mockDatabase.Object);
        }

        [Fact]
        public async Task SubmitReport_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            var newReport = new CreateReportDto
            {
                DocumentId = "doc123",
                Reason = "Test reason"
            };

            _mockReportCollection.Setup(c => c.InsertOneAsync(It.IsAny<ReportDto>(), null, default))
                                 .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SubmitReport(newReport);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var createdReport = Assert.IsType<ReportDto>(createdResult.Value);
            Assert.Equal("pending", createdReport.Status);
        }

        [Fact]
        public async Task GetAllReports_ReturnsOkResultWithReports()
        {
            // Arrange
            var reports = new List<ReportDto>
            {
                new ReportDto { Id = "1", DocumentId = "doc1", Reason = "Reason 1", Status = "pending" },
                new ReportDto { Id = "2", DocumentId = "doc2", Reason = "Reason 2", Status = "approved" }
            };
            var mockCursor = new Mock<IAsyncCursor<ReportDto>>();
            mockCursor.Setup(c => c.Current).Returns(reports);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
            _mockReportCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<ReportDto>>(), null, default))
                                 .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _controller.GetAllReports();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReports = Assert.IsType<List<ReportDto>>(okResult.Value);
            Assert.Equal(2, returnedReports.Count);
        }

        [Fact]
        public async Task UpdateReportStatus_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var updateDto = new UpdateReportDto { Status = "approved" };
            _mockReportCollection.Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<ReportDto>>(),
                    It.IsAny<UpdateDefinition<ReportDto>>(),
                    null, default))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _controller.UpdateReportStatus("1", updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Report status updated successfully.", ((dynamic)okResult.Value).message);
        }

        [Fact]
        public async Task DeleteApprovedReports_DeletesReports_ReturnsOkResult()
        {
            // Arrange
            _mockReportCollection.Setup(c => c.DeleteManyAsync(
                    It.IsAny<FilterDefinition<ReportDto>>(),
                    default))
                .ReturnsAsync(new DeleteResult.Acknowledged(3));

            // Act
            var result = await _controller.DeleteApprovedReports();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("3 approved reports deleted.", ((dynamic)okResult.Value).message);
        }

        [Fact]
        public async Task SubmitReport_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            CreateReportDto newReport = null;

            // Act
            var result = await _controller.SubmitReport(newReport);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Please provide all required information (DocumentId and Reason).", ((dynamic)badRequestResult.Value).message);
        }

        [Fact]
        public async Task UpdateReportStatus_WithInvalidStatus_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateReportDto { Status = "invalidStatus" };

            // Act
            var result = await _controller.UpdateReportStatus("1", updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Status must be either 'approved', 'denied', or 'pending'.", ((dynamic)badRequestResult.Value).message);
        }

        [Fact]
        public async Task UpdateReportStatus_ReportNotFound_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new UpdateReportDto { Status = "approved" };
            _mockReportCollection.Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<ReportDto>>(),
                    It.IsAny<UpdateDefinition<ReportDto>>(),
                    null, default))
                .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

            // Act
            var result = await _controller.UpdateReportStatus("1", updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Report not found or status unchanged.", ((dynamic)notFoundResult.Value).message);
        }

        [Fact]
        public async Task DeleteApprovedReports_NoApprovedReports_ReturnsNotFound()
        {
            // Arrange
            _mockReportCollection.Setup(c => c.DeleteManyAsync(
                    It.IsAny<FilterDefinition<ReportDto>>(),
                    default))
                .ReturnsAsync(new DeleteResult.Acknowledged(0));

            // Act
            var result = await _controller.DeleteApprovedReports();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No approved reports found to delete.", ((dynamic)notFoundResult.Value).message);
        }
    }
}
