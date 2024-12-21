// Base URL for Nextcloud documents
const baseUrl = 'https://innovationstation.ddns.net/remote.php/dav/files/InnovationStation/Uploads/';
const username = "InnovationStation";
const password = "IS_S2T24";

// Global variables for rating system
let currentRating = 1;
const maxRating = 10;
const minRating = 1;

class NotificationManager {
    constructor() {
        this.notifications = new Set();
    }

    show(message, type = 'success', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        
        // Create message content
        const messageContent = document.createElement('div');
        messageContent.textContent = message;
        notification.appendChild(messageContent);

        // Create progress bar
        const progressBar = document.createElement('div');
        progressBar.className = 'progress-bar';
        notification.appendChild(progressBar);

        document.body.appendChild(notification);
        this.notifications.add(notification);

        // Trigger animation
        requestAnimationFrame(() => {
            notification.classList.add('show');
            progressBar.style.transition = `transform ${duration}ms linear`;
            progressBar.style.transform = 'scaleX(0)';
        });

        // Remove notification after duration
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(notification);
                this.notifications.delete(notification);
            }, 300);
        }, duration);
    }

    clearAll() {
        this.notifications.forEach(notification => {
            document.body.removeChild(notification);
        });
        this.notifications.clear();
    }
}

// Create a single instance of NotificationManager
const notificationManager = new NotificationManager();

// Initialize the application
document.addEventListener('DOMContentLoaded', () => {
    // Cache DOM elements
    const elements = {
        ratingDisplay: document.getElementById('ratingDisplay'),
        incrementButton: document.getElementById('incrementRating'),
        decrementButton: document.getElementById('decrementRating'),
        modal: document.getElementById('ratingModal'),
        closeButton: document.querySelector('.close-button'),
        moreInfoButton: document.getElementById('moreInfoButton'),
        additionalInfoDiv: document.getElementById('additionalInfo'),
        submitRatingButton: document.getElementById('submitRating'),
        submitModerationButton: document.getElementById('submitModeration'),
        tableBody: document.querySelector('#documentsTable tbody')
    };

    // Initialize the display with the current rating
    updateRatingDisplay(elements.ratingDisplay);

    // Fetch documents only once on page load
    fetchUnmoderatedDocuments();

    // Event listeners
    elements.incrementButton.addEventListener('click', () => incrementRating(elements.ratingDisplay));
    elements.decrementButton.addEventListener('click', () => decrementRating(elements.ratingDisplay));
    elements.closeButton.addEventListener('click', closeModal);
    elements.submitRatingButton.addEventListener('click', submitRating);
    elements.moreInfoButton.addEventListener('click', toggleMoreInfo);
    elements.submitModerationButton.addEventListener('click', openModerationModal);

    // Close modal when clicking outside
    window.addEventListener('click', (event) => {
        if (event.target === elements.modal) {
            closeModal();
        }
    });

    // Event delegation for file links
    elements.tableBody.addEventListener('click', (event) => {
        if (event.target.matches('.view-file-link')) {
            const fileUrl = event.target.dataset.fileUrl;
            openFileWithAuth(fileUrl);
        }
    });
});

// Rating functions
function updateRatingDisplay(display) {
    display.textContent = currentRating;
}

function incrementRating(display) {
    if (currentRating < maxRating) {
        currentRating++;
        updateRatingDisplay(display);
    }
}

function decrementRating(display) {
    if (currentRating > minRating) {
        currentRating--;
        updateRatingDisplay(display);
    }
}

// Fetch documents function
async function fetchUnmoderatedDocuments() {
    try {
        const response = await fetch('http://localhost:5281/api/Moderation/unmoderated');
        if (!response.ok) {
            notificationManager.show('Network response was not ok!', 'error', 4000);

        }
        const documents = await response.json();
        console.log('Received documents:', documents); // Add this line to debug
        populateTable(documents);
    } catch (error) {
        console.error('Error fetching unmoderated documents:', error);
        notificationManager.show('Failed to fetch documents. Please try again later.', 'error', 4000);

    }
}

function convertToObjectId(idObj) {
    if (idObj && typeof idObj === 'object') {
        const { timestamp, machine, pid, increment } = idObj;

        // Convert each part to hexadecimal and pad with leading zeros
        const timestampHex = timestamp.toString(16).padStart(8, '0');  // 4 bytes -> 8 hex digits
        const machineHex = machine.toString(16).padStart(6, '0');      // 3 bytes -> 6 hex digits
        const pidHex = pid.toString(16).padStart(4, '0');              // 2 bytes -> 4 hex digits
        const incrementHex = increment.toString(16).padStart(6, '0');  // 3 bytes -> 6 hex digits

        // Concatenate to form the final ObjectId (24 hex characters)
        return `${timestampHex}${machineHex}${pidHex}${incrementHex}`;
    }

    return null; // Return null if the idObj is invalid
}

function populateTable(documents) {
    const tableBody = document.querySelector('#documentsTable tbody');
    tableBody.innerHTML = '';

    console.log('Raw documents from server:', documents);

    if (!Array.isArray(documents) || documents.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="11">No documents found.</td></tr>';
        return;
    }

    documents.forEach(doc => {
        console.log('Processing document:', doc);

        let documentId = '';

        // Use the convertToObjectId function if the id is an object
        if (doc.id && typeof doc.id === 'object') {
            documentId = convertToObjectId(doc.id);
        } else if (typeof doc._id === 'string' && /^[0-9a-fA-F]{24}$/.test(doc._id)) {
            documentId = doc._id;
        }

        console.log('Extracted ID:', documentId, 'from document:', doc.title);

        const row = document.createElement('tr');
        
        // Create the radio input separately for better control
        const radioInput = document.createElement('input');
        radioInput.type = 'radio';
        radioInput.name = 'document';
        radioInput.setAttribute('data-document-id', documentId);
        radioInput.setAttribute('data-title', doc.title || '');
        radioInput.setAttribute('data-grade', doc.grade || '');
        radioInput.setAttribute('data-subject', doc.subject || '');
        radioInput.setAttribute('data-description', doc.description || '');
        radioInput.setAttribute('data-file-size', doc.file_Size || 0);

        // Create first cell and append radio input
        const firstCell = document.createElement('td');
        firstCell.appendChild(radioInput);
        row.appendChild(firstCell);

        // Add remaining cells
        row.innerHTML += `
            <td>${doc.title || ''}</td>
            <td>${doc.subject || ''}</td>
            <td>${doc.grade || ''}</td>
            <td>${doc.description || ''}</td>
            <td>${doc.file_Size || 0} MB</td>
            <td><a href="#" class="view-file-link" data-file-url="${baseUrl}${doc.file_Url || ''}">View File</a></td>
            <td>${doc.moderation_Status || ''}</td>
            <td>${doc.ratings || 0}</td>
            <td>${new Date(doc.date_Uploaded || Date.now()).toLocaleDateString()}</td>
        `;
        
        tableBody.appendChild(row);

        // Verify the data attributes were set correctly
        console.log('Radio button attributes:', {
            id: radioInput.getAttribute('data-document-id'),
            title: radioInput.getAttribute('data-title'),
            grade: radioInput.getAttribute('data-grade'),
            subject: radioInput.getAttribute('data-subject'),
            description: radioInput.getAttribute('data-description'),
            fileSize: radioInput.getAttribute('data-file-size')
        });
    });
}



// File handling functions
function openFileWithAuth(fileUrl) {
    const credentials = btoa(`${username}:${password}`);
    const authUrl = fileUrl.replace('://', `://${username}:${password}@`);
    window.open(authUrl, '_blank');
}

// Modal functions
function closeModal() {
    const modal = document.getElementById('ratingModal');
    modal.style.display = 'none';
    document.getElementById('comments').value = '';
}

function getAuthToken() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.warn('No authentication token found. User might need to log in.');
        return null;
    }
    return token;
}

async function submitRating() {
    try {
        const selectedDocument = document.querySelector('input[name="document"]:checked');
        if (!selectedDocument) {
            notificationManager.show('Please select a document to moderate!', 'error', 2000);
            return;
        }

        const documentId = selectedDocument.getAttribute('data-document-id');
        console.log('Selected document element:', selectedDocument);
        console.log('All data attributes:', {
            id: documentId,
            title: selectedDocument.getAttribute('data-title'),
            grade: selectedDocument.getAttribute('data-grade'),
            subject: selectedDocument.getAttribute('data-subject'),
            description: selectedDocument.getAttribute('data-description'),
            fileSize: selectedDocument.getAttribute('data-file-size')
        });

        if (!documentId) {
            notificationManager.show('Document ID is missing. Please ensure documents are loaded correctly!', 'error', 4000);
        }

        // Validate DocumentId format (24-character hexadecimal string)
        const objectIdRegex = /^[0-9a-fA-F]{24}$/;
        if (!objectIdRegex.test(documentId)) {
            throw new Error('Invalid document ID format. Please try again or contact support.');
        }

        const comments = document.getElementById('comments').value;
        const moderationData = {
            documentId: documentId,
            status: "Moderated",
            comment: comments,
            rating: currentRating
        };

        console.log('Submitting moderation data:', moderationData);

        const submitButton = document.getElementById('submitRating');
        submitButton.disabled = true;
        submitButton.textContent = 'Submitting...';

        const token = getAuthToken();
        if (!token) {
            notificationManager.show('Authentication token is missing or has expired, please log in again!', 'error', 4000);
        }

        const response = await fetch(`http://localhost:5281/api/Moderation/update/${documentId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(moderationData)
        });

        const responseText = await response.text();
        console.log('Server response:', responseText);

        let responseData;
        try {
            responseData = JSON.parse(responseText);
        } catch (e) {
            responseData = { message: responseText };
        }

        if (!response.ok) {
            notificationManager.show('Server returned an error!', 'error', 4000);

        }

        notificationManager.show('Document successfully moderated!', 'success', 4000);
        //alert('Document successfully moderated!');
        closeModal();
        fetchUnmoderatedDocuments();

    } catch (error) {
        console.error('Error submitting moderation:', error);
        notificationManager.show('Failed to submit moderated document, please try again', 'error', 4000);
    } finally {
        const submitButton = document.getElementById('submitRating');
        submitButton.disabled = false;
        submitButton.textContent = 'Submit';
    }
}

function isValidObjectId(id) {
    return /^[0-9a-fA-F]{24}$/.test(id);
}

// Toggle additional information display
function toggleMoreInfo() {
    const additionalInfoDiv = document.getElementById('additionalInfo');
    const moreInfoButton = document.getElementById('moreInfoButton');
    
    console.log("More Info button clicked"); // Debugging line

    // Check if additionalInfoDiv is hidden or shown
    if (additionalInfoDiv.style.display === 'none' || additionalInfoDiv.style.display === '') {
        console.log("Showing additional info"); // Debugging line
        additionalInfoDiv.style.display = 'block';
        moreInfoButton.textContent = 'Less Info';
    } else {
        console.log("Hiding additional info"); // Debugging line
        additionalInfoDiv.style.display = 'block';
        moreInfoButton.textContent = 'Less Info';
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
        notificationManager.show('Please select a document to moderate!', 'error', 2000);

    }
}

function goBack() {
    window.location.href = 'index.html';
}