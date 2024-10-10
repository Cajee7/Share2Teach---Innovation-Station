document.addEventListener('DOMContentLoaded', () => {
    let currentRating = 1;
    const maxRating = 10;
    const minRating = 1;

    const ratingDisplay = document.getElementById('ratingDisplay');
    const incrementButton = document.getElementById('incrementRating');
    const decrementButton = document.getElementById('decrementRating');
    const modal = document.getElementById('ratingModal');
    const closeButton = document.querySelector('.close-button');
    const moreInfoButton = document.getElementById('moreInfoButton');
    const additionalInfoDiv = document.getElementById('additionalInfo');

    // Update the rating display
    function updateRatingDisplay() {
        ratingDisplay.textContent = currentRating;
    }

    // Increment rating
    function incrementRating() {
        if (currentRating < maxRating) {
            currentRating++;
            updateRatingDisplay();
        }
    }

    // Decrement rating
    function decrementRating() {
        if (currentRating > minRating) {
            currentRating--;
            updateRatingDisplay();
        }
    }

    // Initialize the display with the current rating
    updateRatingDisplay();

    // Fetch unmoderated documents
    fetchUnmoderatedDocuments();

    // Event listeners
    incrementButton.addEventListener('click', incrementRating);
    decrementButton.addEventListener('click', decrementRating);
    closeButton.addEventListener('click', closeModal);
    window.addEventListener('click', (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    // Handle submit rating button click
    document.getElementById('submitRating').addEventListener('click', submitRating);
    
    // Handle More Info button click
    moreInfoButton.addEventListener('click', toggleAdditionalInfo);
    
    // New moderate button event
    document.getElementById('submitModeration').addEventListener('click', openModerationModal);
});

// Fetch unmoderated documents
async function fetchUnmoderatedDocuments() {
    try {
        const response = await fetch('http://localhost:5281/api/Moderation/unmoderated'); 
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const documents = await response.json();
        console.log('Fetched documents:', documents);
        populateTable(documents);
    } catch (error) {
        console.error('Error fetching unmoderated documents:', error);
        alert('Failed to fetch documents. Please try again later.');
    }
}

// Populate table with documents
function populateTable(documents) {
    const tableBody = document.querySelector('#documentsTable tbody');
    tableBody.innerHTML = '';

    if (!Array.isArray(documents) || documents.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="11">No documents found.</td></tr>';
        return;
    }

    documents.forEach(doc => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td><input type="radio" name="document" 
                data-title="${doc.title}" 
                data-grade="${doc.grade}" 
                data-subject="${doc.subject}" 
                data-description="${doc.description}" 
                data-file-size="${doc.file_Size}" /></td>
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

// Handle modal operations
function closeModal() {
    const modal = document.getElementById('ratingModal');
    modal.style.display = 'none';
    document.getElementById('comments').value = '';
}

function submitRating() {
    const title = document.getElementById('documentTitle').textContent;
    const comments = document.getElementById('comments').value;

    console.log(`Rating for ${title}: ${currentRating} Comments: ${comments}`);
    
    closeModal();
}

function toggleAdditionalInfo() {
    const additionalInfoDiv = document.getElementById('additionalInfo');
    const moreInfoButton = document.getElementById('moreInfoButton');
    
    if (additionalInfoDiv.style.display === 'none') {
        additionalInfoDiv.style.display = 'block';
        moreInfoButton.textContent = 'Less Info';
    } else {
        additionalInfoDiv.style.display = 'none';
        moreInfoButton.textContent = 'More Info';
    }
}

function openModerationModal() {
    const selectedDocument = document.querySelector('input[name="document"]:checked');

    if (selectedDocument) {
        document.getElementById('documentTitle').textContent = selectedDocument.getAttribute('data-title');
        document.getElementById('documentGrade').textContent = selectedDocument.getAttribute('data-grade');
        document.getElementById('documentSubject').textContent = selectedDocument.getAttribute('data-subject');
        document.getElementById('documentDescription').textContent = selectedDocument.getAttribute('data-description');
        document.getElementById('documentFileSize').textContent = `${selectedDocument.getAttribute('data-file-size')} MB`;

        document.getElementById('ratingModal').style.display = 'block';
    } else {
        alert("Please select a document to moderate.");
    }
}
