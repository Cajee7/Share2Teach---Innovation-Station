document.addEventListener('DOMContentLoaded', () => {
    fetchUnmoderatedDocuments();

    // Modal event listeners
    const closeButton = document.querySelector('.close-button');
    const modal = document.getElementById('ratingModal');
    const moreInfoButton = document.getElementById('moreInfoButton');
    const additionalInfoDiv = document.getElementById('additionalInfo');

    // Close modal button
    closeButton.addEventListener('click', () => {
        modal.style.display = 'none';
    });

    // Close modal when clicking outside
    window.addEventListener('click', (event) => {
        if (event.target === modal) {
            modal.style.display = 'none';
        }
    });

    // Handle submit rating button click
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

    // Handle More Info button click
    moreInfoButton.addEventListener('click', () => {
        if (additionalInfoDiv.style.display === 'none') {
            additionalInfoDiv.style.display = 'block';
            moreInfoButton.textContent = 'Less Info';
        } else {
            additionalInfoDiv.style.display = 'none';
            moreInfoButton.textContent = 'More Info';
        }
    });

    // New moderate button event
    document.getElementById('submitModeration').addEventListener('click', () => {
        const selectedDocument = document.querySelector('input[name="document"]:checked');

        if (selectedDocument) {
            const docTitle = selectedDocument.getAttribute('data-title');
            const docGrade = selectedDocument.getAttribute('data-grade');
            const docSubject = selectedDocument.getAttribute('data-subject');
            const docDescription = selectedDocument.getAttribute('data-description');
            const docFileSize = selectedDocument.getAttribute('data-file-size');

            // Populate modal with document data
            document.getElementById('documentTitle').textContent = docTitle;
            document.getElementById('documentGrade').textContent = docGrade;
            document.getElementById('documentSubject').textContent = docSubject;
            document.getElementById('documentDescription').textContent = docDescription;
            document.getElementById('documentFileSize').textContent = `${docFileSize} MB`;

            // Show the modal
            modal.style.display = 'block';
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
            <td><input type="radio" name="document" 
                data-title="${doc.title}" 
                data-grade="${doc.grade}" 
                data-subject="${doc.subject}" 
                data-description="${doc.description}" 
                data-file-size="${doc.file_Size}" /></td> <!-- Radio button for selection -->
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
