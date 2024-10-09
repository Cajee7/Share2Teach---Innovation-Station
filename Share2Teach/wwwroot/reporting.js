document.getElementById('report-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    const documentId = document.getElementById('documentId').value;
    const reason = document.getElementById('reason').value;

    try {
        const response = await fetch('http://localhost:5000/api/Reporting/CreateReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ documentId, reason })
        });

        const result = await response.json();
        document.getElementById('create-response').textContent = result.message || 'Report submitted successfully!';
    } catch (error) {
        document.getElementById('create-response').textContent = 'Error submitting report: ' + error.message;
    }
});

document.getElementById('fetch-reports').addEventListener('click', async () => {
    try {
        const response = await fetch('http://localhost:5000/api/Reporting/GetAllReports');
        const reports = await response.json();
        const reportsList = document.getElementById('reports-list');
        reportsList.innerHTML = '';

        reports.forEach(report => {
            const listItem = document.createElement('li');
            listItem.textContent = `ID: ${report.id}, Document ID: ${report.documentId}, Reason: ${report.reason}, Status: ${report.status}, Date: ${new Date(report.dateReported).toLocaleDateString()}`;
            reportsList.appendChild(listItem);
        });
    } catch (error) {
        alert('Error fetching reports: ' + error.message);
    }
});

document.getElementById('update-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    const reportId = document.getElementById('reportId').value;
    const status = document.getElementById('status').value;

    try {
        const response = await fetch(`http://localhost:5000/api/Reporting/updateStatus/${reportId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ status })
        });

        const result = await response.json();
        document.getElementById('update-response').textContent = result.message;
    } catch (error) {
        document.getElementById('update-response').textContent = 'Error updating status: ' + error.message;
    }
});
