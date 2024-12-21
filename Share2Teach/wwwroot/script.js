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



// Function to open tabs and handle dynamic loading
function openTab(evt, tabName) {
    const tabContents = document.getElementsByClassName("tab-content");
    for (let i = 0; i < tabContents.length; i++) {
        tabContents[i].style.display = "none"; // Hide all tab content
    }

    const tabButtons = document.getElementsByClassName("tab-button");
    for (let i = 0; i < tabButtons.length; i++) {
        tabButtons[i].className = tabButtons[i].className.replace(" active", ""); // Remove active class
    }

    document.getElementById(tabName).style.display = "block"; // Show the selected tab content
    evt.currentTarget.className += " active"; // Set the active class

    // Close document display when switching tabs
    const displayDiv = document.getElementById('documentDisplay');
    if (displayDiv) {
        displayDiv.innerHTML = ""; // Clear the document display content
    }

    // Load FAQs if the FAQ tab is opened
    if (tabName === 'faq') {
        loadFAQs(); // Load FAQs when FAQ tab is opened
    }
}


// Function to fetch FAQs from the API
async function loadFAQs() {
    const faqErrorMessage = document.getElementById('faq-error-message'); // Error message for FAQs
    faqErrorMessage.style.display = 'none'; // Hide previous error messages

    try {
        const response = await fetch('http://localhost:5281/api/FAQ/list'); 
        if (!response.ok) throw new Error(notificationManager.show('Error loading FAQS', 'error', 4000));

        const faqs = await response.json();
        renderFAQs(faqs); // Render the fetched FAQs
    } catch (error) {
        faqErrorMessage.innerText = notificationManager.show('Error fetching FAQS', 'error', 4000);
        faqErrorMessage.style.display = 'block'; // Show the error message
    }
}

async function performLogin(event) {
    event.preventDefault();
    
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value.trim();
    const errorMessage = document.getElementById('error-message');
    const successMessage = document.getElementById('success-message');
    
    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    // Validation code remains the same...
    let errors = [];
    if (!email) {
        errors.push('Email is required.');
    } else if (!validateEmail(email)) {
        errors.push('Please enter a valid email address.');
    }

    if (!password) {
        errors.push('Password is required.');
    } else if (password.length < 6) {
        errors.push('Password must be at least 6 characters long.');
    }

    if (errors.length > 0) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = errors.join('<br>');
        return;
    }

    const formData = new FormData();
    formData.append('Email', email);
    formData.append('Password', password);

    try {
        const response = await fetch('http://localhost:5281/api/Authenticate/login', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            
            // Store user information
            localStorage.setItem('userName', result.userName);
            localStorage.setItem('isLoggedIn', 'true');
            localStorage.setItem('userRole', result.role);
            localStorage.setItem('token', result.token);

            // Show success message with shorter animation
            successMessage.style.display = 'block';
            successMessage.innerHTML = 'Logged in successfully!';
            successMessage.style.backgroundColor = "#4CAF50";
            successMessage.style.bottom = '-100px';
            successMessage.style.opacity = '0';

            // Animate in
            requestAnimationFrame(() => {
                successMessage.style.bottom = '20px';
                successMessage.style.opacity = '1';
            });

            // Update UI immediately
            loadUserProfile();
            updateContributeTabAndNavigation(result.role);

            // Redirect after a short delay (just 1.5 seconds total)
            setTimeout(() => {
                successMessage.style.bottom = '-100px';
                successMessage.style.opacity = '0';
                
                setTimeout(() => {
                    window.location.href = './index.html';
                }, 500); // Additional 0.5s for fade out
            }, 1000); // Show message for 1s

        } else if (response.status === 400) {
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'Invalid input. Please check your email and password.';
        } else if (response.status === 401) {
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'Invalid login attempt. Please check your email and password.';
        } else {
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'An error occurred while logging in. Please try again later.';
        }
    } catch (error) {
        console.error('An error occurred:', error);
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'An unexpected error occurred. Please try again.';
    }
}

function getAuthToken() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.warn('No authentication token found. User might need to log in.');
        return null;
    }
    return token;
}

function checkLoginStatus() {
    const isLoggedIn = localStorage.getItem('isLoggedIn');
    const userName = localStorage.getItem('userName');
    const userRole = localStorage.getItem('userRole');

    if (isLoggedIn && userName) {
        loadUserProfile();
        updateContributeTabAndNavigation(userRole);
    }
}

// Call this function when the page loads
document.addEventListener('DOMContentLoaded', checkLoginStatus);

// Function to clear the login form
function clearLoginForm() {
    document.getElementById('email').value = '';
    document.getElementById('password').value = '';
    const errorMessage = document.getElementById('error-message');
    if (errorMessage) errorMessage.style.display = 'none'; // Hide the error message on clearing the form
}

// Helper function to validate email format using regex
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

document.querySelector('.forgot-password a').addEventListener('click', function() {
    document.getElementById('login').style.display = 'none'; // Hide login form
    document.getElementById('forgot-password').style.display = 'block'; // Show forgot password form
});


async function submitForgotPassword(event) {
    event.preventDefault();
    const email = document.getElementById('forgot-email').value.trim();
    const errorMessage = document.getElementById('forgot-error-message');
    const successMessage = document.getElementById('forgot-success-message');

    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    // Validate email
    if (!email) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'Email is required.';
        return;
    }

    // Prepare FormData
    const formData = new FormData();
    formData.append('Email', email);

    try {
        const response = await fetch('http://localhost:5281/api/Authenticate/forgot-password', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            notificationManager.show('Password reset token sent to your email.', 'success', 4000);
            // Optionally show reset password form
            document.getElementById('forgot-password').style.display = 'none';
            document.getElementById('reset-password').style.display = 'block';
        } else {
            const result = await response.json();
            notificationManager.show('An error occured', 'error', 4000);
        }
    } catch (error) {
        notificationManager.show('An unexpected error occured, please try again!', 'error', 4000);
    }
}

async function submitResetPassword(event) {
    event.preventDefault();
    const token = document.getElementById('reset-token').value.trim();
    const newPassword = document.getElementById('new-password').value.trim();
    const confirmPassword = document.getElementById('confirm-password').value.trim();
    const errorMessage = document.getElementById('reset-error-message');
    const successMessage = document.getElementById('reset-success-message');

    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    // Validate passwords
    if (newPassword !== confirmPassword) {
        notificationManager.show('Passwords do not match!', 'error', 4000);
        return;
    }
    
    // Prepare FormData
    const formData = new FormData();
    formData.append('Token', token);
    formData.append('NewPassword', newPassword);
    formData.append('ConfirmPassword', confirmPassword);

    try {
        const response = await fetch('http://localhost:5281/api/Authenticate/reset-password', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            notificationManager.show('Password has been reset successfully. You can now log in with your new password.', 'success', 4000);
            window.location.href = './index.html'; // Redirect to landing page
            
            //redirect
            
        } else {
            const result = await response.json();
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = result.message || 'An error occurred.';
        }
    } catch (error) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'An unexpected error occurred. Please try again.';
    }
}

function clearForgotPasswordForm() {
    document.getElementById('forgot-password-form').reset();
    document.getElementById('forgot-error-message').style.display = 'none';
    document.getElementById('forgot-success-message').style.display = 'none';
}

function clearResetPasswordForm() {
    document.getElementById('reset-password-form').reset();
    document.getElementById('reset-error-message').style.display = 'none';
    document.getElementById('reset-success-message').style.display = 'none';
}


// Function to render FAQs
function renderFAQs(faqs) {
    const faqContainer = document.getElementById('faq-list-container'); // ID from your HTML for the FAQ list
    faqContainer.innerHTML = ''; // Clear existing FAQs

    if (faqs.length === 0) {
        faqContainer.innerHTML = '<p>No FAQs available.</p>';
        return;
    }

    faqs.forEach(faq => {
        const faqItem = document.createElement('div');
        faqItem.className = 'faq-item'; 
        faqItem.innerHTML = `
            <h3 class="question">Q: ${faq.question || 'No question available'}</h3>
            <p class="answer">A: ${faq.answer || 'No answer available'}</p>
            <small>Date uploaded: ${faq.dateAdded ? new Date(faq.dateAdded).toLocaleDateString() : 'Date not available'}</small>
            <h4 class="faq-id"><span>ID: ${faq.id || 'N/A'}</span></h3>  <!-- Updated to use id -->
        `;
        faqContainer.appendChild(faqItem);
    });
}



// Global variable to track current operation
let currentOperation = '';

// Define apiUrl (you may want to set this to your actual API URL)
const apiUrl = 'http://localhost:5281/api/FAQ';

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM fully loaded and parsed');

    const faqModal = document.getElementById('faq-modal');
    const addFaqButton = document.getElementById('add-faq');
    const updateFaqButton = document.getElementById('update-faq');
    const deleteFaqButton = document.getElementById('delete-faq');
    const backBtn = document.getElementById('back-btn');
    const submitBtn = document.getElementById('submit-btn');
    const clearBtn = document.getElementById('clear-btn');

    console.log('Back button element:', backBtn);

    // Open modal for Add operation
    if (addFaqButton) {
        addFaqButton.addEventListener('click', () => openModal('add'));
    }

    // Open modal for Update operation
    if (updateFaqButton) {
        updateFaqButton.addEventListener('click', () => openModal('update'));
    }

    // Open modal for Delete operation
    if (deleteFaqButton) {
        deleteFaqButton.addEventListener('click', () => openModal('delete'));
    }

    // Submit form
    if (submitBtn) {
        submitBtn.addEventListener('click', handleSubmit);
    }

    // Clear form
    if (clearBtn) {
        clearBtn.addEventListener('click', clearForm);
    }
});

function openModal(operation) {
    console.log('Opening modal for operation:', operation);
    currentOperation = operation;
    const modal = document.getElementById('faq-modal');
    const modalTitle = document.getElementById('modal-title');
    const faqIdField = document.getElementById('faq-id-field');
    const questionField = document.getElementById('question-field');
    const answerField = document.getElementById('answer-field');

    if (modal) {
        modal.style.display = 'block'; // Make modal visible
    } else {
        console.error('Modal element not found when opening modal');
        return;
    }

    switch (operation) {
        case 'add':
            modalTitle.textContent = 'Add New FAQ';
            faqIdField.style.display = 'none';
            questionField.style.display = 'block';
            answerField.style.display = 'block';
            clearForm();
            break;
        case 'update':
            modalTitle.textContent = 'Update FAQ';
            faqIdField.style.display = 'block';
            questionField.style.display = 'block';
            answerField.style.display = 'block';
            break;
        case 'delete':
            modalTitle.textContent = 'Delete FAQ';
            faqIdField.style.display = 'block';
            questionField.style.display = 'none';
            answerField.style.display = 'none';
            clearForm();
            break;
    }
}

// Close FAQ Modal
function closeFaqModal() {
    const faqModal = document.getElementById('faq-modal');
    faqModal.style.display = "none";
  }

function clearForm() {
    document.getElementById('faq-id').value = '';
    document.getElementById('question').value = '';
    document.getElementById('answer').value = '';
}

async function handleSubmit() {
    const faqId = document.getElementById('faq-id').value;
    const question = document.getElementById('question').value;
    const answer = document.getElementById('answer').value;

    switch (currentOperation) {
        case 'add':
            await addFaq(question, answer);
            break;
        case 'update':
            await updateFaq(faqId, question, answer);
            break;
        case 'delete':
            await deleteFaq(faqId);
            break;
    }
}

async function addFaq(question, answer) {
    if (!question || !answer) {
        notificationManager.show('Please fill in all fields.', 'error', 4000);
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        notificationManager.show('Authentication token not found, please login again', 'error', 4000);
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/add`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ question, answer })
        });

        if (response.ok) {
            notificationManager.show('FAQ added successfully', 'success', 4000);
            closeFaqModal();
            loadFAQs();
        } else {
            notificationManager.show('Error adding FAQ', 'error', 4000);
        }
    } catch (error) {
        console.error('Error:', error);
        notificationManager.show('An error occurred while adding FAQ', 'error', 4000);
    }
}

async function updateFaq(faqId, question, answer) {
    if (!faqId || !question || !answer) {
        notificationManager.show('Please fill in all fields', 'error', 4000);
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        notificationManager.show('Authentication token not found, please login again!', 'error', 4000);
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/update?id=${faqId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ question, answer })
        });

        if (response.ok) {
            notificationManager.show('FAQ updated successfully', 'success', 4000);
            closeFaqModal();
            loadFAQs();
        } else if (response.status === 404) {
            notificationManager.show('FAQ not found', 'error', 4000);
        } else {
            notificationManager.show('Error updating FAQ', 'error', 4000);
        }
    } catch (error) {
        console.error('Error:', error);
        notificationManager.show('An error occured while updating FAQ', 'error', 4000);
    }
}

async function deleteFaq(faqId) {
    if (!faqId) {
        notificationManager.show('Please enter a FAQ ID', 'error', 4000);
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        notificationManager.show('Authentication token not found, please login again!', 'error', 4000);
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/delete?id=${faqId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });
        if (response.ok) {
            notificationManager.show('Faq deleted successfully', 'success', 4000);
            closeFaqModal();
            loadFAQs();
        } else if (response.status === 404) {
            notificationManager.show('FAQ not found', 'error', 4000);
        } else {
            notificationManager.show('Error deleting FAQ', 'error', 4000);
        }
    } catch (error) {
        console.error('Error:', error);
        notificationManager.show('An error occured while deleting FAQ', 'error', 4000);
    }
}

async function performSearch() {
    console.log('Performing search...');
    const searchInputElement = document.getElementById('search-input');
    const resultsContainer = document.getElementById('results-container');
    const searchErrorMessage = document.getElementById('search-error-message');
    searchErrorMessage.style.display = 'none';

    resultsContainer.innerHTML = '';

    if (!searchInputElement) {
        console.error('Search input element not found');
        searchErrorMessage.innerText = 'Search input not found. Please check the HTML structure.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    const query = searchInputElement.value.trim();
    
    if (!query) {
        console.log('Empty search query');
        searchErrorMessage.innerText = 'Please enter a search term.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    try {
        console.log('Fetching search results for query:', query);
        const encodedQuery = encodeURIComponent(query);
        const response = await fetch(`http://localhost:5281/api/File/Search?query=${encodedQuery}`);
        if (!response.ok) throw new Error('Network response was not ok');

        const results = await response.json();
        console.log('Search results:', results);

        if (results.length === 0) {
            console.log('No results found');
            resultsContainer.innerHTML = '<p>No documents found matching the search criteria.</p>';
            return;
        }

        results.forEach((doc, index) => {
            console.log(`Processing document ${index + 1}:`, doc);
            const docElement = document.createElement('div');
            docElement.className = 'doc-item';
        
            const tags = doc.tags?.join(', ') ?? 'No tags available';
        
            docElement.innerHTML = `
                <p><strong>Title:</strong> ${doc.title ?? 'No title available'}</p>
                <p><strong>Subject:</strong> ${doc.subject ?? 'No subject available'}</p>
                <p><strong>Grade:</strong> ${doc.grade ?? 'No grade available'}</p>
                <p><strong>Description:</strong> ${doc.description ?? 'No description available'}</p>
                <p><strong>File Size:</strong> ${doc.file_Size ? `${doc.file_Size} MB` : 'File size not available'}</p>
                <p><strong>Tags:</strong> ${tags}</p>
                <p><strong>Date Uploaded:</strong> ${doc.date_Uploaded ? new Date(doc.date_Uploaded).toLocaleDateString() : 'Date not available'}</p>
                <p><strong>Date Updated:</strong> ${doc.date_Updated ? new Date(doc.date_Updated).toLocaleDateString() : 'Date not available'}</p>
            `;


            const buttonContainer = document.createElement('div');
            buttonContainer.className = 'button-container';

            // Preview button
            const previewButton = document.createElement('button');
            previewButton.className = 'preview-button';
            previewButton.innerText = 'Preview';
            if (doc.file_Url) {
                previewButton.onclick = function() {
                    console.log('Preview button clicked for document:', doc.title);
                    previewDocument(doc.file_Url);
                };
            } else {
                previewButton.disabled = true;
                previewButton.title = 'Preview not available';
                console.log('Preview not available for document:', doc.title);
            }

            // Download button
            const downloadButton = document.createElement('button');
            downloadButton.className = 'download-button';
            downloadButton.innerText = 'Download';
            if (doc.file_Url) {
                downloadButton.onclick = function() {
                    console.log('Download button clicked for document:', doc.title);
                    downloadDocument(doc.file_Url, doc.title);
                };
            } else {
                downloadButton.disabled = true;
                downloadButton.title = 'Download not available';
                console.log('Download not available for document:', doc.title);
            }

            // Report button creation and handler
            const reportButton = document.createElement('button');
            reportButton.className = 'report-button';
            reportButton.innerHTML = '<strong>!</strong>';
            reportButton.onclick = function() {
                console.log('Report button clicked for document:', doc.title);
                
                let documentId = null;
                
                if (doc.id) {
                    try {
                        // Properly format MongoDB ObjectId from components
                        if (typeof doc.id === 'object' && 
                            doc.id.timestamp && 
                            doc.id.machine && 
                            doc.id.pid && 
                            doc.id.increment) {
                            
                            // Ensure proper padding for each component
                            const timestamp = doc.id.timestamp.toString(16).padStart(8, '0');
                            const machine = doc.id.machine.toString(16).padStart(6, '0');
                            const pid = doc.id.pid.toString(16).padStart(4, '0');
                            const increment = doc.id.increment.toString(16).padStart(6, '0');
                            
                            // Combine to create proper 24-character MongoDB ObjectId
                            documentId = `${timestamp}${machine}${pid}${increment}`;
                            
                            // Validate the constructed ID length
                            if (documentId.length !== 24) {
                                console.error('Constructed ID invalid length:', documentId.length);
                                throw new Error('Invalid ID construction');
                            }
                            
                            console.log('Constructed MongoDB ObjectId:', documentId);
                        }
                    } catch (error) {
                        console.error('Error constructing document ID:', error);
                        console.error('Raw ID data:', doc.id);
                        documentId = null;
                    }
                }

                if (documentId && /^[0-9a-fA-F]{24}$/.test(documentId)) {
                    openReportModal(documentId);
                } else {
                    console.error('Document ID construction failed:', {
                        title: doc.title,
                        rawId: doc.id,
                        constructedId: documentId
                    });
                    alert('Unable to report this document. Invalid document ID format.');
                }
            };
            

            buttonContainer.appendChild(previewButton);
            buttonContainer.appendChild(downloadButton);
            buttonContainer.appendChild(reportButton);
            docElement.appendChild(buttonContainer);

            resultsContainer.appendChild(docElement);
        });

    } catch (error) {
        console.error('Error fetching search results:', error);
        searchErrorMessage.innerText = 'Error fetching search results. Please try again later.';
        searchErrorMessage.style.display = 'block';
    }
}

const AUTH_TOKEN = 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';

// Store the current document ID globally
let currentDocumentId = null;

// Simple direct preview function
function previewDocument(url) {
    console.log('Previewing document:', url);
    const modal = document.getElementById('preview-modal');
    const iframe = document.getElementById('preview-iframe');
    
    if (!modal || !iframe) {
        console.error('Modal or iframe element not found');
        showFeedback('Preview functionality is not available. Please check your HTML structure.');
        return;
    }
    
    if (!url) {
        console.error('Invalid URL for preview:', url);
        showFeedback('Preview is not available for this document.');
        return;
    }
    
    // Direct preview in iframe
    iframe.src = url;
    modal.style.display = 'block';
}

async function downloadDocument(url, title) {
    console.log('Downloading document:', url, 'with title:', title);
    if (!url) {
        console.error('Invalid URL for download:', url);
        showFeedback('Download is not available for this document.');
        return;
    }

    try {
        // Fetch the document with authorization
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': AUTH_TOKEN
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // Get the blob from the response
        const blob = await response.blob();
        
        // Create a URL for the blob
        const downloadUrl = window.URL.createObjectURL(blob);
        
        // Create and trigger download
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = title || 'document';
        document.body.appendChild(link);
        link.click();
        
        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);

    } catch (error) {
        console.error('Error downloading document:', error);
        showFeedback('Error downloading document. Please try again later.');
    }
}

// Modal management
function closePreviewModal() {
    const previewModal = document.getElementById('preview-modal');
    if (previewModal) {
        previewModal.style.display = "none";
    }
}

function openReportModal(documentId) {
    console.log('openReportModal called with documentId:', documentId);
    if (!documentId) {
        console.error('No document ID provided to report modal');
        showFeedback('Error: Cannot report document without ID');
        return;
    }

    const modal = document.getElementById('report-modal');
    if (!modal) {
        console.error('Report modal element not found');
        return;
    }

    // Store the document ID globally
    currentDocumentId = documentId;
    
    // Also set it in the hidden input as a backup
    const hiddenInput = document.getElementById('report-doc-id');
    if (hiddenInput) {
        hiddenInput.value = documentId;
    }
    
    modal.style.display = 'block';
}

function closeReportModal() {
    const modal = document.getElementById('report-modal');
    if (modal) {
        modal.style.display = 'none';
        // Clear the form
        const reasonInput = document.getElementById('report-reason');
        if (reasonInput) {
            reasonInput.value = '';
        }
        // Clear the stored document ID
        currentDocumentId = null;
    }
}

async function submitReport() {
    // Get document ID from either global variable or hidden input
    const documentId = currentDocumentId || document.getElementById('report-doc-id')?.value;
    const reason = document.getElementById('report-reason')?.value?.trim();

    console.log('Submitting report for document:', documentId);

    try {
        // Input validation
        if (!documentId || !reason) {
            throw new Error(
                !documentId ? 'No document selected for reporting' : 'Please provide a reason for reporting'
            );
        }

        // Validate DocumentId format (24-character hexadecimal string)
        const objectIdRegex = /^[0-9a-fA-F]{24}$/;
        if (!objectIdRegex.test(documentId)) {
            throw new Error('Invalid document ID format. Please try again or contact support.');
        }

        const reportData = {
            documentId: documentId,
            reason: reason
        };

        const response = await fetch('http://localhost:5281/api/Reporting/CreateReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(reportData)
        });

        // Handle non-200 responses
        if (!response.ok) {
            let errorMessage = 'Failed to submit report';
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
                errorMessage = response.statusText;
            }
            throw new Error(errorMessage);
        }

        // Parse successful response
        const responseData = await response.json();
        console.log('Report submitted successfully:', responseData);
        
        // Show success message with report ID if available
        showFeedback(
            "Report submitted successfully!" + 
            (responseData.id ? ` ID: ${responseData.id}` : ''),
            'success'
        );

        // Clear form and close modal
        const reasonInput = document.getElementById('report-reason');
        if (reasonInput) {
            reasonInput.value = '';
        }
        closeReportModal();

    } catch (error) {
        console.error('Error submitting report:', error);
        showFeedback(error.message, 'error');
    }
}


// Feedback display
function showFeedback(message) {
    const feedbackElement = document.getElementById('create-response');
    if (feedbackElement) {
        feedbackElement.textContent = message;
        // Make sure the feedback is visible
        feedbackElement.style.display = 'block';
    } else {
        console.warn('Feedback element not found. Message:', message);
        alert(message); // Fallback to alert if feedback element is missing
    }
}

// Event listeners for modal closing
window.addEventListener('DOMContentLoaded', () => {
    // Close modals when clicking outside
    window.onclick = function(event) {
        const previewModal = document.getElementById('preview-modal');
        const reportModal = document.getElementById('report-modal');
        
        if (event.target === previewModal) {
            closePreviewModal();
        }
        if (event.target === reportModal) {
            closeReportModal();
        }
    };

    // Close buttons for modals
    const closePreviewButton = document.getElementById('close-preview-modal');
    const closeReportButton = document.getElementById('close-report-modal');

    if (closePreviewButton) {
        closePreviewButton.onclick = closePreviewModal;
    }
    if (closeReportButton) {
        closeReportButton.onclick = closeReportModal;
    }
});


function showFeedback(message) {
    const feedbackElement = document.getElementById('create-response');
    if (feedbackElement) {
        feedbackElement.textContent = message;
    } else {
        console.warn('Feedback element not found. Message:', message);
        alert(message);
    }
}



// Function to show error messages for login
function showError(message, errorType) {
    let errorMessage; 
    if (errorType === 'login') {
        errorMessage = document.getElementById('error-message'); // Assuming you have a separate div for login errors
    } else {
        errorMessage = document.getElementById('error-message'); // Change this logic as needed
    }
    
    errorMessage.innerText = message;
    errorMessage.style.display = 'block';

    // Automatically hide the error message after 5 seconds
    setTimeout(() => {
        errorMessage.style.display = 'none';
    }, 5000);
}

// Function to load the user's profile icon and name after login
function loadUserProfile() {
    const userName = localStorage.getItem('userName');
    const profileIcon = document.getElementById('user-profile');
    const profileName = document.getElementById('profile-name');
    
    if (userName && profileIcon && profileName) {
        // Show the profile icon
        profileIcon.style.display = 'block'; 
        
        // Display initials or full name in the profile icon
        const initials = userName.split(' ').map(name => name.charAt(0).toUpperCase()).join('');
        profileName.textContent = initials; 
    } else {
        // Hide the profile icon if no user is logged in
        if (profileIcon) {
            profileIcon.style.display = 'none';
        }
    }
}


function updateContributeTabAndNavigation(userRole) {
    const contributeTab = document.getElementById('contribute');
    if (contributeTab) {
        // Remove or hide the introductory message paragraph
        const introParagraph = contributeTab.querySelector('.contribute-message'); 
        if (introParagraph) {
            introParagraph.style.display = 'none';
        }

        // Update heading and message for logged-in users
        contributeTab.querySelector('h2').innerText = 'Welcome Back!';
        const paragraph = contributeTab.querySelector('p');
        if (paragraph) {
            paragraph.innerText = 'We are excited to see what valuable resources you will share today!';
        }

        // Create and append the upload button
        const uploadBtn = document.createElement('button');
        uploadBtn.id = 'upload-btn';
        uploadBtn.className = 'upload-btn';
        uploadBtn.innerText = 'Upload';
        contributeTab.appendChild(uploadBtn);

        // Create and append the file input (hidden)
        const fileUpload = document.createElement('input');
        fileUpload.type = 'file';
        fileUpload.id = 'file-upload';
        fileUpload.style.display = 'none';
        contributeTab.appendChild(fileUpload);

        // Create and append the upload form (initially hidden)
        const uploadForm = document.createElement('div');
        uploadForm.id = 'upload-form';
        uploadForm.style.display = 'none';
        uploadForm.innerHTML = `
            <input type="text" id="title" placeholder="Title" />
            <input type="text" id="subject" placeholder="Subject" />
            <input type="text" id="grade" placeholder="Grade (integer)" /> 
            <textarea id="description" placeholder="Description"></textarea>
            <button id="clear-btn" style="background-color: red; color: white;">Clear</button> 
        `;
        contributeTab.appendChild(uploadForm);

        // Maximum file size in MB
        const maxFileSizeMb = 10;

        // Allowed file types
        const allowedFileTypes = ['.pdf', '.doc', '.docx'];

        // Add event listener to the upload button
        uploadBtn.addEventListener('click', () => {
            if (uploadForm.style.display === 'block') {
                handleUpload();
            } else {
                fileUpload.click();
            }
        });

        // Handle file selection
        fileUpload.addEventListener('change', (event) => {
            const file = event.target.files[0];
            if (file) {
                try {
                    // Validate file size
                    if (file.size > maxFileSizeMb * 1024 * 1024) {
                        throw new Error(`File size exceeds the limit of ${maxFileSizeMb} MB`);
                    }
                
                    // Validate file type
                    const fileType = '.' + file.name.split('.').pop().toLowerCase();
                    if (!allowedFileTypes.includes(fileType)) {
                        throw new Error(`File type '${fileType}' is not allowed. Allowed types are: ${allowedFileTypes.join(', ')}`);
                    }

                    uploadForm.style.display = 'block';
                    notificationManager.show(`Selected file: ${file.name}`, 'success', 2000);
                } catch (error) {
                    fileUpload.value = '';
                    notificationManager.show(error.message, 'error', 4000);
                }
            }
        });

        // Function to handle the upload process
        async function handleUpload() {
            const titleInput = document.getElementById('title');
            const subjectInput = document.getElementById('subject');
            const gradeInput = document.getElementById('grade');
            const descriptionInput = document.getElementById('description');

            // Check if elements exist
            if (!titleInput || !subjectInput || !gradeInput || !descriptionInput) {
                notificationManager.show('One or more form elements are missing. Please check the HTML structure.', 'error', 4000);
                return;
            }

            // Validate inputs
            if (!titleInput.value.trim() || !subjectInput.value.trim() || !gradeInput.value.trim() || !descriptionInput.value.trim()) {
                notificationManager.show('Please fill in all fields.', 'error', 4000);
                return;
            }

            // Validate grade is an integer
            const gradeValue = parseInt(gradeInput.value, 10);
            if (isNaN(gradeValue) || gradeValue <= 0) {
                notificationManager.show('Please enter a valid grade as a positive integer.', 'error', 4000);
                return;
            }

            // Prepare form data
            const formData = new FormData();
            formData.append('UploadedFile', fileUpload.files[0]);
            formData.append('Title', titleInput.value);
            formData.append('Subject', subjectInput.value);
            formData.append('Grade', gradeValue);
            formData.append('Description', descriptionInput.value);

            // Get the authentication token
            const authToken = localStorage.getItem('token');
            if (!authToken) {
                notificationManager.show('Authentication token not found. Please log in.', 'error', 4000);
                return;
            }

            try {
                // Show upload in progress notification
                notificationManager.show('Uploading file...', 'warning', 6000);

                const response = await fetch('http://localhost:5281/api/File/upload', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                });
            
                if (!response.ok) {
                    throw new Error(`Upload failed: ${response.statusText}`);
                }

                const result = await response.json();
                
                // Clear the uploading message
                notificationManager.clearAll();

                // Show success message
                notificationManager.show(`File uploaded successfully! Generated tags: ${result.tags.join(', ')}`, 'success', 5000);

                // Clear the form
                fileUpload.value = '';
                titleInput.value = '';
                subjectInput.value = '';
                gradeInput.value = '';
                descriptionInput.value = '';
                uploadForm.style.display = 'none';

            } catch (error) {
                console.error('Upload error:', error);
                notificationManager.show(error.message, 'error', 4000);
            }
        }

        // Add event listener for the clear button
        const clearBtn = document.getElementById('clear-btn');
        clearBtn.addEventListener('click', () => {
            document.getElementById('title').value = '';
            document.getElementById('subject').value = '';
            document.getElementById('grade').value = '';
            document.getElementById('description').value = '';
            fileUpload.value = '';
            uploadForm.style.display = 'none';
            notificationManager.show('Form cleared', 'success', 2000);
        });
    }


    // Update dropdown menu: change the login button to logout
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) logoutBtn.style.display = 'block'; // Show the logout button

    const loginLink = document.querySelector('a[href="#login"]');
    if (loginLink) loginLink.style.display = 'none'; // Hide the login link

     // Get the more dropdown content
     const dropdownContent = document.querySelector('.slide-out-content');
     if (dropdownContent) {
         // Remove existing admin links if they exist
         const existingAdminLinks = document.getElementById('admin-links');
         if (existingAdminLinks) {
             existingAdminLinks.remove();
         }
 
         // Create new admin links container
         const adminLinks = document.createElement('div');
         adminLinks.id = 'admin-links';
         
         // Create Moderate link
         const moderateLink = document.createElement('a');
         moderateLink.href = '#moderate';
         moderateLink.id = 'moderate-link';
         moderateLink.textContent = 'Moderate';
         moderateLink.addEventListener('click', (e) => {
             e.preventDefault();
             window.location.href = './moderation.html';
         });
 
         // Create Reports link
         const reportsLink = document.createElement('a');
         reportsLink.href = '#reports';
         reportsLink.id = 'reports-link';
         reportsLink.textContent = 'Reports';
         reportsLink.addEventListener('click', (e) => {
             e.preventDefault();
             window.location.href = './reporting.html';
         });
 
         // Add the new links to the admin links container
         adminLinks.appendChild(moderateLink);
         adminLinks.appendChild(reportsLink);
 
         // Add the admin links to the dropdown
         dropdownContent.appendChild(adminLinks);
 
         // Show admin links based on user role
         if (userRole === 'admin' || userRole === 'teacher') {
             adminLinks.style.display = 'block';
         } else {
             adminLinks.style.display = 'none';
         }
     }
    // Show FAQ management buttons only if the user is an admin
    if (userRole === 'admin') {
        const faqButtons = document.getElementById('faq-buttons');
        if (faqButtons) {
            faqButtons.style.display = 'flex'; // Make sure this line is executing
        }
    }
}

// Helper function to show messages to the user
function showMessage(message, type) {
    const contributeTab = document.getElementById('contribute');
    const messageElement = document.createElement('div');
    messageElement.textContent = message;
    messageElement.className = 'message ${type}';
    contributeTab.appendChild(messageElement);
    
    // Remove the message after 5 seconds
    setTimeout(() => {
        messageElement.remove();
    }, 5000);
}



// Function to check login status and update UI on page load
function checkLoginStatus() {
    const isLoggedIn = localStorage.getItem('isLoggedIn');
    const userName = localStorage.getItem('userName');
    const userRole = 'admin' //localStorage.getItem('userRole'); // Get the stored user role

    if (isLoggedIn && userName) {
        // Update the user profile and UI elements
        loadUserProfile();
        updateContributeTabAndNavigation(userRole);
    }
}

// Function to handle logout
function performLogout() {
    localStorage.removeItem('userName');
    localStorage.removeItem('isLoggedIn');
    localStorage.removeItem('userRole');
    window.location.href = './index.html'; // Redirect to landing page
};

document.addEventListener('DOMContentLoaded', function() {
    const featureItems = document.querySelectorAll('.feature-item');

    // Use setTimeout to stagger the visibility for each feature item
    featureItems.forEach((item, index) => {
        setTimeout(() => {
            item.classList.add('visible');
        }, index * 200); // Stagger visibility by 200ms
    });
});

// Global variable to store all documents
let documentsBySubject = {};

// Function to fetch documents from the API
async function fetchModeratedDocuments() {
    try {
        const response = await fetch('http://localhost:5281/api/File/GetModerated');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        documentsBySubject = data.groupedBySubject;
        updateSubjectBlocks();
    } catch (error) {
        console.error('Error fetching documents:', error);
        showError('Failed to load documents. Please try again later.');
    }
}

// Function to handle report submission
async function handleReport() {
    const docIdInput = document.getElementById('report-doc-id');
    const reasonSelect = document.getElementById('report-reason');
    
    try {
        // Get values and validate
        const documentId = docIdInput?.value;
        const reason = reasonSelect?.value;

        if (!documentId || !reason) {
            throw new Error('Please provide both document ID and reason for reporting');
        }

        // Validate DocumentId format (24-character hexadecimal string)
        const objectIdRegex = /^[0-9a-fA-F]{24}$/;
        if (!objectIdRegex.test(documentId)) {
            throw new Error('Invalid document ID format');
        }

        // Prepare request data
        const reportData = {
            documentId: documentId,
            reason: reason
        };

        // Send request to API
        const response = await fetch('http://localhost:5281/api/Reporting/CreateReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(reportData)
        });

        // Handle response
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to submit report');
        }

        const result = await response.json();
        
        // Show success message
        showFeedback(`Report submitted successfully! ID: ${result.id}`, 'success');
        
        // Reset form and close modal
        reasonSelect.value = 'Inappropriate Content'; // Reset to default option
        closeReportModal();

    } catch (error) {
        console.error('Report submission error:', error);
        showFeedback(error.message, 'error');
    }
}

// Function to display documents for a selected subject
function displayDocuments(subject) {
    const documents = documentsBySubject[subject] || [];
    const displayDiv = document.getElementById('documentDisplay');

    if (documents.length === 0) {
        displayDiv.innerHTML = `
            <div class="no-documents-message">
                No documents available for ${subject}
            </div>
        `;
        return;
    }

    // Create a unique ID for the documents section
    const sectionId = `documents-section-${subject.replace(/[^a-zA-Z0-9]/g, '-')}`;

    // Create the document display HTML
    let html = `
        <div class="documents-container" id="${sectionId}">
            <h2 class="documents-title">${subject} Documents</h2>
            <div class="documents-grid">
    `;

    documents.forEach(doc => {
        const uploadDate = new Date(doc.date_Uploaded).toLocaleDateString();
        const fileSize = formatFileSize(doc.file_Size);

        html += `
            <div class="document-card">
                <div class="document-header">
                    <h3 class="document-card-title">${doc.title}</h3>
                </div>
                <div class="document-content">
                    <div class="document-details">
                        <div class="detail-row">
                            <span class="detail-label">Title:</span>
                            <span class="detail-value">${doc.title}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Description:</span>
                            <span class="detail-value">${doc.description || 'No description available'}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Grade:</span>
                            <span class="detail-value">${doc.grade}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Size:</span>
                            <span class="detail-value">${fileSize} mb</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Uploaded:</span>
                            <span class="detail-value">${uploadDate}</span>
                        </div>
                    </div>
                    
                    <div class="document-tags">
                        ${(doc.tags || []).map(tag => `<span class="tag">${tag}</span>`).join('')}
                    </div>
                </div>
                
                <div class="document-actions">
                    <button class="button preview-button" onclick="window.open('${doc.preview_Url || '#'}', '_blank')">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                        Preview
                    </button>
                    
                    <a href="${doc.file_Url}" class="button download-button" target="_blank">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                            <polyline points="7 10 12 15 17 10"/>
                            <line x1="12" y1="15" x2="12" y2="3"/>
                        </svg>
                        Download
                    </a>
                    
                    <button class="button report-button" onclick="handleReport('${doc.id}')">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0"/>
                        </svg>
                        Report
                    </button>
                </div>
            </div>
        `;
    });
    
    html += `
            </div>
        </div>
    `;

    displayDiv.innerHTML = html;

    // Scroll to the newly created section
    const documentsSection = document.getElementById(sectionId);
    if (documentsSection) {
        // Wait for the DOM to be fully updated
        requestAnimationFrame(() => {
            documentsSection.scrollIntoView({
                behavior: 'smooth',
                block: 'start',
                inline: 'nearest'
            });
        });
    }
}



// Function to update subject blocks with document counts and click handlers
function updateSubjectBlocks() {
    const subjectBlocks = document.querySelectorAll('.subject-block');
    
    subjectBlocks.forEach(block => {
        const subject = block.textContent;
        const documents = documentsBySubject[subject] || [];
        
        // Update block appearance
        block.innerHTML = `
            <div class="subject-title">${subject}</div>
            <div class="document-count">${documents.length} documents</div>
        `;
        
        // Add click event listener with visual feedback
        block.addEventListener('click', (e) => {
            // Add visual feedback on click
            block.classList.add('clicking');
            
            // Display documents and scroll
            displayDocuments(subject);
            
            // Remove the visual feedback after animation
            setTimeout(() => {
                block.classList.remove('clicking');
            }, 200);
        });
        
        // Add styling classes
        block.classList.add('subject-block-interactive');
        if (documents.length > 0) {
            block.classList.add('has-documents');
        } else {
            block.classList.add('no-documents');
        }
    });
}

// Function to format file size
function formatFileSize(size) {
    return parseFloat(size).toFixed(2);
}

// Function to show error messages
function showError(message) {
    const displayDiv = document.getElementById('documentDisplay');
    displayDiv.innerHTML = `
        <div class="error-message">
            <p>${message}</p>
        </div>
    `;
}

// Initialize the view when the document is loaded
document.addEventListener('DOMContentLoaded', fetchModeratedDocuments);

