// Base URL for the backend API
const baseUrl = 'http://localhost:5000/api/Reporting';

// Handle form submission for creating a new report
document.getElementById('report-form').addEventListener('submit', async (event) => {
    event.preventDefault();

    const documentId = document.getElementById('documentId').value.trim();
    const reason = document.getElementById('reason').value.trim();

    // Input validation
    if (!documentId || !reason) {
        alert('Please provide a Document ID and a Reason for the report.');
        return;
    }

    try {
        const response = await fetch(`${baseUrl}/CreateReport`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ documentId, reason })
        });

        const result = await response.json();
        document.getElementById('create-response').textContent = result.message || 'Report submitted successfully!';
    } catch (error) {
        console.error('Error submitting report:', error);
        document.getElementById('create-response').textContent = 'Error submitting report: ' + error.message;
    }
});

// Handle fetching all reports
document.getElementById('fetch-reports').addEventListener('click', async () => {
    try {
        const response = await fetch(`${baseUrl}/GetAllReports`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            }
        });

        const reports = await response.json();
        const reportsList = document.getElementById('reports-list');
        reportsList.innerHTML = '';

        // Display each report in a list item
        reports.forEach(report => {
            const listItem = document.createElement('li');
            listItem.textContent = `ID: ${report.id}, Document ID: ${report.documentId}, Reason: ${report.reason}, Status: ${report.status}, Date: ${new Date(report.dateReported).toLocaleDateString()}`;
            reportsList.appendChild(listItem);
        });
    } catch (error) {
        console.error('Error fetching reports:', error);
        alert('Error fetching reports: ' + error.message);
    }
});

// Handle form submission for updating report status
document.getElementById('update-form').addEventListener('submit', async (event) => {
    event.preventDefault();

    const reportId = document.getElementById('reportId').value.trim();
    const status = document.getElementById('status').value.trim();

    // Input validation
    if (!reportId || !status) {
        alert('Please provide a Report ID and select a Status.');
        return;
    }

    try {
        const response = await fetch(`${baseUrl}/updateStatus/${reportId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ status })
        });

        const result = await response.json();
        document.getElementById('update-response').textContent = result.message || 'Status updated successfully!';
    } catch (error) {
        console.error('Error updating report status:', error);
        document.getElementById('update-response').textContent = 'Error updating status: ' + error.message;
    }
});
