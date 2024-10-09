document.addEventListener('DOMContentLoaded', () => {
    fetchUnmoderatedDocuments();

    // Modal event listeners
    const closeButton = document.querySelector('.close-button');
    const modal = document.getElementById('ratingModal');

    closeButton.addEventListener('click', () => {
        modal.style.display = 'none';
    });

    window.addEventListener('click', (event) => {
        if (event.target === modal) {
            modal.style.display = 'none';
        }
    });

    document.getElementById('submitRating').addEventListener('click', () => {
        const title = document.getElementById('documentTitle').textContent;
        const rating = document.getElementById('rating').value;
        const comments = document.getElementById('comments').value;

        // Handle the submission logic (e.g., send to the server)
        console.log(`Rating for ${title}: ${rating} Comments: ${comments}`);
        
        // Close modal after submission
        modal.style.display = 'none';

        // Clear the input fields
        document.getElementById('comments').value = '';
    });

    // New moderate button event
    document.getElementById('submitModeration').addEventListener('click', () => {
        const selectedDocument = document.querySelector('input[name="document"]:checked');

        if (selectedDocument) {
            const docTitle = selectedDocument.getAttribute('data-title');
            document.getElementById('documentTitle').textContent = docTitle; // Set the document title in the modal
            modal.style.display = 'block'; // Show the modal
        } else {
            alert("Please select a document to moderate.");
        }
    });
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
            <td><input type="radio" name="document" data-title="${doc.title}" /></td> <!-- Radio button for selection -->
            <td>${doc.title}</td>
            <td>${doc.subject}</td>
            <td>${doc.grade}</td>
            <td>${doc.description}</td>
            <td>${doc.file_Size} MB</td>
            <td><a href="file:///${doc.file_Url.replace(/\\/g, '/')}" target="_blank">View File</a></td>
            <td>${doc.moderation_Status}</td>
            <td>${doc.ratings}</td>
            <td>${doc.tags ? doc.tags.join(', ') : ''}</td>
            <td>${new Date(doc.date_Uploaded).toLocaleDateString()}</td>
            <td>${doc.date_Updated ? new Date(doc.date_Updated).toLocaleDateString() : ''}</td>
        `;
        tableBody.appendChild(row);
    });
}
