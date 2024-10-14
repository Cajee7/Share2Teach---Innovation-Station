const apiUrl = 'http://localhost:5281/api/reporting'; // Update this URL to match your backend

async function submitReport() {
    const documentId = document.getElementById('documentId').value;
    const reason = document.getElementById('reason').value;

    if (!documentId || !reason) {
        alert("Please fill in all fields.");
        return;
    }

    const response = await fetch(`${apiUrl}/CreateReport`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ DocumentId: documentId, Reason: reason }),
    });

    const feedbackElement = document.getElementById('createReportFeedback');
    if (response.ok) {
        const result = await response.json();
        feedbackElement.innerText = "Report submitted successfully! ID: " + result.id;
        document.getElementById('documentId').value = '';
        document.getElementById('reason').value = '';
    } else {
        const error = await response.json();
        feedbackElement.innerText = "Error: " + error.message;
    }
}

async function fetchReports() {
    const response = await fetch(`${apiUrl}/GetAllReports`);

    const feedbackElement = document.getElementById('fetchReportFeedback');
    const reportsList = document.getElementById('reports-list');
    reportsList.innerHTML = ''; // Clear previous results

    if (response.ok) {
        const reports = await response.json();
        reports.forEach(report => {
            const row = `<tr>
                <td>${report.id}</td>
                <td>${report.documentId}</td>
                <td>${report.reason}</td>
                <td>${report.status}</td>
            </tr>`;
            reportsList.insertAdjacentHTML('beforeend', row);
        });
        feedbackElement.innerText = `${reports.length} reports fetched successfully.`;
    } else {
        const error = await response.json();
        feedbackElement.innerText = "Error: " + error.message;
    }
}

async function updateReportStatus() {
    const id = document.getElementById('reportId').value;
    const status = document.getElementById('status').value;

    if (!id || !status) {
        alert("Please fill in all fields.");
        return;
    }

    const response = await fetch(`${apiUrl}/updateStatus/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ Status: status }),
    });

    const feedbackElement = document.getElementById('updateStatusFeedback');
    if (response.ok) {
        feedbackElement.innerText = "Report status updated successfully!";
        document.getElementById('reportId').value = '';
    } else {
        const error = await response.json();
        feedbackElement.innerText = "Error: " + error.message;
    }
}

async function deleteApprovedReports() {
    const response = await fetch(`${apiUrl}/DeleteApprovedReports`, {
        method: 'DELETE',
    });

    const feedbackElement = document.getElementById('deleteReportFeedback');
    if (response.ok) {
        const result = await response.json();
        feedbackElement.innerText = result.message;
    } else {
        const error = await response.json();
        feedbackElement.innerText = "Error: " + error.message;
    }
}
