// Base URL for Nextcloud documents

const baseUrl = 'https://innovationstation.ddns.net/remote.php/dav/files/InnovationStation/Uploads/ ';
const username = "InnovationStation"; // Nextcloud username
const password = "IS_S2T24"; // Nextcloud password

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

    // Increment and decrement rating functions
    function incrementRating() {
        if (currentRating < maxRating) {
            currentRating++;
            updateRatingDisplay();
        }
    }

    function decrementRating() {
        if (currentRating > minRating) {
            currentRating--;
            updateRatingDisplay();
        }
    }

    // Initialize the display with the current rating
    updateRatingDisplay();
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

    document.getElementById('submitRating').addEventListener('click', submitRating);
    moreInfoButton.addEventListener('click', toggleAdditionalInfo);
    document.getElementById('submitModeration').addEventListener('click', openModerationModal);

    // Add event listener to dynamically created View File links
    const tableBody = document.querySelector('#documentsTable tbody');
    tableBody.addEventListener('click', (event) => {
        if (event.target.matches('.view-file-link')) {
            const fileUrl = event.target.dataset.fileUrl;
            openFileWithAuth(fileUrl);
        }
    });
});

// Fetch unmoderated documents
async function fetchUnmoderatedDocuments() {
    try {
        const response = await fetch('http://localhost:5281/api/Moderation/unmoderated');
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const documents = await response.json();
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
        // Use the provided URL to construct the full link for each document
        const fileUrl = baseUrl + doc.file_Url; // Ensure this matches your Nextcloud structure
        console.log('Constructed File URL:', fileUrl); // Log the URL for debugging

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
            <td><a href="https://innovationstation.ddns.net/s/your-shared-link" target="_blank">View File</a></td>
            <td>${doc.moderation_Status}</td>
            <td>${doc.ratings}</td>
            <td>${new Date(doc.date_Uploaded).toLocaleDateString()}</td>
        `;
        tableBody.appendChild(row);
    });
}

// Function to open file with Basic Authentication directly
function openFileWithAuth(fileUrl) {
    // Construct a URL with embedded Basic Authentication (only for test/dev purposes)
    const credentials = `${username}:${password}`;
    const authUrl = fileUrl.replace('http://', `http://${credentials}@`);

    console.log('Opening file with URL:', authUrl); // Log the authenticated URL
    
    // Open the file in a new tab using the authenticated URL
    window.open(authUrl, '_blank');
    function openFileWithAuth(fileUrl) {
    const credentials = btoa(`${username}:${password}`);
    const authUrl = `http://${username}:${password}@${fileUrl}`;

    console.log('Opening file with authenticated URL:', authUrl); // Log the URL

    // Open the file in a new tab
    window.open(authUrl, '_blank');
}
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

