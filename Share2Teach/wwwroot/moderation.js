document.addEventListener('DOMContentLoaded', () => {
    fetchUnmoderatedDocuments();
});

async function fetchUnmoderatedDocuments() {
    try {
        const response = await fetch('http://localhost:5281/api/Moderation/unmoderated'); 
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const documents = await response.json();
        console.log('Fetched documents:', documents); // Log the documents to verify the data structure
        populateTable(documents);
    } catch (error) {
        console.error('Error fetching unmoderated documents:', error);
    }
}

function populateTable(documents) {
    const tableBody = document.querySelector('#documentsTable tbody');
    tableBody.innerHTML = ''; // Clear any existing rows

    // Check if documents is an array
    if (!Array.isArray(documents) || documents.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="11">No documents found.</td></tr>';
        return; // Exit if no documents
    }

    // Loop through each document and create a table row
    documents.forEach(doc => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${doc.title}</td>
            <td>${doc.subject}</td>
            <td>${doc.grade}</td>
            <td>${doc.description}</td>
            <td>${doc.file_Size} MB</td>
            <td><a href="file:///${doc.file_Url.replace(/\\/g, '/')}" target="_blank">View File</a></td>
            <td>${doc.moderation_Status}</td>
            <td>${doc.ratings}</td>
            <td>${new Date(doc.date_Uploaded).toLocaleDateString()}</td>
            <td>${doc.date_Updated ? new Date(doc.date_Updated).toLocaleDateString() : ''}</td>
        `;
        tableBody.appendChild(row);
    });
}
